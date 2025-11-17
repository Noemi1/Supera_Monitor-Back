using System.Diagnostics.Tracing;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aluno;
using Supera_Monitor_Back.Models.Eventos;
using Supera_Monitor_Back.Models.Eventos.Aula;
using Supera_Monitor_Back.Models.Eventos.Dtos;
using Supera_Monitor_Back.Models.Eventos.Participacao;

namespace Supera_Monitor_Back.Services.Eventos;

public interface IEventoService
{

	CalendarioEventoList GetPseudoAula(PseudoEventoRequest request);
	CalendarioEventoList GetEventoById(int eventoId);
	//public Task<List<FeriadoResponse>> GetFeriados(int ano);

	ResponseModel Insert(CreateEventoRequest request, int eventoTipoId);
	ResponseModel InsertAulaZero(CreateAulaZeroRequest request);
	ResponseModel InsertAulaExtra(CreateAulaExtraRequest request);
	ResponseModel InsertAulaForTurma(CreateAulaTurmaRequest request);

	ResponseModel Update(UpdateEventoRequest request);
	ResponseModel Cancelar(CancelarEventoRequest request);
	ResponseModel Finalizar(FinalizarEventoRequest request);
	ResponseModel FinalizarAulaZero(FinalizarAulaZeroRequest request);
	ResponseModel AgendarPrimeiraAula(PrimeiraAulaRequest model);
	ResponseModel AgendarReposicao(ReposicaoRequest model);
}

public class EventoService : IEventoService
{
	private readonly DataContext _db;
	private readonly IMapper _mapper;
	private readonly IProfessorService _professorService;
	private readonly ISalaService _salaService;
	private readonly IRoteiroService _roteiroService;
	private readonly IFeriadoService _feriadoService;

	private readonly Account? _account;

	public EventoService(
		DataContext db,
		IMapper mapper,
		IProfessorService professorService,
		ISalaService salaService,
		IRoteiroService roteiroService,
		IFeriadoService feriadoService,
		IHttpContextAccessor httpContextAccessor
	)
	{
		_db = db;
		_mapper = mapper;
		_professorService = professorService;
		_salaService = salaService;
		_roteiroService = roteiroService;
		_feriadoService = feriadoService;
		_account = (Account?)httpContextAccessor.HttpContext?.Items["Account"];
	}


	public CalendarioEventoList GetEventoById(int eventoId)
	{
		CalendarioEventoList? evento = _db.CalendarioEventoList.FirstOrDefault(e => e.Id == eventoId);

		if (evento is null)
		{
			throw new Exception("Evento não encontrado");
		}

		evento.Alunos = _db.CalendarioAlunoList.Where(a => a.Evento_Id == evento.Id).ToList();
		evento.Professores = _db.CalendarioProfessorLists.Where(e => e.Evento_Id == evento.Id).ToList();

		var PerfisCognitivos = _db.Evento_Aula_PerfilCognitivo_Rel
			.Where(p => p.Evento_Aula_Id == evento.Id)
			.Include(p => p.PerfilCognitivo)
			.Select(p => p.PerfilCognitivo)
			.ToList();

		evento.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(PerfisCognitivos);

		var feriados = _feriadoService.GetList();
		var feriado = feriados.FirstOrDefault(x => x.Data.Date == evento.Data.Date);
		evento.Feriado = feriado;

		var roteiros = _roteiroService.GetAll(evento.Data.Year);
		var roteiro = roteiros.FirstOrDefault(x => (evento.Roteiro_Id.HasValue && x.Id == evento.Roteiro_Id)
									|| x.DataInicio <= evento.Data.Date && x.DataFim >= evento.Data.Date);

		evento.Roteiro_Id = roteiro?.Id ?? -1;
		evento.Tema = roteiro?.Tema;
		evento.RoteiroCorLegenda = roteiro?.CorLegenda;
		evento.Semana = roteiro?.Semana;


		if (feriado is not null && evento.Active == true)
		{
			evento.Observacao = "Cancelamento Automático. <br> Feriado: " + feriado.Descricao;
			evento.Deactivated = feriado.Data;
		}
		return evento;
	}

