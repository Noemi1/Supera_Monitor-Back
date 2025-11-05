using System.Globalization;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Eventos;
using Supera_Monitor_Back.Models.Eventos.Dtos;
using Supera_Monitor_Back.Models.Eventos.Participacao;

namespace Supera_Monitor_Back.Services.Eventos;

public interface IEventoService
{
	public CalendarioEventoList GetPseudoAula(PseudoEventoRequest request);
	public List<CalendarioEventoList> GetOficinas();

	public CalendarioEventoList GetEventoById(int eventoId);
	public ResponseModel Insert(CreateEventoRequest request, int eventoTipoId);
	public ResponseModel Update(UpdateEventoRequest request);
	public ResponseModel Cancelar(CancelarEventoRequest request);
	//public Task<ResponseModel> CancelaEventosFeriado(int ano);
	public ResponseModel Finalizar(FinalizarEventoRequest request);
	public ResponseModel FinalizarAulaZero(FinalizarAulaZeroRequest request);
	public ResponseModel Reagendar(ReagendarEventoRequest request);

	public ResponseModel InsertParticipacao(InsertParticipacaoRequest request);
	public ResponseModel UpdateParticipacao(UpdateParticipacaoRequest request);
	public ResponseModel CancelarParticipacao(CancelarParticipacaoRequest request);
	public ResponseModel RemoveParticipacao(int participacaoId);

	public Task<List<FeriadoResponse>> GetFeriados(int ano);
}

public class EventoService : IEventoService
{
	private readonly DataContext _db;
	private readonly IMapper _mapper;
	private readonly IProfessorService _professorService;
	private readonly ISalaService _salaService;
	private readonly IRoteiroService _roteiroService;

	private readonly Account? _account;

	public EventoService(
		DataContext db,
		IMapper mapper,
		IProfessorService professorService,
		ISalaService salaService,
		IRoteiroService roteiroService,
		IHttpContextAccessor httpContextAccessor
	)
	{
		_db = db;
		_mapper = mapper;
		_professorService = professorService;
		_salaService = salaService;
		_roteiroService = roteiroService;
		_account = (Account?)httpContextAccessor.HttpContext?.Items["Account"];
	}

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
					{
						return new ResponseModel { Message = "Um evento de reunião não pode ter alunos associados" };
					}
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
			;

			IQueryable<Aluno> alunosInRequest = _db.Alunos.Where(a => a.Deactivated == null && request.Alunos.Contains(a.Id));

			if (alunosInRequest.Count() != request.Alunos.Count)
			{
				return new ResponseModel { Message = "Aluno(s) não encontrado(s)" };
			}

			IQueryable<Professor> professoresInRequest = _db.Professors.Include(p => p.Account).Where(p => p.Account.Deactivated == null && request.Professores.Contains(p.Id));

			if (professoresInRequest.Count() != request.Professores.Count)
			{
				return new ResponseModel { Message = "Professor(es) não encontrado(s)" };
			}

			foreach (var professor in professoresInRequest)
			{
				bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
					professorId: professor.Id,
					DiaSemana: (int)request.Data.DayOfWeek,
					Horario: request.Data.TimeOfDay,
					IgnoredTurmaId: null
				);

				if (hasTurmaConflict)
				{
					return new ResponseModel { Message = $"Professor: '{professor.Account.Name}' possui uma turma nesse mesmo horário" };
				}

				bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
					professorId: professor.Id,
					Data: request.Data,
					DuracaoMinutos: request.DuracaoMinutos,
					IgnoredEventoId: null
				);

				if (hasParticipacaoConflict)
				{
					return new ResponseModel { Message = $"Professor: {professor.Account.Name} possui participação em outro evento nesse mesmo horário" };
				}
			}

			// Não devo poder registrar um evento em uma sala que não existe
			bool salaExists = _db.Salas.Any(s => s.Id == request.Sala_Id);

			if (!salaExists)
			{
				return new ResponseModel { Message = "Sala não encontrada" };
			}

			// Sala deve estar livre no horário do evento
			bool isSalaOccupied = _salaService.IsSalaOccupied(request.Sala_Id, request.Data, request.DuracaoMinutos, null);

			if (isSalaOccupied)
			{
				return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };
			}

			if (request.CapacidadeMaximaAlunos < 0)
			{
				return new ResponseModel { Message = "Capacidade máxima de alunos inválida" };
			}

			// Validations passed

			Evento evento = new()
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
				Account_Created_Id = _account!.Id
			};

			_db.Eventos.Add(evento);
			_db.SaveChanges();

			// Adicionar as participações dos envolvidos no evento - Alunos e Professores
			var participacoesAlunos = alunosInRequest.Select(aluno => new Evento_Participacao_Aluno
			{
				Aluno_Id = aluno.Id,
				Evento_Id = evento.Id,

				// Inserir o progresso do aluno no evento
				Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
				NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
				Apostila_AH_Id = aluno.Apostila_AH_Id,
				NumeroPaginaAH = aluno.NumeroPaginaAH,
			});

			foreach (var participacao in participacoesAlunos)
			{
				_db.Evento_Participacao_Alunos.Add(participacao);

				_db.Aluno_Historicos.Add(new Aluno_Historico
				{
					Account_Id = _account.Id,
					Aluno_Id = participacao.Aluno_Id,
					Data = evento.Data,
					Descricao = $"Aluno se inscreveu em um evento de '{eventoTipo}' no dia {evento.Data:G}"
				});
			}

			var participacoesProfessores = professoresInRequest.Select(aluno => new Evento_Participacao_Professor
			{
				Professor_Id = aluno.Id,
				Evento_Id = evento.Id,
			});

			if (participacoesProfessores.Any())
			{
				_db.Evento_Participacao_Professors.AddRange(participacoesProfessores);
			}

			foreach (var professor in professoresInRequest)
			{
				bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
					professorId: professor.Id,
					DiaSemana: (int)request.Data.DayOfWeek,
					Horario: request.Data.TimeOfDay,
					IgnoredTurmaId: null
				);

				if (hasTurmaConflict)
				{
					return new ResponseModel { Message = $"Professor: '{professor.Account.Name}' possui uma turma nesse mesmo horário" };
				}

				bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
					professorId: professor.Id,
					Data: request.Data,
					DuracaoMinutos: request.DuracaoMinutos,
					IgnoredEventoId: null
				);

				if (hasParticipacaoConflict)
				{
					return new ResponseModel { Message = $"Professor: {professor.Account.Name} possui participação em outro evento nesse mesmo horário" };
				}
			}

			_db.SaveChanges();

			var responseObject = this.GetEventoById(evento.Id);
			responseObject.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == evento.Id).ToList();
			responseObject.Professores = _db.CalendarioProfessorLists.Where(p => p.Evento_Id == evento.Id).ToList();

			response.Success = true;
			response.Message = $"{responseObject.Evento_Tipo} registrada com sucesso";
			response.Object = responseObject;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao inserir evento de tipo '{(int)eventoTipoId}': {ex}";
		}

		return response;
	}

	public ResponseModel Update(UpdateEventoRequest request)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Evento? evento = _db.Eventos
				.Include(e => e.Evento_Tipo)
				.Include(e => e.Evento_Participacao_Alunos)
				.FirstOrDefault(e => e.Id == request.Id);

			if (evento is null)
			{
				return new ResponseModel { Message = "Evento não encontrado" };
			}

			IQueryable<Professor> professoresInRequest = _db.Professors.Include(p => p.Account).Where(p => p.Account.Deactivated == null && request.Professores.Contains(p.Id));

			if (professoresInRequest.Count() != request.Professores.Count)
			{
				return new ResponseModel { Message = "Professor(es) não encontrado(s)" };
			}

			//if (request.Data < TimeFunctions.HoraAtualBR()) {
			//    return new ResponseModel { Message = "Data do evento não pode ser no passado" };
			//}

			foreach (var professor in professoresInRequest)
			{
				bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
					professorId: professor.Id,
					DiaSemana: (int)evento.Data.DayOfWeek,
					Horario: evento.Data.TimeOfDay,
					IgnoredTurmaId: null
				);

				if (hasTurmaConflict)
				{
					return new ResponseModel { Message = $"Professor: '{professor.Account.Name}' possui uma turma nesse mesmo horário" };
				}

				Evento_Participacao_Professor? participacaoProfessor = _db.Evento_Participacao_Professors.FirstOrDefault(p => p.Evento_Id == evento.Id);

				bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
					professorId: professor.Id,
					Data: evento.Data,
					DuracaoMinutos: request.DuracaoMinutos,
					IgnoredEventoId: evento.Id
				);

				if (hasParticipacaoConflict)
				{
					return new ResponseModel { Message = $"Professor: {professor.Account.Name} possui participação em outro evento nesse mesmo horário" };
				}
			}

			// Não devo poder registrar um evento em uma sala que não existe
			bool salaExists = _db.Salas.Any(s => s.Id == request.Sala_Id);

			if (!salaExists)
			{
				return new ResponseModel { Message = "Sala não encontrada" };
			}

			// Sala deve estar livre no horário do evento
			bool isSalaOccupied = _salaService.IsSalaOccupied(request.Sala_Id, evento.Data, request.DuracaoMinutos, evento.Id);

			if (isSalaOccupied)
			{
				return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };
			}

			// Validations passed

			var oldObject = _db.CalendarioEventoLists.First(e => e.Id == evento.Id);

			evento.Observacao = request.Observacao ?? request.Observacao;
			evento.Descricao = request.Descricao ?? evento.Descricao;
			evento.Sala_Id = request.Sala_Id;
			evento.DuracaoMinutos = request.DuracaoMinutos;
			evento.CapacidadeMaximaAlunos = request.CapacidadeMaximaAlunos;
			evento.LastUpdated = TimeFunctions.HoraAtualBR();

			_db.Eventos.Update(evento);
			_db.SaveChanges();

			// IDS que preciso desativar
			List<Evento_Participacao_Professor> participacoesToDeactivate = _db.Evento_Participacao_Professors.Where(p => !request.Professores.Contains(p.Professor_Id) && p.Evento_Id == evento.Id).ToList();

			// IDS que existem
			var idsExistentes = _db.Evento_Participacao_Professors.Where(p => p.Evento_Id == evento.Id).Select(p => p.Professor_Id).ToList();

			// IDS que preciso adicionar
			List<int> participacoesToAdd = request.Professores.Where(id => !idsExistentes.Contains(id)).ToList();

			foreach (var participacao in participacoesToDeactivate)
			{
				participacao.Deactivated = TimeFunctions.HoraAtualBR();
				_db.Evento_Participacao_Professors.Update(participacao);
			}

			foreach (int professorId in participacoesToAdd)
			{
				var participacao = new Evento_Participacao_Professor
				{
					Evento_Id = evento.Id,
					Professor_Id = professorId
				};

				_db.Evento_Participacao_Professors.Add(participacao);
			}

			_db.SaveChanges();

			var responseObject = _db.CalendarioEventoLists.First(e => e.Id == evento.Id);
			responseObject.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == evento.Id).ToList();
			responseObject.Professores = _db.CalendarioProfessorLists.Where(p => p.Evento_Id == evento.Id).ToList();

			response.Message = $"Evento de '{evento.Evento_Tipo.Nome}' atualizado com sucesso";
			response.OldObject = oldObject;
			response.Object = responseObject;
			response.Success = true;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao atualizar evento ID: '{request.Id}' | {ex}";
		}

		return response;
	}

	public ResponseModel InsertParticipacao(InsertParticipacaoRequest request)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Evento? evento = _db.Eventos
				.Include(e => e.Evento_Aula)
				.Include(e => e.Evento_Participacao_Alunos)
				.Include(e => e.Evento_Tipo)
				.FirstOrDefault(e => e.Id == request.Evento_Id);

			ResponseModel eventValidation = ValidateEvent(evento);

			if (!eventValidation.Success)
			{
				return eventValidation;
			}

			Aluno? aluno = _db.Alunos.Find(request.Aluno_Id);

			if (aluno is null)
			{
				return new ResponseModel { Message = "Aluno não encontrado" };
			}

			// Se aluno já está inscrito, não deve poder ser inscrito novamente
			bool alunoIsAlreadyEnrolled = evento!.Evento_Participacao_Alunos.Any(p => p.Aluno_Id == aluno.Id);

			if (alunoIsAlreadyEnrolled)
			{
				return new ResponseModel { Message = "Aluno já está inscrito neste evento" };
			}

			int amountOfAlunosEnrolled = evento.Evento_Participacao_Alunos.Count;

			switch (evento.Evento_Tipo_Id)
			{
				case (int)EventoTipo.Aula:
					if (amountOfAlunosEnrolled >= evento.CapacidadeMaximaAlunos)
					{
						return new ResponseModel { Message = "Este evento de aula se encontra lotado." };
					}
					break;

				case (int)EventoTipo.AulaExtra:
					if (amountOfAlunosEnrolled >= evento.CapacidadeMaximaAlunos)
					{
						return new ResponseModel { Message = "Este evento de aula extra se encontra lotado." };
					}
					break;

				case (int)EventoTipo.AulaZero:
					// Se o aluno sendo inscrito já participou de uma aula zero, não deve permitir a inscrição
					bool alunoAlreadyParticipated = _db.Evento_Participacao_Alunos
						.Include(p => p.Evento)
						.Any(p =>
							p.Aluno_Id == aluno.Id
							&& p.Evento.Evento_Tipo_Id == (int)EventoTipo.AulaZero
							&& p.Evento.Deactivated == null);

					if (alunoAlreadyParticipated)
					{
						return new ResponseModel { Message = $"Este aluno já participou de uma aula zero." };
					}

					aluno.AulaZero_Id = evento.Id;
					_db.Alunos.Update(aluno);
					if (alunoAlreadyParticipated)
					{
						return new ResponseModel { Message = $"Este aluno já participou de uma aula zero." };
					}

					break;

				case (int)EventoTipo.Reuniao:
					return new ResponseModel { Message = "Não é possível inscrever alunos em uma reunião." };

				default:
					break;
			}

			// Validations passed

			Evento_Participacao_Aluno newParticipacao = new()
			{
				Evento_Id = evento.Id,
				Aluno_Id = aluno.Id,
				Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
				NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
				Apostila_AH_Id = aluno.Apostila_AH_Id,
				NumeroPaginaAH = aluno.NumeroPaginaAH,
			};

			_db.Aluno_Historicos.Add(new Aluno_Historico
			{
				Aluno_Id = aluno.Id,
				Descricao = $"Aluno foi inscrito no evento '{evento.Descricao}' do dia {evento.Data:G} - Evento é do tipo '{evento.Evento_Tipo.Nome}'",
				Account_Id = _account!.Id,
				Data = TimeFunctions.HoraAtualBR(),
			});

			_db.Evento_Participacao_Alunos.Add(newParticipacao);
			_db.SaveChanges();

			response.Message = $"Aluno foi inscrito no evento com sucesso";
			response.Object = newParticipacao;
			response.Success = true;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao inscrever aluno no evento: {ex}";
		}

		return response;
	}

	public ResponseModel UpdateParticipacao(UpdateParticipacaoRequest request)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Evento_Participacao_Aluno? participacao = _db.Evento_Participacao_Alunos.Find(request.Participacao_Id);

			if (participacao is null)
			{
				return new ResponseModel { Message = "Participação não encontrada" };
			}

			Apostila? apostilaAbaco = _db.Apostilas.Find(request.Apostila_Abaco_Id);

			if (request.Apostila_Abaco_Id.HasValue && apostilaAbaco is null)
			{
				return new ResponseModel { Message = "Apostila Ábaco não encontrada" };
			}

			Apostila? apostilaAh = _db.Apostilas.Find(request.Apostila_AH_Id);

			if (request.Apostila_Abaco_Id.HasValue && apostilaAbaco is null)
			{
				return new ResponseModel { Message = "Apostila AH não encontrada" };
			}

			// Validations passed

			participacao.Observacao = request.Observacao;
			participacao.Deactivated = request.Deactivated;

			participacao.Apostila_Abaco_Id = request.Apostila_Abaco_Id;
			participacao.NumeroPaginaAbaco = apostilaAbaco is not null ? request.NumeroPaginaAbaco : null;
			participacao.Apostila_AH_Id = request.Apostila_Abaco_Id;
			participacao.NumeroPaginaAH = apostilaAh is not null ? request.NumeroPaginaAbaco : null;

			participacao.AlunoContactado = request.AlunoContactado;
			participacao.ContatoObservacao = request.ContatoObservacao;
			participacao.StatusContato_Id = request.StatusContato_Id;

			_db.Evento_Participacao_Alunos.Update(participacao);
			_db.SaveChanges();

			response.Message = "Participação do aluno foi atualizada com sucesso.";
			response.Success = true;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao atualizar participação do aluno: {ex}";
		}

		return response;
	}

	public ResponseModel RemoveParticipacao(int participacaoId)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Evento_Participacao_Aluno? participacao = _db.Evento_Participacao_Alunos.Include(e => e.Aluno).FirstOrDefault(p => p.Id == participacaoId);

			if (participacao is null)
			{
				return new ResponseModel { Message = "Participação do aluno em evento não encontrada" };
			}

			if (participacao.Deactivated.HasValue)
			{
				return new ResponseModel { Message = "Não é possível remover uma participação que está desativada" };
			}

			// Validations passed

			if (participacao.Aluno.PrimeiraAula_Id == participacao.Evento_Id)
			{
				participacao.Aluno.PrimeiraAula_Id = null;
			}

			if (participacao.Aluno.AulaZero_Id == participacao.Evento_Id)
			{
				participacao.Aluno.AulaZero_Id = null;
			}

			_db.Evento_Participacao_Alunos.Remove(participacao);
			_db.SaveChanges();

			response.Message = "Participação do aluno removida com sucesso";
			response.Success = true;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao remover participação aluno em evento: {ex}";
		}

		return response;
	}

	public ResponseModel CancelarParticipacao(CancelarParticipacaoRequest request)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Evento_Participacao_Aluno? participacao = _db.Evento_Participacao_Alunos.Include(e => e.Aluno).FirstOrDefault(p => p.Id == request.Participacao_Id);

			if (participacao is null)
			{
				return new ResponseModel { Message = "Participação do aluno em evento não encontrada" };
			}

			if (participacao.Deactivated.HasValue)
			{
				return new ResponseModel { Message = "A participação já está desativada" };
			}

			// Validations passed

			if (participacao.Aluno.PrimeiraAula_Id == participacao.Evento_Id)
			{
				participacao.Aluno.PrimeiraAula_Id = null;
			}

			if (participacao.Aluno.AulaZero_Id == participacao.Evento_Id)
			{
				participacao.Aluno.AulaZero_Id = null;
			}

			participacao.Presente = false;
			participacao.Observacao = request.Observacao;
			participacao.Deactivated = TimeFunctions.HoraAtualBR();

			participacao.AlunoContactado = request.AlunoContactado;
			participacao.StatusContato_Id = request.StatusContato_Id;
			participacao.ContatoObservacao = request.ContatoObservacao;

			if (request.ReposicaoDe_Evento_Id.HasValue)
			{
				participacao.StatusContato_Id = (int)StatusContato.REPOSICAO_DESMARCADA;
			}

			_db.Evento_Participacao_Alunos.Update(participacao);
			_db.SaveChanges();

			response.Message = "A participação do aluno foi cancelada";
			response.Success = true;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao cancelar participação do aluno no evento: {ex}";
		}

		return response;
	}

	public ResponseModel Cancelar(CancelarEventoRequest request)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Evento? evento = _db.Eventos
				.FirstOrDefault(e => e.Id == request.Id);

			if (evento is null)
			{
				return new ResponseModel { Message = "Evento não encontrado." };
			}

			if (evento.Deactivated.HasValue)
			{
				return new ResponseModel { Message = "Evento já foi cancelado" };
			}

			// Validations passed
			List<Evento_Participacao_Aluno> participacoes = _db.Evento_Participacao_Alunos
				.Include(p => p.Aluno)
				.Where(p => p.Evento_Id == evento.Id)
				.ToList();

			foreach (var participacao in participacoes)
			{
				if (participacao.Aluno.PrimeiraAula_Id == evento.Id)
				{
					participacao.Aluno.PrimeiraAula_Id = null;
				}

				if (participacao.Aluno.AulaZero_Id == evento.Id)
				{
					participacao.Aluno.AulaZero_Id = null;
				}

				participacao.StatusContato_Id = (int)StatusContato.AULA_CANCELADA;
			}

			_db.UpdateRange(participacoes);

			evento.Deactivated = TimeFunctions.HoraAtualBR();
			evento.Observacao = request.Observacao;

			_db.Eventos.Update(evento);
			_db.SaveChanges();

			var responseObject = _db.Eventos.Where(e => e.Id == evento.Id)
				.ProjectTo<EventoModel>(_mapper.ConfigurationProvider)
				.AsNoTracking()
				.First();

			response.Message = $"Evento foi cancelado com sucesso";
			response.Object = responseObject;
			response.Success = true;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao cancelar evento: {ex}";
		}

		return response;
	}

	public ResponseModel Reagendar(ReagendarEventoRequest request)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Evento? evento = _db.Eventos
				.Include(e => e.Evento_Participacao_Alunos)
				.Include(e => e.Evento_Participacao_Professors)
				.Include(e => e.Evento_Aula)
				.FirstOrDefault(e => e.Id == request.Evento_Id);

			if (evento is null)
			{
				return new ResponseModel { Message = "Evento não encontrado." };
			}

			if (evento.Deactivated.HasValue)
			{
				return new ResponseModel { Message = "Evento está desativado." };
			}

			if (request.Data <= TimeFunctions.HoraAtualBR())
			{
				return new ResponseModel { Message = "Não é possível reagendar um evento para uma data no passado." };
			}

			// Sala deve estar livre no horário do evento
			bool isSalaOccupied = _salaService.IsSalaOccupied(request.Sala_Id, request.Data, evento.DuracaoMinutos, evento.Id);

			if (isSalaOccupied)
			{
				return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };
			}

			// Todos os professores devem estar livres no horário do evento

			List<Evento_Participacao_Professor> professorParticipacoes = evento.Evento_Participacao_Professors.ToList();

			foreach (var participacao in professorParticipacoes)
			{
				bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
					professorId: participacao.Professor_Id,
					DiaSemana: (int)request.Data.DayOfWeek,
					Horario: request.Data.TimeOfDay,
					IgnoredTurmaId: null
				);

				if (hasTurmaConflict)
				{
					return new ResponseModel { Message = $"Professor ID: '{participacao.Professor_Id}' possui uma turma nesse mesmo horário" };
				}

				bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
					professorId: participacao.Professor_Id,
					Data: request.Data,
					DuracaoMinutos: evento.DuracaoMinutos,
					IgnoredEventoId: evento.Id
				);

				if (hasParticipacaoConflict)
				{
					return new ResponseModel { Message = $"Professor ID: {participacao.Professor_Id} possui participação em outro evento nesse mesmo horário" };
				}
			}
			// Validations passed

			evento.Deactivated = TimeFunctions.HoraAtualBR();

			_db.Eventos.Update(evento);

			Evento newEvento = new()
			{
				Data = request.Data,
				Observacao = request.Observacao ?? evento.Observacao,
				Sala_Id = request.Sala_Id,
				DuracaoMinutos = evento.DuracaoMinutos,
				Evento_Tipo_Id = evento.Evento_Tipo_Id,
				Account_Created_Id = evento.Account_Created_Id,
				ReagendamentoDe_Evento_Id = evento.Id,
				Descricao = evento.Descricao,
				CapacidadeMaximaAlunos = evento.CapacidadeMaximaAlunos,

				Created = TimeFunctions.HoraAtualBR(),
				Deactivated = null,
				Finalizado = false,
			};

			if (evento.Evento_Aula is not null)
			{
				Evento_Aula newEventoAula = new()
				{
					Professor_Id = evento.Evento_Aula.Professor_Id,
					Roteiro_Id = evento.Evento_Aula.Roteiro_Id,
					Turma_Id = evento.Evento_Aula.Turma_Id,
				};

				newEvento.Evento_Aula = newEventoAula;
			}

			_db.Add(newEvento);
			_db.SaveChanges();

			// Adicionar uma nova participação na data indicada no request, e em seguida desativar a participação original
			foreach (var participacao in evento.Evento_Participacao_Alunos)
			{
				Evento_Participacao_Aluno newParticipacao = new()
				{
					Evento_Id = newEvento.Id,
					Aluno_Id = participacao.Aluno_Id,
					Observacao = participacao.Observacao,
					Presente = participacao.Presente,
					Apostila_Abaco_Id = participacao.Apostila_Abaco_Id,
					Apostila_AH_Id = participacao.Apostila_AH_Id,
					NumeroPaginaAbaco = participacao.NumeroPaginaAbaco,
					NumeroPaginaAH = participacao.NumeroPaginaAH,
					ReposicaoDe_Evento_Id = participacao.ReposicaoDe_Evento_Id,
					Deactivated = participacao.Deactivated,
				};

				_db.Evento_Participacao_Alunos.Add(newParticipacao);

				participacao.Deactivated = TimeFunctions.HoraAtualBR();
				_db.Evento_Participacao_Alunos.Update(participacao);
			}

			// Fazer o mesmo com a participação do professor
			foreach (var participacao in evento.Evento_Participacao_Professors)
			{
				Evento_Participacao_Professor newParticipacao = new()
				{
					Evento_Id = newEvento.Id,
					Professor_Id = participacao.Professor_Id,
					Observacao = participacao.Observacao,
					Presente = participacao.Presente,
					Deactivated = participacao.Deactivated,
				};

				_db.Evento_Participacao_Professors.Add(newParticipacao);

				participacao.Deactivated = TimeFunctions.HoraAtualBR();
				_db.Evento_Participacao_Professors.Update(participacao);
			}

			_db.SaveChanges();

			var responseObject = _db.CalendarioEventoLists.FirstOrDefault(e => e.Id == newEvento.Id)!;
			responseObject.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == newEvento.Id).ToList();
			responseObject.Professores = _db.CalendarioProfessorLists.Where(p => p.Evento_Id == newEvento.Id).ToList();

			var oldObject = _db.CalendarioEventoLists.FirstOrDefault(e => e.Id == evento.Id);

			response.Message = $"Evento foi reagendado com sucesso para o dia {responseObject?.Data:g}";
			response.OldObject = oldObject;
			response.Object = responseObject;
			response.Success = true;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao reagendar evento: {ex}";
		}

		return response;
	}

	private static ResponseModel ValidateEvent(Evento? evento)
	{
		if (evento is null)
		{
			return new ResponseModel { Message = "Evento não encontrado." };
		}

		if (evento.Deactivated.HasValue)
		{
			return new ResponseModel { Message = $"Este evento foi cancelado às {evento.Deactivated.Value:g}" };
		}

		if (evento.Finalizado)
		{
			return new ResponseModel { Message = "Evento já está finalizado" };
		}

		return new ResponseModel { Success = true, Message = "Evento válido" };
	}

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
		Evento_Participacao_Aluno? participacao = evento.Evento_Participacao_Alunos
						.FirstOrDefault(p => p.Id == participacaoAluno.Participacao_Id);

		if (participacao is null)
		{
			return new ResponseModel { Message = $"Participação de aluno no evento ID: '{evento.Id}' Participacao_Id: '{participacaoAluno.Participacao_Id}' não foi encontrada" };
		}

		//if (participacao.Deactivated.HasValue) {
		//    return new ResponseModel { Message = $"Participação de aluno no evento ID: '{evento.Id}' Participacao_Id: '{participacaoAluno.Participacao_Id}' está desativada" };
		//}

		// Alunos devem possuir as apostilas em que estão tentando marcar progresso

		//var alunoApostilaKitId = participacao.Aluno.Apostila_Kit_Id;

		//if (alunoApostilaKitId == null)
		//{
		//	return new ResponseModel { Message = $"Aluno ID: '{participacao.Aluno.Pessoa.Nome}' não possui kit de apostilas" };
		//}

		//var alunoApostilaKitRels = _db.Apostila_Kit_Rels.Where(a => a.Apostila_Kit_Id == alunoApostilaKitId).ToList();

		//bool alunoHasApostilaAbaco = alunoApostilaKitRels.Any(a => a.Apostila_Id == participacaoAluno.Apostila_Abaco_Id);
		//bool alunoHasApostilaAh = alunoApostilaKitRels.Any(a => a.Apostila_Id == participacaoAluno.Apostila_Ah_Id);

		//// Para poder atualizar, o kit de apostilas do aluno deve possuir a apostila Abaco e a apostila AH passadas na requisição

		//if (!alunoHasApostilaAbaco)
		//{
		//	return new ResponseModel { Message = $"Aluno ID: '{participacao.Aluno_Id}' não possui a apostila Abaco ID: '{participacaoAluno.Apostila_Abaco_Id}'" };
		//}

		//if (!alunoHasApostilaAh)
		//{
		//	return new ResponseModel { Message = $"Aluno ID: '{participacao.Aluno_Id}' não possui a apostila AH ID: '{participacaoAluno.Apostila_Ah_Id}'" };
		//}

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

	public ResponseModel Finalizar(FinalizarEventoRequest request)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Evento? evento = _db.Eventos
				.Include(e => e.Evento_Participacao_Alunos)
				.ThenInclude(e => e.Aluno)
				.Include(e => e.Evento_Participacao_Professors)
				.FirstOrDefault(e => e.Id == request.Evento_Id);

			var eventValidation = ValidateEvent(evento);

			if (!eventValidation.Success)
			{
				return eventValidation;
			}

			var reposicaoIds = request.Alunos
				.Select(a => a.ReposicaoDe_Evento_Id)
				.Where(id => id.HasValue)
				.Select(id => id!.Value)
				.Distinct();

			var eventosExistentes = _db.Eventos
				.Where(e => reposicaoIds.Contains(e.Id))
				.Select(e => e.Id)
				.ToList();

			var idsSemEvento = reposicaoIds.Except(eventosExistentes);

			if (idsSemEvento.Any())
			{
				return new ResponseModel { Message = $"Uma ou mais reposições estão sem Evento associado: {string.Join(", ", idsSemEvento)}" };
			}

			var existingApostilas = _db.Apostilas.ToList();

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
			{
				return validateApostilasAh;
			}

			if (!validateApostilasAbaco.Success)
			{
				return validateApostilasAbaco;
			}

			// Validations passed

			evento!.Observacao = request.Observacao;
			evento.Finalizado = true;
			evento.LastUpdated = TimeFunctions.HoraAtualBR();

			_db.Eventos.Update(evento);

			foreach (ParticipacaoAlunoModel participacaoModel in request.Alunos)
			{
				Evento_Participacao_Aluno? participacao = evento.Evento_Participacao_Alunos
					.FirstOrDefault(p => p.Id == participacaoModel.Participacao_Id);

				var validateParticipacao = ValidateParticipacao(participacaoModel, evento, existingApostilas);

				if (!validateParticipacao.Success)
				{
					return validateParticipacao;
				}

				if (participacao is null)
				{
					return new ResponseModel { Message = $"Participação de aluno no evento ID: '{evento.Id}' Participacao_Id: '{participacaoModel.Participacao_Id}' não foi encontrada" };
				}

				// Atualizar tanto a participação quanto o aluno
				participacao.Apostila_Abaco_Id = participacaoModel.Apostila_Abaco_Id;
				participacao.NumeroPaginaAbaco = participacaoModel.NumeroPaginaAbaco;
				participacao.Aluno.Apostila_Abaco_Id = participacaoModel.Apostila_Abaco_Id;
				participacao.Aluno.NumeroPaginaAbaco = participacaoModel.NumeroPaginaAbaco;

				participacao.Apostila_AH_Id = participacaoModel.Apostila_Ah_Id;
				participacao.NumeroPaginaAH = participacaoModel.NumeroPaginaAh;
				participacao.Aluno.Apostila_AH_Id = participacaoModel.Apostila_Ah_Id;
				participacao.Aluno.NumeroPaginaAH = participacaoModel.NumeroPaginaAh;

				participacao.Observacao = participacaoModel.Observacao;
				participacao.Presente = participacaoModel.Presente;

				participacao.ReposicaoDe_Evento_Id = participacaoModel.ReposicaoDe_Evento_Id;
				participacao.StatusContato_Id = CalcularStatusContato(participacaoModel.Presente, participacaoModel.ReposicaoDe_Evento_Id);

				if (!participacaoModel.Presente)
				{
					_db.Aluno_Historicos.Add(new Aluno_Historico
					{
						Account_Id = _account!.Id,
						Aluno_Id = participacao.Aluno_Id,
						Data = evento.Data,
						Descricao = $"Aluno faltou no evento '{evento.Descricao}' no dia {evento.Data:G}"
					});
				}

				_db.Update(participacao);
			}

			foreach (ParticipacaoProfessorModel partProfessor in request.Professores)
			{
				Evento_Participacao_Professor? participacao = evento.Evento_Participacao_Professors.FirstOrDefault(p => p.Id == partProfessor.Participacao_Id);

				if (participacao is null)
				{
					return new ResponseModel { Message = $"Participação de professor no evento ID: '{evento.Id}' Participacao_Id: '{partProfessor.Participacao_Id}' não foi encontrada" };
				}

				participacao.Observacao = partProfessor.Observacao;
				participacao.Presente = partProfessor.Presente;

				_db.Evento_Participacao_Professors.Update(participacao);
			}

			_db.SaveChanges();

			var responseObject = _db.CalendarioEventoLists.FirstOrDefault(e => e.Id == evento.Id);

			response.Message = $"Evento foi finalizado com sucesso.";
			response.Object = _db.CalendarioEventoLists.FirstOrDefault(e => e.Id == evento.Id);
			response.Success = true;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao finalizar evento: {ex}";
		}

		return response;
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

	public List<CalendarioEventoList> GetOficinas()
	{
		var oficinas = _db.CalendarioEventoLists
			.Where(e =>
				e.Evento_Tipo_Id == (int)EventoTipo.Oficina
				&& e.Data > TimeFunctions.HoraAtualBR())
			.OrderBy(e => e.Data)
			.ToList();

		foreach (var evento in oficinas)
		{
			evento.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == evento.Id).ToList();

			evento.Professores = _db.CalendarioProfessorLists.Where(e => e.Evento_Id == evento.Id).ToList();
		}

		return oficinas;
	}

	public CalendarioEventoList GetEventoById(int eventoId)
	{
		CalendarioEventoList? evento = _db.CalendarioEventoLists.FirstOrDefault(e => e.Id == eventoId);
		if (evento is null)
		{
			throw new Exception("Evento não encontrado");
		}

		evento.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == evento.Id).ToList();
		evento.Professores = _db.CalendarioProfessorLists.Where(e => e.Evento_Id == evento.Id).ToList();

		var PerfisCognitivos = _db.Evento_Aula_PerfilCognitivo_Rels
			.Where(p => p.Evento_Aula_Id == evento.Id)
			.Include(p => p.PerfilCognitivo)
			.Select(p => p.PerfilCognitivo)
			.ToList();

		evento.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(PerfisCognitivos);

		return evento;
	}


	public CalendarioEventoList GetPseudoAula(PseudoEventoRequest request)
	{

		CalendarioEventoList? eventoAula = _db.CalendarioEventoLists.FirstOrDefault(x =>
				  x.Data == request.DataHora
				  && x.Turma_Id == request.Turma_Id);

		if (eventoAula != null)
		{
			eventoAula = this.GetEventoById(eventoAula.Id);
			return eventoAula;
		}
		else
		{
			DateTime data = request.DataHora;

			Turma? turma = _db.Turmas
				.Include(x => x.Alunos)
				.ThenInclude(x => x.Aluno_Checklist_Items)
				.Include(t => t.Professor!)
				.ThenInclude(t => t.Account)
				.Include(t => t.Sala)
				.FirstOrDefault(x => x.Id == request.Turma_Id
						&& x.Deactivated == null
						&& x.DiaSemana == (int)request.DataHora.DayOfWeek
						&& x.Horario.Value == request.DataHora.TimeOfDay);

			if (turma is null)
			{
				throw new Exception("Turma não encontrada!");
			}

			var roteiros = _roteiroService.GetAll(data.Year);
			var roteiro = roteiros.FirstOrDefault(x => x.DataInicio.Date <= data.Date && x.DataFim.Date >= data.Date);

			// Em pseudo-aulas, adicionar só os alunos da turma original
			// e após o início de sua vigência
			// e que tenham sido desativado só depois da data da aula
			List<AlunoList> alunos = _db.AlunoLists
				.Where(x => x.Turma_Id == request.Turma_Id
					&& x.DataInicioVigencia.Date <= data.Date
					&& (x.DataFimVigencia == null || x.DataFimVigencia.Value.Date >= data.Date)
					&& (x.Deactivated == null || x.Deactivated.Value.Date > data.Date))
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

				Data = request.DataHora,

				Turma_Id = turma.Id,
				Turma = turma.Nome,

				VagasDisponiveisEvento = turma.CapacidadeMaximaAlunos - alunosAtivosInTurma,
				CapacidadeMaximaEvento = turma.CapacidadeMaximaAlunos,
				AlunosAtivosEvento = alunosAtivosInTurma,

				VagasDisponiveisTurma = turma.CapacidadeMaximaAlunos - alunosAtivosInTurma,
				CapacidadeMaximaTurma = turma.CapacidadeMaximaAlunos,
				AlunosAtivosTurma = alunosAtivosInTurma,

				Professor_Id = turma?.Professor_Id,
				Professor = turma?.Professor is not null ? turma.Professor.Account.Name : "Professor indefinido",
				CorLegenda = turma?.Professor is not null ? turma.Professor.CorLegenda : "#000",

				Finalizado = false,

				Sala = turma?.Sala?.Descricao ?? "SalaIndefinida",
				Sala_Id = turma?.Sala?.Id,
				NumeroSala = turma?.Sala?.NumeroSala,
				Andar = turma?.Sala?.Andar,
			};


			pseudoAula.Alunos = _mapper.Map<List<CalendarioAlunoList>>(alunos).ToList();

			pseudoAula.Professores.Add(new CalendarioProfessorList
			{
				Id = null,
				Evento_Id = pseudoAula.Id,
				Professor_Id = (int)turma!.Professor_Id!,
				Nome = turma!.Professor!.Account.Name,
				CorLegenda = turma.Professor.CorLegenda,
				Presente = null,
				Observacao = "",
				Account_Id = turma.Professor.Account.Id,
				Telefone = turma.Professor.Account.Phone,
				ExpedienteFim = turma.Professor.ExpedienteFim,
				ExpedienteInicio = turma.Professor.ExpedienteInicio,
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

			IQueryable<Aluno> alunosInRequest = _db.Alunos
				.Where(a => dto.Alunos.Contains(a.Id) && a.Deactivated == null);

			if (alunosInRequest.Count() != dto.Alunos.Count)
			{
				throw new Exception("Aluno(s) não encontrado(s)");
			}

			IQueryable<Professor> professoresInRequest = _db.Professors
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

	// Performance: Always 4 queries for n events
	private List<CalendarioEventoList> PopulateCalendarioEvents(List<CalendarioEventoList> events)
	{
		List<int> eventoIds = events.Select(e => e.Id).ToList();

		List<int> eventoAulaIds = events
			.Where(e => e.Evento_Tipo_Id == (int)EventoTipo.Aula || e.Evento_Tipo_Id == (int)EventoTipo.AulaExtra)
			.Select(e => e.Id)
			.ToList();

		// Fazendo o possível pra otimizar, mas CalendarioAlunoList é uma view, então não lida muito bem com chaves
		var query = from a in _db.CalendarioAlunoLists
					join p in _db.Evento_Participacao_Alunos
						on a.Id equals p.Id
					where eventoIds.Contains(p.Evento_Id)
					orderby a.Aluno
					select a;

		var allAlunos = query.ToList();

		List<CalendarioProfessorList> allProfessores = _db.CalendarioProfessorLists
			.Where(p => eventoIds.Contains(p.Evento_Id))
			.ToList();

		List<Evento_Aula_PerfilCognitivo_Rel> allRels = _db.Evento_Aula_PerfilCognitivo_Rels
		   .AsNoTracking()
		   .Where(r => eventoAulaIds.Contains(r.Evento_Aula_Id))
		   .Include(r => r.PerfilCognitivo)
		   .ToList();

		// Create dictionaries that group alunos / professores by Evento_Id, will improve lookup performance
		// Key: GroupBy key Value: List of items in that group 

		var alunosDictionary = allAlunos
			.GroupBy(p => p.Evento_Id)
			.ToDictionary(g => g.Key, g => g.ToList());

		var professorDictionary = allProfessores
			.GroupBy(p => p.Evento_Id)
			.ToDictionary(g => g.Key, g => g.ToList());

		var perfisDictionary = allRels
			.GroupBy(r => r.Evento_Aula_Id)
			.ToDictionary(
				g => g.Key,
				g => g.Select(r => _mapper.Map<PerfilCognitivoModel>(r.PerfilCognitivo)).ToList()
			);

		foreach (var calendarioEvent in events)
		{
			alunosDictionary.TryGetValue(calendarioEvent.Id, out var alunosInEvent);
			calendarioEvent.Alunos = alunosInEvent ?? new List<CalendarioAlunoList>();

			professorDictionary.TryGetValue(calendarioEvent.Id, out var professoresInEvent);
			calendarioEvent.Professores = professoresInEvent ?? new List<CalendarioProfessorList>();

			if (perfisDictionary.TryGetValue(calendarioEvent.Id, out var aulaPerfisInEvent))
			{
				calendarioEvent.PerfilCognitivo = aulaPerfisInEvent;
			}
			else
			{
				calendarioEvent.PerfilCognitivo = new List<PerfilCognitivoModel>();
			}
		}

		return events;
	}

	public async Task<List<FeriadoResponse>> GetFeriados(int ano)
	{
		List<FeriadoResponse> feriados = new List<FeriadoResponse>() { };
		string token = "20487|fbPtn71wk6mjsGDWRdU8mGECDlNZhyM7";
		string url = $"https://api.invertexto.com/v1/holidays/{ano}?token={token}&state=SP";
		using (HttpClient client = new HttpClient())
		{
			try
			{
				HttpResponseMessage response = await client.GetAsync(url);
				//response.EnsureSuccessStatusCode(); // Lança uma exceção para códigos de status de erro
				string responseContent = await response.Content.ReadAsStringAsync();
				feriados = JsonSerializer.Deserialize<List<FeriadoResponse>>(responseContent);
				feriados = feriados!.OrderBy(x => x.date).ToList();

			}
			catch (HttpRequestException e)
			{
				Console.WriteLine("\nException Caught!");
				Console.WriteLine("Message :{0} ", e.Message);
			}

			return feriados;
		}
	}

	//public async Task<ResponseModel> CancelaEventosFeriado(int ano)
	//{
	//	ResponseModel response = new();

	//	try
	//	{
	//		List<FeriadoResponse> feriados = await GetFeriados(ano);
	//		List<DateTime> feriadosDates = feriados.Select(x => x.date.Date).ToList();
	//		List<int> daysOfWeekWithFeriado = feriadosDates.Select(x => (int)x.DayOfWeek).Distinct()
	//			.Order()
	//			.ToList();

	//		List<Turma> turmas = _db.Turmas
	//			.Where(x => x.Deactivated == null)
	//			.Include(x => x.Alunos)
	//			//.Include(x => x.Turma_PerfilCognitivo_Rels)
	//			//.Include(x => x.Evento_Aulas)
	//			//    .ThenInclude(x => x.Evento)
	//			.AsSplitQuery()
	//			.ToList();

	//		List<Evento> eventos = _db.Eventos
	//			.Where(x => x.Data.Year == ano)
	//			.Include(x => x.Evento_Aula)
	//			.Include(x => x.Evento_Participacao_Alunos)
	//			.ToList();


	//		List<Roteiro> roteiros = _db.Roteiro
	//			.Where(x => x.Deactivated == null)
	//			.ToList();

	//		List<Roteiro> recessos = roteiros.Where(x => x.Recesso == true
	//								&& (x.DataInicio.Year == ano || x.DataFim.Year == ano)).ToList();


	//		List<DateTime> recessoDates = recessos.SelectMany(x =>
	//			Enumerable.Range(0, 1 + x.DataFim.Subtract(x.DataInicio).Days)
	//				.Select(index => x.DataInicio.AddDays(index).Date)).ToList();

	//		List<DateTime> feriadoRecessoDates = feriadosDates.Concat(recessoDates).Distinct().OrderBy(x => x.Date).ToList();

	//		foreach (DateTime data in feriadoRecessoDates)
	//		{
	//			int dayOfWeek = (int)data.DayOfWeek;
	//			FeriadoResponse? feriado = feriados.FirstOrDefault(x => x.date.Date == data.Date);
	//			Roteiro? recesso = recessos.FirstOrDefault(x => data >= x.DataInicio.Date && data <= x.DataFim.Date);
	//			Roteiro? roteiro = roteiros.FirstOrDefault(x => data >= x.DataInicio.Date && data <= x.DataFim.Date);

	//			if (feriado is not null || recesso is not null)
	//			{
	//				string observacao = "";
	//				DateTime deactivated = TimeFunctions.HoraAtualBR();

	//				if (feriado is not null)
	//					observacao = $"Cancelamento automático <br> Feriado: {feriado.name}";
	//				else if (recesso is not null)
	//					observacao = $"Cancelamento automático <br> Recesso: {recesso.Tema}";


	//				List<Evento> eventosInDayOfWeek = eventos.Where(x => x.Data.Date == data.Date).ToList();

	//				// Cancela todos os eventos instanciados
	//				foreach (Evento evento in eventosInDayOfWeek)
	//				{
	//					if (evento.Evento_Aula is not null)
	//					{
	//						if (evento.Evento_Aula.Roteiro_Id is null)
	//						{
	//							evento.Evento_Aula.Roteiro_Id = roteiro?.Id;
	//						}
	//					}

	//					foreach (var participacao in evento.Evento_Participacao_Alunos)
	//					{
	//						participacao.StatusContato_Id = (int)StatusContato.AULA_CANCELADA;
	//					}

	//					evento.Deactivated = deactivated;
	//					evento.Observacao = observacao;
	//					_db.Eventos.Update(evento);
	//				}

	//				List<Turma> turmasInDayOfWeek = turmas.Where(t => t.DiaSemana == dayOfWeek).ToList();

	//				foreach (Turma turma in turmasInDayOfWeek)
	//				{

	//					Evento? aula = eventos.FirstOrDefault(x => x.Data.Date == data.Date
	//											&& x.Evento_Aula is not null
	//											&& x.Evento_Aula?.Turma_Id == turma.Id);

	//					List<Evento_Aula_PerfilCognitivo_Rel> eventoAulaPerfilCognitivoRels = turma.Turma_PerfilCognitivo_Rels
	//						.Select(x => new Evento_Aula_PerfilCognitivo_Rel { PerfilCognitivo_Id = x.PerfilCognitivo_Id })
	//						.ToList();

	//					// Se não tiver, instanciar uma pseudo-aula cancelada
	//					if (aula is null)
	//					{
	//						DateTime dataTurma = new(data.Year, data.Month, data.Day, turma.Horario!.Value.Hours, turma.Horario!.Value.Minutes, 0);

	//						aula = new Evento()
	//						{
	//							Evento_Tipo_Id = (int)EventoTipo.Aula,
	//							Descricao = turma.Nome, // Pseudo aulas ganham o nome da turma
	//							DuracaoMinutos = 120, // As pseudo aulas são de uma turma e duram 2h por padrão
	//							Data = dataTurma,
	//							CapacidadeMaximaAlunos = turma.CapacidadeMaximaAlunos,
	//							Sala_Id = turma.Sala_Id!.Value,
	//							Finalizado = false,
	//							Created = deactivated,
	//							LastUpdated = deactivated,
	//							Account_Created_Id = _account?.Id ?? 5,
	//							Deactivated = deactivated,
	//							Observacao = observacao,

	//							Evento_Aula = new Evento_Aula
	//							{
	//								Turma_Id = turma.Id,
	//								Roteiro_Id = roteiro?.Id,
	//								Professor_Id = turma.Professor_Id!.Value,
	//								Evento_Aula_PerfilCognitivo_Rels = eventoAulaPerfilCognitivoRels,
	//							},

	//							Evento_Participacao_Alunos = turma.Alunos
	//							.Where(x => x.DataInicioVigencia.Date < dataTurma.Date && (!x.DataFimVigencia.HasValue || x.DataFimVigencia.Value < dataTurma))
	//							.Select(x => new Evento_Participacao_Aluno
	//							{
	//								Aluno_Id = x.Id,
	//								StatusContato_Id = (int)StatusContato.AULA_CANCELADA,
	//							}).ToList(),

	//							Evento_Participacao_Professors = new List<Evento_Participacao_Professor> {
	//														 new() {
	//															 Professor_Id = turma!.Professor_Id!.Value,
	//														 },
	//													 },
	//						};

	//						_db.Eventos.Add(aula);
	//					}
	//					// Se possuir uma aula instanciada ativa no feriado, desativá-la
	//					//else if (aula is not null && aula.Deactivated is null)
	//					//{
	//					//	aula.Evento_Aula.Roteiro_Id = aula.Evento_Aula.Roteiro_Id ?? roteiro?.Id;
	//					//	aula.Deactivated = deactivated;
	//					//	aula.Observacao = observacao;
	//					//	_db.Eventos.Update(aula);
	//					//}
	//				}

	//			}
	//		}

	//		#region antigo cancelamento
	//		//         foreach (int dayOfWeek in daysOfWeekWithFeriado) 
	//		//{
	//		//             IEnumerable<Turma> turmasInDayOfWeek = turmas.Where(t => t.DiaSemana == dayOfWeek);
	//		//             List<FeriadoResponse> feriadosInDayOfWeek = feriados.Where(f => (int)f.date.DayOfWeek == dayOfWeek).ToList();

	//		//             // Para cada data de feriado
	//		//             foreach (FeriadoResponse feriado in feriadosInDayOfWeek) 
	//		//	{
	//		//		DateTime feriadoDate = feriado.date.Date;

	//		//		Roteiro? roteiro = roteiros.FirstOrDefault(x => feriadoDate.Date >= x.DataInicio.Date
	//		//													&& feriadoDate.Date <= x.DataFim.Date);
	//		//		string? nomeFeriado = feriado.name;

	//		//                 // Saber se a turma tem um evento nesse feriado
	//		//                 foreach (Turma turma in turmasInDayOfWeek) 
	//		//		{
	//		//                     Evento? aulaInFeriado = turma.Evento_Aulas
	//		//                         .Select(e => e.Evento)
	//		//                         .FirstOrDefault(e => e.Data.Date == feriadoDate.Date);

	//		//                     var eventoAulaPerfilCognitivoRels = turma.Turma_PerfilCognitivo_Rels
	//		//                         .Select(x => new Evento_Aula_PerfilCognitivo_Rel { PerfilCognitivo_Id = x.PerfilCognitivo_Id })
	//		//                         .ToList();

	//		//                     // Se não tiver, instanciar uma pseudo-aula cancelada
	//		//                     if (aulaInFeriado is null) 
	//		//			{
	//		//                         DateTime data = new(feriadoDate.Date.Year, feriadoDate.Date.Month, feriadoDate.Date.Day, turma.Horario!.Value.Hours, turma.Horario!.Value.Minutes, 0);

	//		//                         Evento pseudoAula = new()
	//		//                         {
	//		//                             Evento_Tipo_Id = (int)EventoTipo.Aula,
	//		//                             Descricao = turma.Nome, // Pseudo aulas ganham o nome da turma
	//		//                             DuracaoMinutos = 120, // As pseudo aulas são de uma turma e duram 2h por padrão
	//		//                             Data = data,
	//		//                             CapacidadeMaximaAlunos = turma.CapacidadeMaximaAlunos,
	//		//                             Sala_Id = turma.Sala_Id!.Value,
	//		//                             Finalizado = false,
	//		//                             Created = TimeFunctions.HoraAtualBR(),
	//		//                             Account_Created_Id = _account!.Id,
	//		//                             Deactivated = TimeFunctions.HoraAtualBR(),
	//		//                             Observacao = $"Cancelamento automático <br> Feriado: {nomeFeriado}",

	//		//                             Evento_Aula = new Evento_Aula
	//		//                             {
	//		//                                 Turma_Id = turma.Id,
	//		//                                 Roteiro_Id = roteiro?.Id,
	//		//                                 Professor_Id = turma.Professor_Id!.Value,
	//		//                                 Evento_Aula_PerfilCognitivo_Rels = eventoAulaPerfilCognitivoRels,
	//		//                             },

	//		//                             Evento_Participacao_Alunos = turma.Alunos.Select(x => new Evento_Participacao_Aluno
	//		//                             {
	//		//                                 Aluno_Id = x.Id,
	//		//                             }).ToList(),

	//		//                             Evento_Participacao_Professors = new List<Evento_Participacao_Professor> {
	//		//                                 new() {
	//		//                                     Professor_Id = turma!.Professor_Id!.Value,
	//		//                                 },
	//		//                             },
	//		//                         };

	//		//                         _db.Eventos.Add(pseudoAula);
	//		//                     }

	//		//                     // Se possuir uma aula instanciada ativa no feriado, desativá-la
	//		//                     else if (aulaInFeriado is not null && aulaInFeriado.Deactivated is null) 
	//		//			{
	//		//                         aulaInFeriado.Deactivated = TimeFunctions.HoraAtualBR();
	//		//                         aulaInFeriado.Observacao = $"Cancelamento automático <br> Feriado: {nomeFeriado}";
	//		//                         _db.Eventos.Update(aulaInFeriado);
	//		//                     }

	//		//                 }
	//		//             }
	//		//         }
	//		#endregion
	//		_db.SaveChanges();

	//		response.Message = "Eventos cancelados com sucesso";
	//		response.Success = true;
	//		return response;
	//	}
	//	catch (Exception e)
	//	{
	//		response.Success = false;
	//		response.Message = e.Message;
	//		return response;
	//	}
	//}

	public ResponseModel FinalizarAulaZero(FinalizarAulaZeroRequest request)
	{
		ResponseModel response = new ResponseModel { Success = false };

		try
		{
			Evento evento = _db.Eventos
				.Include(e => e.Evento_Participacao_Alunos)
				.ThenInclude(e => e.Aluno)
				.FirstOrDefault(e => e.Id == request.Evento_Id) ?? throw new Exception("Evento não encontrado.");

			List<ParticipacaoAulaZeroModel> alunosPresentes = request.Alunos.Where(model => model.Presente).ToList();

			var alunosPorTurma = alunosPresentes
				.GroupBy(a => a.Turma_Id)
				.ToDictionary(g => g.Key, g => g.Where(a => a.Turma_Id == g.Key).ToList());

			var turmaIds = request.Alunos.Select(a => a.Turma_Id).Distinct();
			var turmas = _db.TurmaLists.Where(t => turmaIds.Contains(t.Id));

			// Valida turmas
			foreach (var turmaId in alunosPorTurma.Keys)
			{
				var turma = turmas.FirstOrDefault(t => t.Id == turmaId) ?? throw new Exception($"Turma ID: '{turmaId}' não encontrada.");
				alunosPorTurma.TryGetValue(turmaId, out var alunos);

				alunos ??= []; // Caso alunos seja nulo, calcular vagas com uma lista vazia (não é pra acontecer)
				int alunosJaCadastradosNessaTurma = alunos.Count(a => a.Turma_Id == turmaId);
				int vagasNecessarias = (alunos.Count - alunosJaCadastradosNessaTurma);

				if (turma.VagasDisponiveis < (vagasNecessarias - alunosJaCadastradosNessaTurma))
				{
					throw new Exception($"Turma {turma.Nome} não possui capacidade para acomodar mais {vagasNecessarias} alunos.");
				}
			}

			// Valida perfis cognitivos
			var perfisCognitivosInEvento = alunosPresentes.Select(p => p.PerfilCognitivo_Id).ToList();

			foreach (var perfilCognitivoId in perfisCognitivosInEvento)
			{
				bool perfilCognitivoExists = _db.PerfilCognitivos.Any(p => p.Id == perfilCognitivoId);

				if (!perfilCognitivoExists)
				{
					throw new Exception("Perfil cognitivo não encontrado");
				}
			}

			// Valida kits e prepara um dicionário de lookup pra facilitar depois
			var kitsInEvento = alunosPresentes.Select(p => p.Apostila_Kit_Id).ToList();
			var kitsDictionary = new Dictionary<int, List<int>>();

			foreach (var kitId in kitsInEvento)
			{
				var apostilas = _db.Apostila_Kit_Rels
					.Include(k => k.Apostila)
					.Where(rel => rel.Apostila_Kit_Id == kitId && rel.Apostila.Ordem == 1)
					.Select(rel => rel.Apostila)
					.ToList();

				var apostilaAbaco = apostilas.Find(a => a.Apostila_Tipo_Id == (int)ApostilaTipo.Abaco) ?? throw new Exception("Apostila(s) não encontrada(s)");
				var apostilaAh = apostilas.Find(a => a.Apostila_Tipo_Id == (int)ApostilaTipo.AH) ?? throw new Exception("Apostila(s) não encontrada(s)");

				kitsDictionary.Add(kitId, [apostilaAbaco.Id, apostilaAh.Id]);
			}

			// Validations passed

			IEnumerable<Aluno> alunosInEvento = evento.Evento_Participacao_Alunos.Select(ep => ep.Aluno);

			foreach (var model in alunosPresentes)
			{
				Aluno aluno = alunosInEvento.FirstOrDefault(a => a.Id == model.Aluno_Id) ?? throw new Exception("Aluno não encontrado.");

				if (aluno.Turma_Id != null)
				{
					bool trocandoDeTurma = aluno.Turma_Id != model.Turma_Id;

					if (trocandoDeTurma)
					{
						aluno.UltimaTrocaTurma = TimeFunctions.HoraAtualBR();

						_db.Aluno_Historicos.Add(new Aluno_Historico
						{
							Aluno_Id = aluno.Id,
							Descricao = $"Aluno foi transferido da turma: '{aluno.Turma_Id}' para a turma : '{model.Turma_Id}' como resultado de um evento de aula zero.",
							Account_Id = _account!.Id,
							Data = TimeFunctions.HoraAtualBR(),
						});
					}
				}

				kitsDictionary.TryGetValue(model.Apostila_Kit_Id, out var apostilas);

				int? apostilaAbacoId = apostilas?.FirstOrDefault();
				int? apostilaAHId = apostilas?.LastOrDefault();

				aluno.PerfilCognitivo_Id = model.PerfilCognitivo_Id;
				aluno.Apostila_Kit_Id = model.Apostila_Kit_Id;
				aluno.Turma_Id = model.Turma_Id;
				aluno.Apostila_Abaco_Id = apostilaAbacoId;
				aluno.NumeroPaginaAbaco = apostilaAbacoId.HasValue ? 1 : null;
				aluno.Apostila_AH_Id = apostilaAHId;
				aluno.NumeroPaginaAH = apostilaAHId.HasValue ? 1 : null;

				_db.Alunos.Update(aluno);
			}

			string mensagemPresente = $"Aluno completou evento de aula zero '{evento.Descricao}' no dia {evento.Data:G}.";
			string mensagemAusente = $"Aluno faltou no evento de aula zero '{evento.Descricao}' no dia {evento.Data:G}";

			foreach (var model in request.Alunos)
			{
				Evento_Participacao_Aluno participacao = evento.Evento_Participacao_Alunos
					.FirstOrDefault(p => p.Id == model.Participacao_Id) ?? throw new Exception("Participação não encontrada.");

				string mensagem = model.Presente ? mensagemPresente : mensagemAusente;

				participacao.Presente = model.Presente;

				var historico = new Aluno_Historico
				{
					Account_Id = _account!.Id,
					Aluno_Id = participacao.Aluno_Id,
					Descricao = mensagem,
					Data = evento.Data,
				};

				if (!model.Presente)
				{
					Aluno aluno = alunosInEvento.FirstOrDefault(a => a.Id == model.Aluno_Id) ?? throw new Exception("Aluno não encontrado.");
					aluno.AulaZero_Id = null;
					_db.Alunos.Update(aluno);
				}

				_db.Aluno_Historicos.Add(historico);
				_db.Evento_Participacao_Alunos.Update(participacao);
			}

			evento.Observacao = request.Observacao;
			evento.Finalizado = true;
			_db.Eventos.Update(evento);

			_db.SaveChanges();

			response.Success = true;
			response.Message = "Evento de aula zero finalizado com sucesso.";
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao finalizar evento de aula zero: {ex.Message}";
		}

		return response;
	}
}