	public CalendarioEventoList GetPseudoAula(PseudoEventoRequest request)
	{
		CalendarioEventoList? eventoAula = _db.CalendarioEventoList.FirstOrDefault(x =>
				  x.Data == request.DataHora
				  && x.Turma_Id == request.Turma_Id);

		if (eventoAula != null)
		{
			eventoAula = this.GetEventoById(eventoAula.Id);
			return eventoAula;
		}
		else
		{

			var turma = _db.TurmaLists
				.FirstOrDefault(x => x.Id == request.Turma_Id
						&& x.Deactivated == null
						&& x.DiaSemana == (int)request.DataHora.DayOfWeek
						&& x.Horario!.Value == request.DataHora.TimeOfDay);


			if (turma is null)
			{
				throw new Exception("Turma não encontrada!");
			}

			var professorTurma = _db.ProfessorList.FirstOrDefault(x => x.Id == turma.Professor_Id);
			if (professorTurma is null)
			{
				throw new Exception("Professor não encontrado!");
			}

			var roteiros = _roteiroService.GetAll(request.DataHora.Year);
			var feriados = _feriadoService.GetList();

			var data = request.DataHora.Date;

			var roteiro = roteiros.FirstOrDefault(x => x.DataInicio.Date <= data && x.DataFim.Date >= data);
			var feriado = feriados.FirstOrDefault(x => x.Data.Date == data);


			var vigenciasTurma = _db.Aluno_Turma_Vigencia
									.Where(x => x.Turma_Id == request.Turma_Id
										&& x.DataInicioVigencia.Date <= data
										&& (!x.DataFimVigencia.HasValue || x.DataFimVigencia.Value.Date >= data))
									.ToList();

			var alunosVigentes = vigenciasTurma.Select(x => x.Aluno_Id);

			var alunos = _db.AlunoList
				.Where(x => alunosVigentes.Contains(x.Id))
				.OrderBy(x => x.Nome)
				.ToList();

			var alunosAtivosInTurma = alunos.Count;

			CalendarioEventoList pseudoAula = new()
			{
				Id = -1,
				Evento_Tipo_Id = (int)EventoTipo.Aula,
				Evento_Tipo = "Pseudo-Aula",

				Descricao = turma.Nome, // Pseudo aulas ganham o nome da turma
				DuracaoMinutos = 120, // As pseudo aulas são de uma turma e duram 2h por padrão

				Roteiro_Id = roteiro?.Id,
				Semana = roteiro?.Semana,
				Tema = roteiro?.Tema,
				RoteiroCorLegenda = roteiro?.CorLegenda,

				Data = request.DataHora,

				Turma_Id = turma.Id,
				Turma = turma.Nome,

				VagasDisponiveisEvento = turma.CapacidadeMaximaAlunos - alunosAtivosInTurma,
				CapacidadeMaximaEvento = turma.CapacidadeMaximaAlunos,
				AlunosAtivosEvento = alunosAtivosInTurma,

				VagasDisponiveisTurma = turma.CapacidadeMaximaAlunos - alunosAtivosInTurma,
				CapacidadeMaximaTurma = turma.CapacidadeMaximaAlunos,
				AlunosAtivosTurma = alunosAtivosInTurma,

				Professor_Id = turma.Professor_Id,
				Professor = turma.Professor ?? "Professor Indefinido",
				CorLegenda = turma.CorLegenda ?? "#000",

				Finalizado = false,

				Sala = turma.Sala ?? "SalaIndefinida",
				Sala_Id = turma.Sala_Id,
				NumeroSala = turma.NumeroSala,
				Andar = turma.Andar,

				Feriado = feriado,
			};

			if (feriado is not null)
			{
				pseudoAula.Feriado = feriado;
				pseudoAula.Deactivated = feriado.Data;
				pseudoAula.Observacao = "Cancelamento Automático. <br> Feriado: " + feriado.Descricao;
			}

			pseudoAula.Alunos = _mapper.Map<List<CalendarioAlunoList>>(alunos).ToList();

			pseudoAula.Professores.Add(new CalendarioProfessorList
			{
				Id = null,
				Evento_Id = pseudoAula.Id,
				Professor_Id = professorTurma.Id,
				Nome = professorTurma.Nome,
				CorLegenda = professorTurma.CorLegenda,
				Presente = null,
				Observacao = "",
				Account_Id = professorTurma.Account_Id,
				Telefone = professorTurma.Telefone,
				ExpedienteFim = professorTurma.ExpedienteFim,
				ExpedienteInicio = professorTurma.ExpedienteInicio,
			});

			List<PerfilCognitivo> perfisCognitivos = _db.Turma_PerfilCognitivo_Rels
				.Include(x => x.PerfilCognitivo)
				.Where(p => p.Turma_Id == request.Turma_Id)
				.Select(p => p.PerfilCognitivo)
				.ToList();

			pseudoAula.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfisCognitivos);

			return pseudoAula;
		}
	}

	//public async Task<List<FeriadoResponse>> GetFeriados(int ano)
	//{
	//	List<FeriadoResponse> feriados = new List<FeriadoResponse>() { };
	//	string token = "20487|fbPtn71wk6mjsGDWRdU8mGECDlNZhyM7";
	//	string url = $"https://api.invertexto.com/v1/holidays/{ano}?token={token}&state=SP";
	//	using (HttpClient client = new HttpClient())
	//	{
	//		try
	//		{
	//			HttpResponseMessage response = await client.GetAsync(url);
	//			//response.EnsureSuccessStatusCode(); // Lança uma exceção para códigos de status de erro
	//			string responseContent = await response.Content.ReadAsStringAsync();
	//			feriados = JsonSerializer.Deserialize<List<FeriadoResponse>>(responseContent) ?? feriados;
	//			feriados = feriados!.OrderBy(x => x.date).ToList();

	//		}
	//		catch (Exception e)
	//		{
	//			Console.WriteLine("\nException Caught!");
	//			Console.WriteLine("Message :{0} ", e.Message);
	//		}

	//		return feriados;
	//	}
	//}


	public ResponseModel Insert(CreateEventoRequest request, int eventoTipoId)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			// Validação de quantidades de alunos/professores para cada tipo de evento

			string eventoTipo;

			switch (eventoTipoId)
			{
				case (int)EventoTipo.Reuniao:
					if (request.Alunos.Count != 0)
						return new ResponseModel { Message = "Um evento de reunião não pode ter alunos associados" };

					eventoTipo = "Reunião";
					break;

				case (int)EventoTipo.Oficina:
					eventoTipo = "Oficina";
					break;

				case (int)EventoTipo.Superacao:
					eventoTipo = "Superação";
					break;

				default:
					return new ResponseModel { Message = "Internal Server Error : 'Tipo de evento inválido'" };
			}

			var alunosInRequestId = request.Alunos
				.ToHashSet();

			var alunos = _db.Aluno
				.Where(x => alunosInRequestId.Contains(x.Id) && x.Deactivated == null)
				.ToList();

			if (alunos.Count != request.Alunos.Count)
				return new ResponseModel { Message = "Aluno(s) não encontrado(s)" };

			var professoresInRequestId = request.Professores
				.ToHashSet();

			var professores = _db.ProfessorList
				.Where(x => professoresInRequestId.Contains(x.Id) && x.Deactivated == null)
				.ToList();

			if (professores.Count != request.Professores.Count)
				return new ResponseModel { Message = "Professor(es) não encontrado(s)" };

			foreach (var professor in professores)
			{
				bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
					professorId: professor.Id,
					DiaSemana: (int)request.Data.DayOfWeek,
					Horario: request.Data.TimeOfDay,
					IgnoredTurmaId: null
				);

				if (hasTurmaConflict)
					return new ResponseModel { Message = $"Professor: '{professor.Nome}' possui uma turma nesse mesmo horário" };

				bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
					professorId: professor.Id,
					Data: request.Data,
					DuracaoMinutos: request.DuracaoMinutos,
					IgnoredEventoId: null
				);

				if (hasParticipacaoConflict)
					return new ResponseModel { Message = $"Professor: {professor.Nome} possui participação em outro evento nesse mesmo horário" };
			}

			if (request.Sala_Id.HasValue)
			{
				// Não devo poder registrar um evento em uma sala que não existe
				bool salaExists = _db.Salas.Any(s => s.Id == request.Sala_Id.Value);
				if (!salaExists)
					return new ResponseModel { Message = "Sala não encontrada" };

				// Sala deve estar livre no horário do evento
				bool isSalaOccupied = _salaService.IsSalaOccupied(request.Sala_Id.Value, request.Data, request.DuracaoMinutos, null);
				if (isSalaOccupied)
					return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };
			}

			if (request.CapacidadeMaximaAlunos < 0)
				return new ResponseModel { Message = "Capacidade máxima de alunos inválida" };

			//
			// Validations passed
			//

			Evento evento = new Evento
			{
				Data = request.Data,
				Descricao = request.Descricao ?? "Evento sem descrição",
				Observacao = request.Observacao ?? "Sem observação",
				DuracaoMinutos = request.DuracaoMinutos,
				CapacidadeMaximaAlunos = request.CapacidadeMaximaAlunos,
				Finalizado = false,

				Sala_Id = request.Sala_Id,

				Created = TimeFunctions.HoraAtualBR(),
				LastUpdated = null,
				Deactivated = null,
				Evento_Tipo_Id = eventoTipoId,
				Account_Created_Id = _account?.Id ?? 1
			};

			//
			// Insere professores
			//
			foreach (var professor in professores)
			{
				evento.Evento_Participacao_Professor.Add(new Evento_Participacao_Professor
				{
					Professor_Id = professor.Id
				});
			}

			// 
			// Insere alunos
			//
			var historicoInserir = new List<Aluno_Historico>() { };
			var hoje = TimeFunctions.HoraAtualBR();
			foreach (var aluno in alunos)
			{
				evento.Evento_Participacao_Aluno.Add(new Evento_Participacao_Aluno
				{
					Aluno_Id = aluno.Id,
					NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
					NumeroPaginaAH = aluno.NumeroPaginaAH,
					Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
					Apostila_AH_Id = aluno.Apostila_AH_Id,
				});

				historicoInserir.Add(new Aluno_Historico
				{
					Account_Id = _account?.Id ?? 1,
					Aluno_Id = aluno.Id,
					Data = hoje,
					Descricao = $"Aluno se inscreveu na {eventoTipo} do dia {evento.Data:G}"
				});
			}

			_db.Add(evento);
			_db.SaveChanges();

			// 
			// Atualiza checklists
			//
			if (alunos.Count > 0)
			{
				var alunosChecklists = _db.Aluno_Checklist_Item
					.Where(x => alunosInRequestId.Contains(x.Aluno_Id)
						&& x.DataFinalizacao == null
						&& (x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Oficina
						 || x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Oficina
						 || x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Superacao
						 || x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Superacao))
					.ToList();

				var alunosChecklistsOficina = alunosChecklists
					.Where(x => x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Oficina
						|| x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Oficina)
					.ToList();

				var alunosChecklistsSuperacao = alunosChecklists
					.Where(x => x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Superacao
						|| x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Superacao)
					.ToList();

				var alunosChecklistAtualizar = new List<Aluno_Checklist_Item>() { };

				if (evento.Evento_Tipo_Id == (int)EventoTipo.Superacao)
				{
					var item = alunosChecklistsSuperacao
						.FirstOrDefault(x => x.DataFinalizacao == null);

					if (item is not null)
					{
						item.Evento_Id = evento.Id;
						item.DataFinalizacao = hoje;
						item.Account_Finalizacao_Id = _account?.Id ?? 1;
						item.Observacoes = $"Checklist finalizado automaticamente. <br> Aluno agendou na superação do dia {request.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}.";
						alunosChecklistAtualizar.Add(item);
					}
				}
				else if (evento.Evento_Tipo_Id == (int)EventoTipo.Oficina)
				{
					var item = alunosChecklistsOficina
						.FirstOrDefault(x => x.DataFinalizacao == null);

					if (item is not null)
					{
						item.Evento_Id = evento.Id;
						item.DataFinalizacao = hoje;
						item.Account_Finalizacao_Id = _account?.Id ?? 1;
						item.Observacoes = $"Checklist finalizado automaticamente. <br> Aluno se inscreveu na oficina do dia {request.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}.";
						alunosChecklistAtualizar.Add(item);
					}

				}

				_db.Aluno_Historico.AddRange(historicoInserir);
				_db.Aluno_Checklist_Item.UpdateRange(alunosChecklistAtualizar);
				_db.SaveChanges();

			}


			response.Success = true;
			response.Message = $"{eventoTipo} registrada com sucesso";
			response.Object = this.GetEventoById(evento.Id);
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao inserir evento de tipo '{(int)eventoTipoId}': {ex}";
		}

		return response;
	}

	public ResponseModel InsertAulaForTurma(CreateAulaTurmaRequest request)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			// Se Turma_Id passado na requisição for NÃO NULO, a turma deve existir
			Turma? turma = _db.Turmas.Find(request.Turma_Id);

			if (turma is null)
			{
				return new ResponseModel { Message = "Turma não encontrada" };
			}

			// Não devo poder registrar uma aula com um professor que não existe
			var professor = _db.ProfessorList
				.FirstOrDefault(p => p.Id == request.Professor_Id);

			if (professor is null)
				return new ResponseModel { Message = "Professor não encontrado" };

			// Não devo poder registrar uma aula com um professor que está desativado
			if (!professor.Active)
				return new ResponseModel { Message = "Este professor está desativado" };

			// Não devo poder registrar uma aula em uma sala que não existe
			bool salaExists = _db.Salas.Any(s => s.Id == request.Sala_Id);
			if (!salaExists)
				return new ResponseModel { Message = "Sala não encontrada" };

			// Sala deve estar livre no horário do evento
			bool isSalaOccupied = _salaService.IsSalaOccupied(request.Sala_Id, request.Data, request.DuracaoMinutos, -1);
			if (isSalaOccupied)
				return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };


			bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
				professorId: professor.Id,
				DiaSemana: (int)request.Data.DayOfWeek,
				Horario: request.Data.TimeOfDay,
				IgnoredTurmaId: request.Turma_Id
			);

			if (hasTurmaConflict)
				return new ResponseModel { Message = $"Professor: '{professor.Nome}' possui uma turma nesse mesmo horário" };

			bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
				professorId: professor.Id,
				Data: request.Data,
				DuracaoMinutos: request.DuracaoMinutos,
				IgnoredEventoId: null
			);

			if (hasParticipacaoConflict)
				return new ResponseModel { Message = $"Professor: {professor.Nome} possui participação em outro evento nesse mesmo horário" };

			//
			// Validations passed
			//
			var roteiros = _roteiroService.GetAll(request.Data.Year);
			var roteiro = roteiros.FirstOrDefault(x => request.Data.Date >= x.DataInicio.Date
															&& request.Data.Date <= x.DataFim.Date
															&& x.Id != -1);



			var turmaPerfisCognitivos = _db.Turma_PerfilCognitivo_Rels
				.Where(t => t.Turma_Id == turma.Id)
				.Select(t => t.PerfilCognitivo_Id);

			var perfis = turmaPerfisCognitivos
				.Select(id => new Evento_Aula_PerfilCognitivo_Rel() { PerfilCognitivo_Id = id })
				.ToList();

			Evento evento = new()
			{
				Data = request.Data,
				Descricao = turma.Nome ?? request.Descricao ?? "Sem descrição",
				Observacao = request.Observacao,
				Sala_Id = request.Sala_Id,
				DuracaoMinutos = request.DuracaoMinutos,
				CapacidadeMaximaAlunos = turma.CapacidadeMaximaAlunos,

				Evento_Tipo_Id = (int)EventoTipo.Aula,
				Evento_Aula = new Evento_Aula
				{
					Roteiro_Id = roteiro?.Id ?? null,
					Turma_Id = turma.Id,
					Professor_Id = request.Professor_Id,
					Evento_Aula_PerfilCognitivo_Rel = perfis
				},

				Created = TimeFunctions.HoraAtualBR(),
				LastUpdated = null,
				Deactivated = null,
				Finalizado = false,
				ReagendamentoDe_Evento_Id = null,
				Account_Created_Id = _account?.Id ?? 1,
			};


			//
			// Professores
			//
			var participacaoProfessor = new Evento_Participacao_Professor()
			{
				Evento_Id = evento.Id,
				Professor_Id = professor.Id,
			};
			evento.Evento_Participacao_Professor = new List<Evento_Participacao_Professor>() { participacaoProfessor };


			// 
			// Alunos
			//
			var vigenciasTurma = _db.Aluno_Turma_Vigencia.Where(x => x.Turma_Id == request.Turma_Id
														&& x.DataInicioVigencia <= request.Data
														&& (!x.DataFimVigencia.HasValue || x.DataFimVigencia.Value >= request.Data))
													.ToList();

			var alunosVigentesId = vigenciasTurma.Select(x => x.Aluno_Id);
			var alunos = _db.AlunoList
				.Where(x => alunosVigentesId.Contains(x.Id))
				.ToList();


			var participacoesAluno = alunos.Select(aluno => new Evento_Participacao_Aluno
			{
				Evento_Id = evento.Id,
				Aluno_Id = aluno.Id,
				Presente = null,

				Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
				NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
				Apostila_AH_Id = aluno.Apostila_AH_Id,
				NumeroPaginaAH = aluno.NumeroPaginaAH,
			});
			evento.Evento_Participacao_Aluno = participacoesAluno.ToList();

			_db.Add(evento);
			_db.SaveChanges();

			response.Message = $"Aula para a turma '{turma.Nome}' registrado com sucesso";
			response.Object = this.GetEventoById(evento.Id);
			response.Success = true;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao registrar evento de aula: {ex}";
		}

		return response;
	}

	public ResponseModel InsertAulaExtra(CreateAulaExtraRequest request)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			// Não devo poder registrar uma aula com um professor que não existe
			var professor = _db.ProfessorList
				.FirstOrDefault(p => p.Id == request.Professor_Id);

			if (professor is null)
				return new ResponseModel { Message = "Professor não encontrado" };

			// Não devo poder registrar uma aula com um professor que está desativado
			if (!professor.Active)
				return new ResponseModel { Message = "Este professor está desativado" };

			// Não devo poder registrar uma aula em uma sala que não existe
			bool salaExists = _db.Salas.Any(s => s.Id == request.Sala_Id);
			if (!salaExists)
				return new ResponseModel { Message = "Sala não encontrada" };

			// Sala deve estar livre no horário do evento
			bool isSalaOccupied = _salaService.IsSalaOccupied(request.Sala_Id, request.Data, request.DuracaoMinutos, null);
			if (isSalaOccupied)
				return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };


			// O professor associado não pode possuir conflitos de horário
			bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
				professorId: professor.Id,
				DiaSemana: (int)request.Data.DayOfWeek,
				Horario: request.Data.TimeOfDay,
				IgnoredTurmaId: null
			);

			if (hasTurmaConflict)
				return new ResponseModel { Message = $"Professor: '{professor.Nome}' possui uma turma nesse mesmo horário" };

			bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
				professorId: professor.Id,
				Data: request.Data,
				DuracaoMinutos: request.DuracaoMinutos,
				IgnoredEventoId: null
			);

			if (hasParticipacaoConflict)
				return new ResponseModel { Message = $"Professor: {professor.Nome} possui participação em outro evento nesse mesmo horário" };

			var alunoIds = request.Alunos
				.Select(a => a.Aluno_Id)
				.ToHashSet();

			var alunos = _db.AlunoList
				.Where(x => alunoIds.Contains(x.Id)
						&& x.Deactivated == null || x.Deactivated.Value.Date > request.Data.Date)
				.ToList();

			if (alunos.Count != request.Alunos.Count)
				return new ResponseModel { Message = "Aluno(s) não encontrado(s)" };

			if (alunos.Count > request.CapacidadeMaximaAlunos)
				return new ResponseModel { Message = "Número máximo de alunos excedido" };

			var roteiros = _roteiroService.GetAll(request.Data.Year);
			var roteiro = roteiros.FirstOrDefault(x => request.Data.Date >= x.DataInicio.Date
															&& request.Data.Date <= x.DataFim.Date
															&& x.Id != -1);

			var checklistAgendamentos = _db.Aluno_Checklist_Item
				.Where(x => alunoIds.Contains(x.Aluno_Id)
					&& x.Checklist_Item_Id == (int)ChecklistItemId.AgendamentoPrimeiraAula)
				.ToList();

			var checklistAgendamentoPorAlunoId = checklistAgendamentos
				.ToLookup(x => x.Aluno_Id);

			var requestAlunosPorAlunoId = request.Alunos
				.ToDictionary(x => x.Aluno_Id);

			var eventosIds = request.Alunos
				.Select(x => x.ReposicaoDe_Evento_Id)
				.ToHashSet();

			var eventos = _db.Evento
				.Where(x => eventosIds.Contains(x.Id))
				.ToList();

			var eventosPorId = eventos
				.ToDictionary(x => x.Id, x => x);

			//
			// Evento
			//
			Evento evento = new Evento
			{
				Data = request.Data,
				Descricao = request.Descricao ?? "Turma extra",
				Observacao = request.Observacao,
				Sala_Id = request.Sala_Id,
				DuracaoMinutos = request.DuracaoMinutos,
				CapacidadeMaximaAlunos = request.CapacidadeMaximaAlunos,

				Evento_Tipo_Id = (int)EventoTipo.AulaExtra,

				Created = TimeFunctions.HoraAtualBR(),
				LastUpdated = null,
				Deactivated = null,
				Finalizado = false,
				Account_Created_Id = _account!.Id,
			};

			evento.Evento_Aula = new Evento_Aula
			{
				Turma_Id = null,
				Roteiro_Id = roteiro?.Id,
				Professor_Id = request.Professor_Id,
			};

			//
			// Insere Perfil Cognitivo
			//
			evento.Evento_Aula.Evento_Aula_PerfilCognitivo_Rel = request.PerfilCognitivo
				.Select(perfilId => new Evento_Aula_PerfilCognitivo_Rel
				{
					Evento_Aula_Id = evento.Id,
					PerfilCognitivo_Id = perfilId
				})
				.ToList();

			//
			// Inserir professores
			//
			evento.Evento_Participacao_Professor.Add(new Evento_Participacao_Professor()
			{
				Evento_Id = evento.Id,
				Professor_Id = professor.Id,
			});



			_db.Add(evento);
			_db.SaveChanges();

			//
			// Inserir alunos
			//
			var checklistsAtualizar = new List<Aluno_Checklist_Item>();
			var hoje = TimeFunctions.HoraAtualBR();

			foreach (var aluno in alunos)
			{
				if (requestAlunosPorAlunoId.TryGetValue(aluno.Id, out var reposicao))
				{
					var participacao = new Evento_Participacao_Aluno
					{
						Aluno_Id = aluno.Id,
						Evento_Id = evento.Id,
						Presente = null,
						ReposicaoDe_Evento_Id = reposicao.ReposicaoDe_Evento_Id,
						Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
						NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
						Apostila_AH_Id = aluno.Apostila_AH_Id,
						NumeroPaginaAH = aluno.NumeroPaginaAH,
					};
					evento.Evento_Participacao_Aluno.Add(participacao);

					if (aluno.PrimeiraAula_Id == reposicao.ReposicaoDe_Evento_Id)
					{
						var checklistAgendamento = checklistAgendamentoPorAlunoId[aluno.Id]
											.FirstOrDefault();

						eventosPorId.TryGetValue(reposicao.ReposicaoDe_Evento_Id, out var eventoSource);

						if (checklistAgendamento is not null)
						{
							var dataSource = eventoSource.Data.ToString("dd/MM/yyyy \'às\' HH:mm");
							var dataDest = evento.Data.ToString("dd/MM/yyyy \'às\' HH:mm");

							checklistAgendamento.Evento_Id = evento.Id;
							checklistAgendamento.DataFinalizacao = hoje;
							checklistAgendamento.Account_Finalizacao_Id = _account?.Id ?? 1;
							checklistAgendamento.Observacoes = $@"
								Checklist finalizado automaticamente. 
								<br> Aluno agendou reposicao da primeira aula do dia {dataSource} para o dia {dataDest}
								<br>
								<br> <b>Agendamento Inicial: </b>
								<br> Data: {dataSource}
								<br> Turma: {eventoSource.Descricao}
								<br>
								<br> <b>Agendamento Reposição: </b>
								<br> Data: {dataDest}
								<br> Turma: {evento.Descricao}
				";

							checklistsAtualizar.Add(checklistAgendamento);

						}
					}
				}


			}

			_db.Aluno_Checklist_Item.UpdateRange(checklistsAtualizar);
			_db.Update(evento);
			_db.SaveChanges();


			response.Message = "Aula extra criada com sucesso";
			response.Object = this.GetEventoById(evento.Id);
			response.Success = true;
		}

		catch (Exception ex)
		{
			response.Message = $"Falha ao registrar aula extra: {ex}";
		}

		return response;
	}

	public ResponseModel InsertAulaZero(CreateAulaZeroRequest request)
	{
		ResponseModel response = new() { Success = false };

		try
		{

			var professor = _db.ProfessorList
				.FirstOrDefault(p => p.Id == request.Professor_Id
										&& p.Active == true);

			if (professor is null)
				return new ResponseModel { Message = "Educador não encontrado" };

			// Sala deve estar livre no horário do evento
			bool isSalaOccupied = _salaService.IsSalaOccupied(
				request.Sala_Id,
				request.Data,
				request.DuracaoMinutos,
				null);

			if (isSalaOccupied)
				return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };

			// O professor associado não pode possuir conflitos de horário
			bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
				professorId: professor.Id,
				DiaSemana: (int)request.Data.DayOfWeek,
				Horario: request.Data.TimeOfDay,
				IgnoredTurmaId: null);

			if (hasTurmaConflict)
				return new ResponseModel { Message = $"Professor: '{professor.Nome}' possui uma turma nesse mesmo horário" };


			bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
				professorId: professor.Id,
				Data: request.Data,
				DuracaoMinutos: request.DuracaoMinutos,
				IgnoredEventoId: null
			);

			if (hasParticipacaoConflict)
				return new ResponseModel { Message = $"Professor: {professor.Nome} possui participação em outro evento nesse mesmo horário" };

			var participacaoOutraAulaZeroQueryable =
				from e in _db.Evento
				join p in _db.Evento_Participacao_Aluno
					on e.Id equals p.Evento_Id
				where e.Evento_Tipo_Id == (int)EventoTipo.AulaZero
					&& request.Alunos.Contains(p.Aluno_Id)
				select p;


			var participacaoOutraAulaZero = participacaoOutraAulaZeroQueryable
				.Include(x => x.Evento)
				.ToList();

			var participacaoFinalizada = participacaoOutraAulaZero
				.FirstOrDefault(x => x.Presente == true && x.Evento.Finalizado == true);

			if (participacaoFinalizada is not null)
			{
				var e = participacaoFinalizada.Evento;
				return new ResponseModel { Message = $"Aluno: '{participacaoFinalizada.Aluno}' já participou de aula zero no dia {e.Data.ToString("dd/MM/yyyy HH:mm")}." };
			}

			//
			// Validations passed
			//
			var hoje = TimeFunctions.HoraAtualBR();

			var roteiro = _db.Roteiro.FirstOrDefault(r => request.Data.Date >= r.DataInicio.Date && request.Data.Date <= r.DataFim.Date);

			var ab = roteiro?.Id;
			var evento = new Evento
			{
				Data = request.Data,
				Descricao = request.Descricao ?? "Aula Zero",
				Observacao = request.Observacao,
				Sala_Id = request.Sala_Id,
				DuracaoMinutos = request.DuracaoMinutos,

				Evento_Tipo_Id = (int)EventoTipo.AulaZero,
				CapacidadeMaximaAlunos = 12,
				Evento_Aula = new Evento_Aula
				{
					Turma_Id = null,
					Roteiro_Id = roteiro?.Id,
					Professor_Id = request.Professor_Id,
				},

				Created = hoje,
				LastUpdated = null,
				Deactivated = null,
				Finalizado = false,
				ReagendamentoDe_Evento_Id = null,
				Account_Created_Id = _account?.Id ?? 1
			};


			var alunos = _db.Aluno.Where(x => request.Alunos.Contains(x.Id))
				.ToList();

			var alunosPorId = alunos
				.ToDictionary(x => x.Id, x => x);

			var participacaoCancelar = participacaoOutraAulaZero
				.Where(x => (x.Presente == false || x.Presente == null) && x.Evento.Finalizado == false)
				.ToList();

			var historicoInserir = new List<Aluno_Historico>() { };

			//
			// Insere alunos
			//
			foreach (var alunoId in request.Alunos)
			{
				if (alunosPorId.TryGetValue(alunoId, out var aluno))
				{
					evento.Evento_Participacao_Aluno.Add(new Evento_Participacao_Aluno
					{
						Aluno_Id = aluno.Id,
						Evento_Id = evento.Id,
						Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
						NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
						Apostila_AH_Id = aluno.Apostila_AH_Id,
						NumeroPaginaAH = aluno.NumeroPaginaAH,
					});

					//
					// Insere histórico
					//
					historicoInserir.Add(new Aluno_Historico
					{
						Account_Id = _account?.Id ?? 1,
						Aluno_Id = aluno.Id,
						Data = hoje,
						Descricao = $"Aluno foi inscrito em um evento 'Aula Zero' no dia {request.Data:G}"
					});
				}
			}

			//
			// Insere professores 
			//
			evento.Evento_Participacao_Professor.Add(new Evento_Participacao_Professor
			{
				Professor_Id = professor.Id,
			});


			// 
			// Cancela outras participacoes não finalizadas
			//
			foreach (var participacao in participacaoCancelar)
			{
				participacao.Presente = false;
				participacao.Observacao = $"Cancelamento automático. <br> Uma nova aula zero foi agendada para o dia {request.Data.ToString("dd/MM/yyyy HH:mm")}";
				participacao.StatusContato_Id = 9;
				participacao.AlunoContactado = null;
				participacao.ContatoObservacao = null;
				participacao.ReposicaoDe_Evento_Id = null;
				participacao.Deactivated = hoje;

				if (participacao.Evento.Evento_Participacao_Aluno.Count == 1)
				{
					participacao.Evento.Deactivated = hoje;
					participacao.Evento.Observacao = $"Cancelamento automático. <br> Uma nova aula zero foi agendada para o dia {request.Data.ToString("dd/MM/yyyy HH:mm")}";
				}
			}
			_db.Add(evento);
			_db.SaveChanges();

			alunos.ForEach(aluno => { aluno.AulaZero_Id = evento.Id; });

			_db.Aluno_Historico.AddRange(historicoInserir);
			_db.Aluno.UpdateRange(alunos);

			_db.Evento_Participacao_Aluno.UpdateRange(participacaoCancelar);
			_db.Evento.UpdateRange(participacaoCancelar.Select(x => x.Evento));

			//
			// Finaliza checklists "agendamento aula zero"
			//
			var alunoChecklists =
				from a in alunos
				join rel in _db.Aluno_Checklist_Item
					on a.Id equals rel.Aluno_Id
				where rel.Checklist_Item_Id == (int)ChecklistItemId.AgendamentoAulaZero
				select rel;

			foreach (var checklistItem in alunoChecklists)
			{
				checklistItem.Account_Finalizacao_Id = _account?.Id ?? 1;
				checklistItem.DataFinalizacao = hoje;
				checklistItem.Observacoes = $"Checklist finalizado automaticamente. <br> Aluno agendou aula zero para o dia {request.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}.";
				checklistItem.Evento_Id = evento.Id;
			}
			_db.Aluno_Checklist_Item.UpdateRange(alunoChecklists);
			_db.SaveChanges();


			response.Message = "Aula zero criada com sucesso";
			response.Object = this.GetEventoById(evento.Id);
			response.Success = true;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao registrar aula zero: {ex}";
		}

		return response;
	}

	public ResponseModel Update(UpdateEventoRequest request)
	{
		ResponseModel response = new() { Success = false };
		try
		{
			Evento? evento = _db.Evento
				.Include(e => e.Evento_Tipo)
				.Include(e => e.Evento_Participacao_Aluno)
				.Include(e => e.Evento_Participacao_Professor)
					.ThenInclude(e => e.Professor)
				.FirstOrDefault(e => e.Id == request.Id);

			if (evento is null)
				return new ResponseModel { Message = $"Evento não encontrado." };

			if (evento.Finalizado)
				return new ResponseModel { Message = $"Não é possivel editar {evento.Evento_Tipo.Nome.ToLower()} que foi finalizada." };

			var professoresQueryable =
				from participacao in evento.Evento_Participacao_Professor
				join professor in _db.ProfessorList
					on participacao.Professor_Id equals professor.Id
				select professor;

			var professores = _db.ProfessorList
				.ToList();

			var participacaoPorProfessorId = evento.Evento_Participacao_Professor
				.ToDictionary(x => x.Professor_Id, x => x);

			var professoresPorId = professores
				.ToDictionary(x => x.Id, x => x);

			var professoresRequestIds = request.Professores
				.ToHashSet();

			#region validacao
			foreach (var professorId in request.Professores)
			{
				professoresPorId.TryGetValue(professorId, out var professor);
				if (professor == null)
					return new ResponseModel { Message = $"Professor: '{professorId}' não encontrado" };

				bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
					professorId: professor.Id,
					DiaSemana: (int)evento.Data.DayOfWeek,
					Horario: evento.Data.TimeOfDay,
					IgnoredTurmaId: request.Turma_Id
				);
				if (hasTurmaConflict)
					return new ResponseModel { Message = $"Professor: '{professor.Nome}' possui uma turma nesse mesmo horário" };

				participacaoPorProfessorId.TryGetValue(professor.Id, out var participacaoProfessor);

				bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
					professorId: professor.Id,
					Data: evento.Data,
					DuracaoMinutos: request.DuracaoMinutos,
					IgnoredEventoId: evento.Id
				);

				if (hasParticipacaoConflict)
					return new ResponseModel { Message = $"Professor: {professor.Nome} possui participação em outro evento nesse mesmo horário" };
			}

			// Não devo poder registrar um evento em uma sala que não existe
			bool salaExists = _db.Salas.Any(s => s.Id == request.Sala_Id);
			if (!salaExists)
				return new ResponseModel { Message = "Sala não encontrada" };

			// Sala deve estar livre no horário do evento
			bool isSalaOccupied = _salaService.IsSalaOccupied(request.Sala_Id, evento.Data, request.DuracaoMinutos, evento.Id);
			if (isSalaOccupied)
				return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };

			#endregion

			// Validations passed

			var oldObject = this.GetEventoById(request.Id);

			evento.Observacao = request.Observacao;
			evento.Descricao = request.Descricao;	
			evento.Sala_Id = request.Sala_Id;
			evento.DuracaoMinutos = request.DuracaoMinutos;
			evento.CapacidadeMaximaAlunos = request.CapacidadeMaximaAlunos;
			evento.LastUpdated = TimeFunctions.HoraAtualBR();


			//
			// Inativa professores removidos
			//
			var participacoesToDeactivate = evento.Evento_Participacao_Professor
				.Where(p => !professoresRequestIds.TryGetValue(p.Professor_Id, out var participacao))
				.ToList();

			foreach (var participacao in participacoesToDeactivate)
			{
				participacao.Deactivated = TimeFunctions.HoraAtualBR();
			}

			//
			// Insere professores adicionados no evento
			//
			var participacoesToAdd = professoresRequestIds
				.Where(p => !participacaoPorProfessorId.TryGetValue(p, out var participacao))
				.ToList();

			foreach (int professorId in participacoesToAdd)
			{
				evento.Evento_Participacao_Professor.Add(new Evento_Participacao_Professor
				{
					Evento_Id = evento.Id,
					Professor_Id = professorId
				});
			}

			_db.Evento_Participacao_Professor.UpdateRange(evento.Evento_Participacao_Professor);

			_db.Update(evento);
			_db.SaveChanges();


			response.Object = this.GetEventoById(evento.Id);
			response.Message = $"{evento.Evento_Tipo.Nome} atualizada com sucesso";
			response.OldObject = oldObject;
			response.Success = true;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao atualizar evento ID: '{request.Id}' | {ex}";
		}

		return response;
	}

	public ResponseModel Cancelar(CancelarEventoRequest request)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Evento? evento = _db.Evento
				.Include(x => x.Evento_Participacao_Aluno)
				.ThenInclude(x => x.Aluno)
				.Include(x => x.Evento_Participacao_Professor)
				.FirstOrDefault(e => e.Id == request.Id);

			if (evento is null)
				return new ResponseModel { Message = "Evento não encontrado." };

			if (evento.Deactivated.HasValue)
				return new ResponseModel { Message = "Evento já está cancelado" };

			//
			// Validations passed
			//

			var participacaoAlunos = evento.Evento_Participacao_Aluno
				.ToList();

			var alunosIds = participacaoAlunos
				.Select(x => x.Aluno_Id)
				.Distinct()
				.ToHashSet();

			var alunos = _db.Aluno
				.Where(x => alunosIds.Contains(x.Id))
				.ToList();

			var checklistAtualizar = _db.Aluno_Checklist_Item
				.Where(x => x.DataFinalizacao == null
					&& x.Evento_Id == evento.Id
					&& alunosIds.Contains(x.Aluno_Id)
					&& (x.Checklist_Item_Id == (int)ChecklistItemId.AgendamentoPrimeiraAula
					  || x.Checklist_Item_Id == (int)ChecklistItemId.AgendamentoAulaZero
					  || x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Oficina
					  || x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Oficina
					  || x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Superacao
					  || x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Superacao
					))
				.ToList();

			var alunosAtualizar = alunos
				.Where(x => x.PrimeiraAula_Id == evento.Id || x.AulaZero_Id == evento.Id)
				.ToList();

			var alunosPorId = alunosAtualizar
				.ToDictionary(x => x.Id, x => x);

			foreach (var item in checklistAtualizar)
			{
				item.DataFinalizacao = null;
				item.Account_Finalizacao_Id = null;
				item.Evento_Id = null;
			}

			// Remove PrimeiraAUla_Id e AulaZero_Id de alunos
			foreach (var participacao in participacaoAlunos)
			{
				participacao.StatusContato_Id = (int)StatusContato.AULA_CANCELADA;

				if (alunosPorId.TryGetValue(participacao.Aluno_Id, out var aluno))
				{
					if (aluno.PrimeiraAula_Id == evento.Id)
						aluno.PrimeiraAula_Id = null;

					if (aluno.AulaZero_Id == evento.Id)
						aluno.AulaZero_Id = null;
				}
			}


			evento.Deactivated = TimeFunctions.HoraAtualBR();
			evento.Observacao = request.Observacao;

			_db.Aluno_Checklist_Item.UpdateRange(checklistAtualizar);
			_db.Aluno.UpdateRange(alunosAtualizar);
			_db.Update(evento);
			_db.SaveChanges();

			response.Message = $"Evento foi cancelado com sucesso";
			response.Object = this.GetEventoById(evento.Id);
			response.Success = true;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao cancelar evento: {ex}";
		}

		return response;
	}

	public ResponseModel Finalizar(FinalizarEventoRequest request)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			var evento = _db.Evento
				.Include(e => e.Evento_Tipo)
				.Include(e => e.Evento_Participacao_Professor)
				.Include(e => e.Evento_Participacao_Aluno)
				.ThenInclude(e => e.Aluno)
				.ThenInclude(e => e.Aluno_Checklist_Items)
				.FirstOrDefault(e => e.Id == request.Evento_Id);

			if (evento is null)
				return new ResponseModel { Message = "Evento não encontrado." };

			var eventValidation = EventoUtils.ValidateEvent(evento);

			if (!eventValidation.Success)
				return eventValidation;

			var reposicaoIds = request.Alunos
				.Select(a => a.ReposicaoDe_Evento_Id)
				.Where(id => id.HasValue)
				.Select(id => id!.Value)
				.Distinct();

			var eventosExistentes = _db.Evento
				.Where(e => reposicaoIds.Contains(e.Id))
				.Select(e => e.Id)
				.ToList();

			var idsSemEvento = reposicaoIds.Except(eventosExistentes);

			if (idsSemEvento.Any())
				return new ResponseModel { Message = $"Uma ou mais reposições estão sem Evento associado: {string.Join(", ", idsSemEvento)}" };

			var existingApostilas = _db.Apostila.ToList();

			List<int> apostilasAbacoIds = request.Alunos
				.Where(x => x.Apostila_Abaco_Id.HasValue)
				.Select(p => p.Apostila_Abaco_Id!.Value)
				.ToList();

			List<int> apostilasAhIds = request.Alunos
				.Where(x => x.Apostila_Ah_Id.HasValue)
				.Select(p => p.Apostila_Ah_Id!.Value)
				.ToList();

			var validateApostilasAh = ValidateApostilas(apostilasAhIds, existingApostilas, "AH");
			var validateApostilasAbaco = ValidateApostilas(apostilasAbacoIds, existingApostilas, "Abaco");

			if (!validateApostilasAh.Success)
				return validateApostilasAh;

			if (!validateApostilasAbaco.Success)
				return validateApostilasAbaco;

			// Validations passed

			var participacaoAlunoPorId = evento.Evento_Participacao_Aluno
				.ToDictionary(x => x.Id, x => x);

			var participacaoProfessorPorId = evento.Evento_Participacao_Professor
				.ToDictionary(x => x.Id, x => x);

			var checklistComparecimentoPorAlunoId = evento.Evento_Participacao_Aluno
				.SelectMany(x => x.Aluno.Aluno_Checklist_Items)
				.Where(x => x.Checklist_Item_Id == (int)ChecklistItemId.Comparecimento1Oficina
							|| x.Checklist_Item_Id == (int)ChecklistItemId.Comparecimento2Oficina
							|| x.Checklist_Item_Id == (int)ChecklistItemId.Comparecimento1Superacao
							|| x.Checklist_Item_Id == (int)ChecklistItemId.Comparecimento2Superacao
							|| x.Checklist_Item_Id == (int)ChecklistItemId.ComparecimentoAulaZero
							|| x.Checklist_Item_Id == (int)ChecklistItemId.ComparecimentoPrimeiraAula
				)
				.ToLookup(x => x.Aluno_Id);

			var checklistsAgendamentoPorAlunoId = evento.Evento_Participacao_Aluno
				.SelectMany(x => x.Aluno.Aluno_Checklist_Items)
				.Where(x => x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Oficina
							|| x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Oficina
							|| x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Superacao
							|| x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Superacao
							|| x.Checklist_Item_Id == (int)ChecklistItemId.AgendamentoAulaZero
							|| x.Checklist_Item_Id == (int)ChecklistItemId.AgendamentoPrimeiraAula
				)
				.ToLookup(x => x.Aluno_Id);

			var historicosInserir = new List<Aluno_Historico>() { };
			var checklistAtualizar = new List<Aluno_Checklist_Item>() { };
			var alunosAtualizar = new List<Aluno>() { };
			var hoje = TimeFunctions.HoraAtualBR();
			// 
			// Atualiza alunos
			//
			foreach (ParticipacaoAlunoModel participacaoModel in request.Alunos)
			{
				participacaoAlunoPorId.TryGetValue(participacaoModel.Participacao_Id, out var participacao);

				if (participacao is null)
					return new ResponseModel { Message = $"Participação de aluno no evento ID: '{evento.Id}' Participacao_Id: '{participacaoModel.Participacao_Id}' não foi encontrada" };

				participacao.Observacao = participacaoModel.Observacao;
				participacao.Presente = participacaoModel.Presente;
				participacao.ReposicaoDe_Evento_Id = participacaoModel.ReposicaoDe_Evento_Id;
				participacao.StatusContato_Id = CalcularStatusContato(participacaoModel.Presente, participacaoModel.ReposicaoDe_Evento_Id);

				if (participacaoModel.Presente)
				{
					//
					// Atualizar participação e aluno
					//
					participacao.Apostila_Abaco_Id = participacaoModel.Apostila_Abaco_Id;
					participacao.NumeroPaginaAbaco = participacaoModel.NumeroPaginaAbaco;
					participacao.Apostila_AH_Id = participacaoModel.Apostila_Ah_Id;
					participacao.NumeroPaginaAH = participacaoModel.NumeroPaginaAh;

					participacao.Aluno.Apostila_Abaco_Id = participacaoModel.Apostila_Abaco_Id;
					participacao.Aluno.NumeroPaginaAbaco = participacaoModel.NumeroPaginaAbaco;
					participacao.Aluno.Apostila_AH_Id = participacaoModel.Apostila_Ah_Id;
					participacao.Aluno.NumeroPaginaAH = participacaoModel.NumeroPaginaAh;

					alunosAtualizar.Add(participacao.Aluno);

					//
					// Atualiza checklist se for oficina ou superação
					//

					var checklistAgendamento = checklistsAgendamentoPorAlunoId[participacao.Aluno_Id];

					var checklistComparecimento = checklistComparecimentoPorAlunoId[participacao.Aluno_Id];

					Aluno_Checklist_Item? itemAgendamento = null;

					Aluno_Checklist_Item? itemComparecimento = null;

					if (evento.Evento_Tipo_Id == (int)EventoTipo.Superacao)
					{

						itemAgendamento = checklistAgendamento
							.FirstOrDefault(x => x.Evento_Id == evento.Id
													&& x.DataFinalizacao != null
													&& (x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Superacao
													|| x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Superacao));

						if (itemAgendamento is not null && itemAgendamento.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Superacao)
						{
							itemComparecimento = checklistComparecimento
								.FirstOrDefault(x => x.Checklist_Item_Id == (int)ChecklistItemId.Comparecimento1Superacao);
						}
						else if (itemAgendamento is not null && itemAgendamento.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Superacao)
						{
							itemComparecimento = checklistComparecimento
								.FirstOrDefault(x => x.Checklist_Item_Id == (int)ChecklistItemId.Comparecimento2Superacao);
						}

					}
					else if (evento.Evento_Tipo_Id == (int)EventoTipo.Oficina)
					{
						itemAgendamento = checklistAgendamento
							.FirstOrDefault(x => x.Evento_Id == evento.Id
													&& x.DataFinalizacao != null
													&& (x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Oficina
													|| x.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Oficina));

						if (itemAgendamento is not null && itemAgendamento.Checklist_Item_Id == (int)ChecklistItemId.Agendamento1Oficina)
						{
							itemComparecimento = checklistComparecimento
								.FirstOrDefault(x => x.Checklist_Item_Id == (int)ChecklistItemId.Comparecimento1Oficina);
						}
						else if (itemAgendamento is not null && itemAgendamento.Checklist_Item_Id == (int)ChecklistItemId.Agendamento2Oficina)
						{
							itemComparecimento = checklistComparecimento
								.FirstOrDefault(x => x.Checklist_Item_Id == (int)ChecklistItemId.Comparecimento2Oficina);
						}
					}
					else if (evento.Evento_Tipo_Id == (int)EventoTipo.AulaZero)
					{
						itemComparecimento = checklistComparecimento
							.FirstOrDefault(x => x.Checklist_Item_Id == (int)ChecklistItemId.ComparecimentoAulaZero);

					}
					else if (evento.Id == participacao.Aluno.PrimeiraAula_Id)
					{
						itemComparecimento = checklistComparecimento
							.FirstOrDefault(x => x.Checklist_Item_Id == (int)ChecklistItemId.ComparecimentoPrimeiraAula);
					}


					if (itemComparecimento is not null)
					{
						var item = itemComparecimento;
						item.Evento_Id = request.Evento_Id;
						item.Account_Finalizacao_Id = _account?.Id ?? 1;
						item.DataFinalizacao = hoje;
						item.Observacoes = $"Checklist finalizado automaticamente. <br> Aluno compareceu na {evento.Evento_Tipo?.Nome ?? "Oficina"} do dia {evento.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}.";
						checklistAtualizar.Add(item);
					}
				}

				historicosInserir.Add(new Aluno_Historico
				{
					Account_Id = _account!.Id,
					Aluno_Id = participacao.Aluno_Id,
					Data = hoje,
					Descricao = participacaoModel.Presente ?
							$"Aluno compareceu na {evento.Evento_Tipo.Nome.ToLower()} agendada no dia {evento.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}" :
							$"Aluno NÃO compareceu na {evento.Evento_Tipo.Nome.ToLower()} agendada no dia {evento.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}",
				});
			}

			// 
			// Atualiza professores
			//
			foreach (var participacaoModel in request.Professores)
			{
				participacaoProfessorPorId.TryGetValue(participacaoModel.Participacao_Id, out var participacao);

				if (participacao is null)
					return new ResponseModel { Message = $"Participação de professor no evento ID: '{evento.Id}' Participacao_Id: '{participacaoModel.Participacao_Id}' não foi encontrada" };

				participacao.Observacao = participacaoModel.Observacao;
				participacao.Presente = participacaoModel.Presente;

			}

			//
			// Atualiza evento
			//
			evento!.Observacao = request.Observacao;
			evento.Finalizado = true;
			evento.LastUpdated = hoje;

			_db.Aluno_Historico.AddRange(historicosInserir);
			_db.Aluno_Checklist_Item.UpdateRange(checklistAtualizar);
			_db.Aluno.UpdateRange(alunosAtualizar);

			_db.Update(evento);

			_db.SaveChanges();

			response.Message = $"Evento foi finalizado com sucesso.";
			response.Object = this.GetEventoById(evento.Id);
			response.Success = true;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao finalizar evento: {ex}";
		}

		return response;
	}

	public ResponseModel FinalizarAulaZero(FinalizarAulaZeroRequest request)
	{
		ResponseModel response = new ResponseModel { Success = false };

		try
		{
			var participacoesQueryable = _db.Evento_Participacao_Aluno
					.Where(x => x.Evento_Id == request.Evento_Id);

			var alunosQueryable =
					from aluno in _db.Aluno
					join participacao in participacoesQueryable
						on aluno.Id equals participacao.Aluno_Id
					select aluno;

			var alunosChecklistItemQueryable =
					from checklist in _db.Aluno_Checklist_Item
					join participacao in participacoesQueryable
					on checklist.Aluno_Id equals participacao.Aluno_Id
					where checklist.Checklist_Item_Id == (int)ChecklistItemId.ComparecimentoAulaZero
						&& checklist.DataFinalizacao == null
					select checklist;

			var vigenciasQueryable =
				from vigencia in _db.Aluno_Turma_Vigencia
				join participacao in participacoesQueryable
					on vigencia.Aluno_Id equals participacao.Aluno_Id
				select vigencia;

			// Materialização

			var evento = _db.Evento
				.Include(x => x.Evento_Participacao_Aluno)
				.FirstOrDefault(x => x.Id == request.Evento_Id);

			if (evento is null)
				throw new Exception("Evento não encontrado.");

			var alunos = alunosQueryable
				.ToList();

			var kitsIds = request.Alunos
				.Where(x => x.Presente)
				.Select(x => x.Apostila_Kit_Id)
				.ToHashSet();

			var kitApostila = _db.Apostila_Kit
				.Where(x => kitsIds.Contains(x.Id))
				.Include(x => x.Apostila_Kit_Rel)
				.ThenInclude(x => x.Apostila)
				.ToList();

			var vigencias = vigenciasQueryable
				.ToList();

			var participacoes = participacoesQueryable
				.ToList();

			// Dicionarios

			var kitApostilaPorId = kitApostila
				.ToDictionary(x => x.Id, x => x);

			var alunosPorId = alunos
				.ToDictionary(x => x.Id, x => x);

			var checklistPorAluno = alunosChecklistItemQueryable
				.ToLookup(x => x.Aluno_Id);

			var vigenciasPorAluno = vigencias
				.ToLookup(x => x.Aluno_Id);

			var historicoInserir = new List<Aluno_Historico>() { };

			var participacaoPorId = evento
				.Evento_Participacao_Aluno
				.ToDictionary(x => x.Id, x => x);

			foreach (var participacaoRequest in request.Alunos)
			{
				if (alunosPorId.TryGetValue(participacaoRequest.Aluno_Id, out var aluno))
				{
					DateTime hoje = TimeFunctions.HoraAtualBR();

					//
					// Insere Histórico
					//
					#region historico

					historicoInserir.Add(new Aluno_Historico
					{
						Account_Id = _account?.Id ?? 1,
						Aluno_Id = participacaoRequest.Aluno_Id,
						Descricao = participacaoRequest.Presente ?
								$"Aluno compareceu na aula zero agendada no dia {evento.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}" :
								$"Aluno NÃO compareceu na aula zero agendada no dia{evento.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}",
						Data = hoje,
					});

					#endregion

					if (participacaoRequest.Presente == true)
					{
						//
						// Salva os dados no aluno
						//
						#region salva aluno
						Apostila_Kit? kit;
						kitApostilaPorId.TryGetValue(participacaoRequest.Apostila_Kit_Id, out kit);

						if (kit is null)
							throw new Exception($"Kit de apostila não encontrada para o aluno: {aluno.Id}");

						var apostilaAbaco = kit.Apostila_Kit_Rel
							.Select(x => x.Apostila)
							.Where(x => x.Apostila_Tipo_Id == (int)ApostilaTipo.Abaco)
							.OrderBy(x => x.Ordem)
							.FirstOrDefault();

						var apostilaAH = kit.Apostila_Kit_Rel
							.Select(x => x.Apostila)
							.Where(x => x.Apostila_Tipo_Id == (int)ApostilaTipo.AH)
							.OrderBy(x => x.Ordem)
							.FirstOrDefault();

						aluno.PerfilCognitivo_Id = participacaoRequest.PerfilCognitivo_Id;
						aluno.Apostila_Kit_Id = participacaoRequest.Apostila_Kit_Id;
						aluno.Turma_Id = participacaoRequest.Turma_Id;
						aluno.Apostila_Abaco_Id = apostilaAbaco?.Id;
						aluno.Apostila_AH_Id = apostilaAH?.Id;
						aluno.NumeroPaginaAbaco = 0;
						aluno.NumeroPaginaAH = 0;
						_db.Aluno.Update(aluno);

						#endregion

						//
						// Atualiza checklist "comparecimento aula zero"
						//
						#region checklist

						var item = checklistPorAluno[aluno.Id].FirstOrDefault();
						if (item is not null)
						{
							item.Evento_Id = evento.Id;
							item.Account_Finalizacao_Id = _account?.Id ?? 1;
							item.DataFinalizacao = TimeFunctions.HoraAtualBR();
							item.Observacoes = $"Checklist finalizado automaticamente. <br> Aluno compareceu na aula zero do dia {evento?.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}.";
							_db.Aluno_Checklist_Item.Update(item);
						}

						#endregion

						//
						// Insere vigencia
						//
						#region insere vigencia

						var vigenciasDoAluno = vigenciasPorAluno[aluno.Id];

						var vigenciasAnteriores = vigenciasDoAluno
							.Where(x => x.DataFimVigencia == null)
							.ToList();

						vigenciasAnteriores.ForEach(vigencia =>
						{
							vigencia.DataFimVigencia = hoje;
						});
						_db.Aluno_Turma_Vigencia.UpdateRange(vigenciasAnteriores);


						_db.Aluno_Turma_Vigencia.Add(new Aluno_Turma_Vigencia
						{
							Account_Id = _account?.Id ?? 1,
							Aluno_Id = participacaoRequest.Aluno_Id,
							Turma_Id = participacaoRequest.Turma_Id,
							DataInicioVigencia = hoje,
						});

						#endregion

					}
					else
					{
						aluno.AulaZero_Id = null;
						_db.Aluno.Update(aluno);
					}

					//
					// Salva participacao
					//
					if (participacaoPorId.TryGetValue(participacaoRequest.Participacao_Id, out var participacao))
					{
						participacao.Presente = participacaoRequest.Presente;
					}

				}
			}


			//
			// Finaliza Aula Zero
			//
			evento.Observacao = request.Observacao;
			evento.Finalizado = true;

			_db.Aluno_Historico.AddRange(historicoInserir);
			_db.Update(evento);

			_db.SaveChanges();

			response.Success = true;
			response.Message = "Aula zero finalizada com sucesso.";
			response.Object = this.GetEventoById(evento.Id);

		}
		catch (Exception e)
		{
			response.Message = "Não foi possível finalizar aula zero: " + e.Message;
			response.Success = false;
		}

		return response;
	}

	public ResponseModel AgendarReposicao(ReposicaoRequest request)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			var aluno = _db.Aluno
				.FirstOrDefault(a => a.Id == request.Aluno_Id);

			if (aluno is null)
				return new ResponseModel { Message = "Aluno não encontrado" };


			var eventoSource = _db.Evento
				.Include(x => x.Evento_Participacao_Aluno)
				.Include(x => x.Evento_Participacao_Aluno)
				.Include(x => x.Evento_Aula)
				.ThenInclude(x => x.Evento_Aula_PerfilCognitivo_Rel)
				.FirstOrDefault(e => e.Id == request.Source_Aula_Id);

			var eventoDest = _db.Evento
				.Include(x => x.Evento_Participacao_Aluno)
				.Include(x => x.Evento_Aula)
				.ThenInclude(x => x.Evento_Aula_PerfilCognitivo_Rel)
				.FirstOrDefault(e => e.Id == request.Dest_Aula_Id);

			if (eventoSource is null || eventoSource.Evento_Aula is null)
				return new ResponseModel { Message = "Aula não encontrada" };

			if (eventoDest is null || eventoDest.Evento_Aula is null)
				return new ResponseModel { Message = "Aula não encontrada" };

			if (request.Source_Aula_Id == request.Dest_Aula_Id)
				return new ResponseModel { Message = "Aula original e aula destino não podem ser iguais" };

			if (eventoDest.Finalizado)
				return new ResponseModel { Message = "Não é possível marcar reposição para uma aula finalizada" };

			if (eventoDest.Deactivated != null)
				return new ResponseModel { Message = "Não é possível marcar reposição em uma aula desativada" };

			if (Math.Abs((eventoDest.Data - eventoSource.Data).TotalDays) > 30)
				return new ResponseModel { Message = "Não é possível marcar reposição em uma aula com mais de 30 dias de intervalo." };

			if (eventoDest.Evento_Aula.Turma_Id.HasValue)
			{
				if (eventoSource.Evento_Aula.Turma_Id == eventoDest.Evento_Aula.Turma_Id)
					return new ResponseModel { Message = "Aluno não pode repor aula na própria turma" };
			}

			bool registroAlreadyExists = eventoDest.Evento_Participacao_Aluno.Any(p => p.Aluno_Id == aluno.Id);
			if (registroAlreadyExists)
				return new ResponseModel { Message = "Aluno já está cadastrado no evento destino" };

			// A aula destino e o aluno devem compartilhar pelo menos um perfil cognitivo
			bool perfilCognitivoMatches = _db.Evento_Aula_PerfilCognitivo_Rel
				.Any(ep =>
					ep.Evento_Aula_Id == eventoDest.Id &&
					ep.PerfilCognitivo_Id == aluno.PerfilCognitivo_Id);

			if (perfilCognitivoMatches == false && aluno.PerfilCognitivo_Id.HasValue)
				return new ResponseModel { Message = "O perfil cognitivo da aula não é adequado para este aluno" };

			var eventoListDest = _db.CalendarioEventoList.FirstOrDefault(x => x.Id == eventoDest.Id);
			int registrosAtivos = eventoListDest?.AlunosAtivosEvento ?? 0;

			// O evento deve ter espaço para comportar o aluno
			if (registrosAtivos >= eventoDest.CapacidadeMaximaAlunos)
				return new ResponseModel { Message = "Esse evento de aula já está em sua capacidade máxima." };

			var participacaoSource = eventoSource.Evento_Participacao_Aluno
				.FirstOrDefault(p =>
					p.Deactivated == null
					&& p.Aluno_Id == aluno.Id
					&& p.Evento_Id == eventoSource.Id);

			if (participacaoSource is null)
				return new ResponseModel { Message = "Aluno não encontrado." };

			if (participacaoSource.Presente == true)
				return new ResponseModel { Message = "Não é possível marcar reposição de uma aula que o aluno esteve presente." };

			//
			// Validations passed
			//

			// Se for a primeira aula do aluno, atualizar a data de primeira aula para a data da aula destino
			if (eventoSource.Id == aluno.PrimeiraAula_Id)
			{
				var hoje = TimeFunctions.HoraAtualBR();

				aluno.PrimeiraAula_Id = eventoDest.Id;

				var checklistsAtualizar = _db.Aluno_Checklist_Item
					.Where(x => x.Aluno_Id == request.Aluno_Id
						&& x.Evento_Id == eventoSource.Id
						&& (x.Checklist_Item_Id == (int)ChecklistItemId.AgendamentoPrimeiraAula
						|| x.Checklist_Item_Id == (int)ChecklistItemId.ComparecimentoPrimeiraAula))
					.ToList();

				var checklistAgendamento = checklistsAtualizar
					.FirstOrDefault(x => x.Checklist_Item_Id == (int)ChecklistItemId.AgendamentoPrimeiraAula);

				var checklistComparecimento = checklistsAtualizar
					.FirstOrDefault(x => x.Checklist_Item_Id == (int)ChecklistItemId.ComparecimentoPrimeiraAula);

				if (checklistAgendamento is not null)
				{
					var dataSource = eventoSource.Data.ToString("dd/MM/yyyy \'às\' HH:mm");
					var dataDest = eventoDest.Data.ToString("dd/MM/yyyy \'às\' HH:mm");

					checklistAgendamento.Evento_Id = eventoDest.Id;
					checklistAgendamento.DataFinalizacao = hoje;
					checklistAgendamento.Account_Finalizacao_Id = _account?.Id ?? 1;
					checklistAgendamento.Observacoes = $@"
								Checklist finalizado automaticamente. 
								<br> Aluno agendou reposicao da primeira aula do dia {dataSource} para o dia {dataDest}
								<br>
								<br> <b>Agendamento Inicial: </b>
								<br> Data: {dataSource}
								<br> Turma: {eventoSource.Descricao}
								<br>
								<br> <b>Agendamento Reposição: </b>
								<br> Data: {dataDest}
								<br> Turma: {eventoDest.Descricao}
				";

					_db.Aluno_Checklist_Item.Update(checklistAgendamento);
				}

				if (checklistComparecimento is not null)
				{
					var dataSource = eventoSource.Data.ToString("dd/MM/yyyy \'às\' HH:mm");
					var dataDest = eventoDest.Data.ToString("dd/MM/yyyy \'às\' HH:mm");

					checklistComparecimento.Evento_Id = eventoDest.Id;
					checklistComparecimento.DataFinalizacao = null;
					checklistComparecimento.Account_Finalizacao_Id = null;
					checklistComparecimento.Observacoes = null;

					_db.Aluno_Checklist_Item.Update(checklistComparecimento);
				}



			}



			// Amarrar o novo registro à aula sendo reposta
			var participacaoDest = new Evento_Participacao_Aluno()
			{
				Aluno_Id = aluno.Id,
				Evento_Id = eventoDest.Id,
				ReposicaoDe_Evento_Id = eventoSource.Id,
				Observacao = request.Observacao,
				Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
				NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
				Apostila_AH_Id = aluno.Apostila_AH_Id,
				NumeroPaginaAH = aluno.NumeroPaginaAH,
			};

			// Desativar o registro da aula
			participacaoSource.Presente = false;
			participacaoSource.Deactivated = TimeFunctions.HoraAtualBR();
			participacaoSource.StatusContato_Id = (int)StatusContato.REPOSICAO_AGENDADA;

			_db.Evento_Participacao_Aluno.Update(participacaoSource);
			_db.Evento_Participacao_Aluno.Add(participacaoDest);
			_db.Aluno.Update(aluno);
			_db.Aluno_Historico.Add(new Aluno_Historico
			{
				Aluno_Id = aluno.Id,
				Descricao = $"O aluno agendou reposição do dia '{eventoSource.Data:G}' para o dia '{eventoDest.Data:G}' com a turma {eventoDest.Evento_Aula?.Turma_Id.ToString() ?? "Extra"}",
				Account_Id = _account!.Id,
				Data = TimeFunctions.HoraAtualBR(),
			});

			_db.SaveChanges();

			response.Success = true;
			response.Object = new
			{
				dest = this.GetEventoById(eventoDest.Id),
				source = this.GetEventoById(eventoSource.Id),
			};
			response.Message = "Reposição agendada com sucesso";
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao inserir reposição de aula do aluno: {ex}";
		}

		return response;
	}

	public ResponseModel AgendarPrimeiraAula(PrimeiraAulaRequest request)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Aluno? aluno = _db.Aluno
				.Include(a => a.PrimeiraAula)
				.ThenInclude(a => a.Evento_Participacao_Aluno)
				.FirstOrDefault(a => a.Id == request.Aluno_Id);

			Evento? evento = _db.Evento
				.Include(e => e.Evento_Participacao_Aluno)
				.Include(e => e.Evento_Aula)
				.ThenInclude(e => e.Evento_Aula_PerfilCognitivo_Rel)
				.FirstOrDefault(e => e.Id == request.Evento_Id);


			if (evento is null)
				return new ResponseModel { Message = "Evento não encontrado" };

			if (aluno is null)
				return new ResponseModel { Message = "Aluno não encontrado" };

			if (aluno.Deactivated.HasValue && aluno.Deactivated.Value.Date <= evento.Data.Date)
				return new ResponseModel { Message = "O aluno está desativado" };

			if (evento is null || evento.Evento_Aula is null)
				return new ResponseModel { Message = "Evento não encontrado" };

			//if (evento.Finalizado == true)
			//	return new ResponseModel { Message = "Não foi possível continuar. Esta aula já está finalizada." };

			if (evento.Deactivated != null)
				return new ResponseModel { Message = "Não foi possível continuar. Esta aula foi cancelada." };

			if (aluno.PrimeiraAula_Id.HasValue && aluno.PrimeiraAula != null)
			{
				var participacao = aluno.PrimeiraAula.Evento_Participacao_Aluno.FirstOrDefault(x => x.Evento_Id == aluno.PrimeiraAula_Id.Value);
				var primeiraAula = aluno.PrimeiraAula;

				if (participacao?.Presente == true)
					return new ResponseModel { Message = $"Aluno já participou da primeira aula no dia: {primeiraAula.Data.ToString("dd/MM/yyyy HH:mm")}" };
			}


			// O aluno deve se encaixar em um dos perfis cognitivos do evento
			bool perfilCognitivoMatches = evento.Evento_Aula.Evento_Aula_PerfilCognitivo_Rel
				.Any(x =>
					x.Evento_Aula_Id == evento.Id &&
					x.PerfilCognitivo_Id == aluno.PerfilCognitivo_Id);

			if (perfilCognitivoMatches == false && aluno.PerfilCognitivo_Id is not null)
				return new ResponseModel { Message = "O perfil cognitivo da aula não é adequado para este aluno." };


			// Se o aluno já estiver no evento, precisa apenas marcar como primeira aula
			if (!evento.Evento_Participacao_Aluno.Any(a => a.Aluno_Id == aluno.Id))
			{
				var eventoList = _db.CalendarioEventoList.FirstOrDefault(x => x.Id == request.Evento_Id);

				// O evento deve ter espaço para comportar o aluno
				if (eventoList is null || eventoList.AlunosAtivosEvento >= evento.CapacidadeMaximaAlunos)
					return new ResponseModel { Message = "Essa aula já está em sua capacidade máxima." };

				var participacao = new Evento_Participacao_Aluno()
				{
					Aluno_Id = aluno.Id,
					Evento_Id = evento.Id,
					Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
					NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
					Apostila_AH_Id = aluno.Apostila_AH_Id,
					NumeroPaginaAH = aluno.NumeroPaginaAH,
				};

				_db.Evento_Participacao_Aluno.Add(participacao);
			}

			var hoje = TimeFunctions.HoraAtualBR();
			var historico = new Aluno_Historico()
			{
				Aluno_Id = aluno.Id,
				Descricao = $"O aluno teve primeira aula agendada para o dia '{evento.Data:G}'",
				Account_Id = _account?.Id ?? 1,
				Data = hoje,
			};

			aluno.PrimeiraAula_Id = request.Evento_Id;

			_db.Aluno.Update(aluno);
			_db.Aluno_Historico.Add(historico);

			var checklistItem = _db.Aluno_Checklist_Item
				.FirstOrDefault(x => x.DataFinalizacao == null
									&& x.Aluno_Id == request.Aluno_Id
									&& x.Checklist_Item_Id == (int)ChecklistItemId.AgendamentoPrimeiraAula);

			if (checklistItem is not null)
			{

				checklistItem.Account_Finalizacao_Id = _account?.Id ?? 1;
				checklistItem.DataFinalizacao = hoje;
				checklistItem.Observacoes = $"Checklist finalizado automaticamente. <br> Aluno agendou aula zero para o dia {evento.Data.ToString("dd/MM/yyyy \'às\' HH:mm")}.";
				checklistItem.Evento_Id = evento.Id;
				_db.Aluno_Checklist_Item.Update(checklistItem);
			}



			_db.SaveChanges();

			response.Success = true;
			response.Object = this.GetEventoById(evento.Id);
			response.Message = "Primeira aula agendada com sucesso";
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao inserir primeira aula do aluno: {ex}";
		}

		return response;
	}

	// Metodos de validacao/suporte


	private ResponseModel ValidateApostilas(List<int> apostilaIds, List<Apostila> existingApostilas, string? type)
	{
		foreach (int apostilaId in apostilaIds)
		{
			var matchingApostila = existingApostilas.FirstOrDefault(a => a.Id == apostilaId);

			if (matchingApostila is null)
			{
				return new ResponseModel { Message = $"Apostila {type} ID: '{apostilaId}' não existe" };
			}
		}

		return new ResponseModel { Success = true };
	}

	private ResponseModel ValidateParticipacao(ParticipacaoAlunoModel participacaoAluno, Evento evento, List<Apostila> existingApostilas)
	{
		Evento_Participacao_Aluno? participacao = evento.Evento_Participacao_Aluno
						.FirstOrDefault(p => p.Id == participacaoAluno.Participacao_Id);

		if (participacao is null)
		{
			return new ResponseModel { Message = $"Participação de aluno no evento ID: '{evento.Id}' Participacao_Id: '{participacaoAluno.Participacao_Id}' não foi encontrada" };
		}

		// Não deve ser possível atualizar além do tamanho máximo da apostila
		int totalPaginasAbaco = existingApostilas.Find(a => a.Id == participacaoAluno.Apostila_Abaco_Id)!.NumeroTotalPaginas;
		int totalPaginasAh = existingApostilas.Find(a => a.Id == participacaoAluno.Apostila_Ah_Id)!.NumeroTotalPaginas;

		if (participacaoAluno.NumeroPaginaAbaco > totalPaginasAbaco)
		{
			return new ResponseModel { Message = $"Número de páginas da apostila Abaco não pode ser maior que o total de páginas: Participacao ID {participacaoAluno.Participacao_Id}" };
		}

		if (participacaoAluno.NumeroPaginaAh > totalPaginasAh)
		{
			return new ResponseModel { Message = $"Número de páginas da apostila AH não pode ser maior que o total de páginas: Participacao ID {participacaoAluno.Participacao_Id}" };
		}

		return new ResponseModel { Success = true };
	}

	private static int? CalcularStatusContato(bool Presente, int? ReposicaoDe_Evento_Id)
	{
		switch (Presente, ReposicaoDe_Evento_Id.HasValue)
		{
			case (true, true):
				return (int)StatusContato.REPOSICAO_REALIZADA;

			case (true, false):
				return null;

			case (false, true):
				return (int)StatusContato.REPOSICAO_NAO_COMPARECEU;

			case (false, false):
				return (int)StatusContato.NAO_COMPARECEU;
		}
	}

	public ResponseModel CreateEventValidation(CreateEventDto dto)
	{
		ResponseModel response = new();

		try
		{
			bool eventoTipoExists = _db.Evento_Tipos.Any(e => e.Id == dto.Evento_Tipo_Id);

			if (!eventoTipoExists)
			{
				throw new Exception("Tipo de evento não encontrado");
			}

			if (dto.CapacidadeMaximaAlunos < 0)
			{
				throw new Exception("Capacidade máxima de alunos não pode ser menor que 0");
			}

			bool salaExists = _db.Salas.Any(s => s.Id == dto.Sala_Id);

			if (!salaExists)
			{
				throw new Exception("Sala não encontrada");
			}

			bool isSalaOccupied = _salaService.IsSalaOccupied(dto.Sala_Id, dto.Data, dto.DuracaoMinutos, null);

			if (isSalaOccupied)
			{
				throw new Exception("Esta sala se encontra ocupada neste horário");
			}

			if (dto.DuracaoMinutos <= 0)
			{
				throw new Exception("Duração inválida: Valor deve ser maior que zero");
			}

			if (dto.Turma_Id.HasValue)
			{
				bool turmaExists = _db.Turmas.Any(t => t.Id == dto.Turma_Id);

				if (!turmaExists)
				{
					throw new Exception("Turma não encontrada");
				}
			}

			if (dto.Roteiro_Id != -1 && dto.Roteiro_Id.HasValue)
			{
				bool roteiroExists = _db.Roteiro.Any(r => r.Id == dto.Roteiro_Id);

				if (!roteiroExists)
				{
					throw new Exception("Roteiro não encontrado");
				}
			}

			IQueryable<Aluno> alunosInRequest = _db.Aluno
				.Where(a => dto.Alunos.Contains(a.Id) && a.Deactivated == null);

			if (alunosInRequest.Count() != dto.Alunos.Count)
			{
				throw new Exception("Aluno(s) não encontrado(s)");
			}

			IQueryable<Professor> professoresInRequest = _db.Professor
				.Include(p => p.Account)
				.Where(p => dto.Professores.Contains(p.Id) && p.Account.Deactivated == null);

			if (professoresInRequest.Count() != dto.Professores.Count)
			{
				throw new Exception("Professor(es) não encontrado(s)");
			}

			if (dto.PerfilCognitivo is not null)
			{
				IQueryable<PerfilCognitivo> perfisCognitivosInRequest = _db.PerfilCognitivos
					.Where(p => dto.PerfilCognitivo.Contains(p.Id));

				if (perfisCognitivosInRequest.Count() != dto.PerfilCognitivo.Count)
				{
					throw new Exception("Pelo menos um perfil cognitivo na requisição não foi encontrado");
				}

				HashSet<int> alunoPerfisSet = alunosInRequest
					.Where(a => a.PerfilCognitivo_Id.HasValue)
					.Select(a => a.PerfilCognitivo_Id!.Value).ToHashSet();

				HashSet<int> perfisCognitivosSet = perfisCognitivosInRequest
					.Select(p => p.Id)
					.ToHashSet();

				bool perfisCognitivosMatch = alunoPerfisSet.IsSubsetOf(perfisCognitivosSet);

				if (!perfisCognitivosMatch)
				{
					throw new Exception("Algum aluno ou perfil cognitivo não é compatível");
				}
			}

			foreach (var professor in professoresInRequest)
			{
				bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
					professorId: professor.Id,
					DiaSemana: (int)dto.Data.DayOfWeek,
					Horario: dto.Data.TimeOfDay,
					IgnoredTurmaId: dto.Turma_Id ?? null
				);

				if (hasTurmaConflict)
				{
					throw new Exception($"Professor: '{professor.Account.Name}' possui uma turma nesse mesmo horário");
				}

				bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
					professorId: professor.Id,
					Data: dto.Data,
					DuracaoMinutos: dto.DuracaoMinutos,
					IgnoredEventoId: null
				);

				if (hasParticipacaoConflict)
				{
					throw new Exception($"Professor: {professor.Account.Name} possui participação em outro evento nesse mesmo horário");
				}
			}

			response.Success = true;
			response.Message = "Validação foi realizada com sucesso";
		}
		catch (Exception ex)
		{
			response.Success = false;
			response.Message = $"Não foi possível validar os dados da requisição: {ex.Message}";
		}

		return response;
	}

}
