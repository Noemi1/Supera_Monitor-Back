using System.Globalization;
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
using Supera_Monitor_Back.Models.Eventos.Dtos;
using Supera_Monitor_Back.Models.Eventos.Participacao;

namespace Supera_Monitor_Back.Services.Eventos;

public interface IEventoService
{

	public Task<CalendarioEventoList> GetPseudoAula(PseudoEventoRequest request);
	public Task<CalendarioEventoList> GetEventoById(int eventoId);
	public Task<List<FeriadoResponse>> GetFeriados(int ano);

	public Task<ResponseModel> Insert(CreateEventoRequest request, int eventoTipoId);
	public ResponseModel Update(UpdateEventoRequest request);
	public ResponseModel Cancelar(CancelarEventoRequest request);
	public ResponseModel Finalizar(FinalizarEventoRequest request);
	public ResponseModel FinalizarAulaZero(FinalizarAulaZeroRequest request);
	public ResponseModel AgendarPrimeiraAula(PrimeiraAulaRequest model);
	public ResponseModel AgendarReposicao(ReposicaoRequest model);

	public ResponseModel InsertParticipacao(InsertParticipacaoRequest request);
	public ResponseModel UpdateParticipacao(UpdateParticipacaoRequest request);
	public ResponseModel CancelarParticipacao(CancelarParticipacaoRequest request);
	public ResponseModel RemoveParticipacao(int participacaoId);

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

	public async Task<ResponseModel> Insert(CreateEventoRequest request, int eventoTipoId)
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

			IQueryable<Aluno> alunosInRequest = _db.Aluno.Where(a => a.Deactivated == null && request.Alunos.Contains(a.Id));

			if (alunosInRequest.Count() != request.Alunos.Count)
			{
				return new ResponseModel { Message = "Aluno(s) não encontrado(s)" };
			}

			IQueryable<Professor> professoresInRequest = _db.Professor.Include(p => p.Account).Where(p => p.Account.Deactivated == null && request.Professores.Contains(p.Id));

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

			_db.Evento.Add(evento);
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
				_db.Evento_Participacao_Aluno.Add(participacao);

				_db.Aluno_Historico.Add(new Aluno_Historico
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
				_db.Evento_Participacao_Professor.AddRange(participacoesProfessores);
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

			var responseObject = await this.GetEventoById(evento.Id);

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
			Evento? evento = _db.Evento
				.Include(e => e.Evento_Tipo)
				.Include(e => e.Evento_Participacao_Aluno)
				.Include(e => e.Evento_Participacao_Professor)
					.ThenInclude(e => e.Professor)
				.FirstOrDefault(e => e.Id == request.Id);

			if (evento is null)
				return new ResponseModel { Message = $"Evento não encontrada" };

			if (evento.Finalizado)
				return new ResponseModel { Message = $"Não é possivel editar {evento.Evento_Tipo.Nome.ToLower()} que foi finalizada." };

			var professores = evento.Evento_Participacao_Professor
				.Select(x => x.Professor)
				.ToList();

			var participacaoPorProfessorId = evento.Evento_Participacao_Professor
				.ToDictionary(x => x.Professor_Id, x => x);

			var professoresPorId = professores
				.ToDictionary(x => x.Id, x => x);

			var professoresRequestIds = request.Professores
				.ToDictionary(x => x, x => x);

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
					IgnoredTurmaId: null
				);
				if (hasTurmaConflict)
					return new ResponseModel { Message = $"Professor: '{professor.Account.Name}' possui uma turma nesse mesmo horário" };

				participacaoPorProfessorId.TryGetValue(professor.Id, out var participacaoProfessor);

				bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
					professorId: professor.Id,
					Data: evento.Data,
					DuracaoMinutos: request.DuracaoMinutos,
					IgnoredEventoId: evento.Id
				);

				if (hasParticipacaoConflict)
					return new ResponseModel { Message = $"Professor: {professor.Account.Name} possui participação em outro evento nesse mesmo horário" };
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
				.Where(p => !professoresRequestIds.TryGetValue(p.Professor_Id, out var participacao) )
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

	public ResponseModel InsertParticipacao(InsertParticipacaoRequest request)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Evento? evento = _db.Evento
				.Include(e => e.Evento_Aula)
				.Include(e => e.Evento_Participacao_Aluno)
				.Include(e => e.Evento_Tipo)
				.FirstOrDefault(e => e.Id == request.Evento_Id);

			ResponseModel eventValidation = ValidateEvent(evento);

			if (!eventValidation.Success)
			{
				return eventValidation;
			}

			Aluno? aluno = _db.Aluno.Find(request.Aluno_Id);

			if (aluno is null)
			{
				return new ResponseModel { Message = "Aluno não encontrado" };
			}

			// Se aluno já está inscrito, não deve poder ser inscrito novamente
			bool alunoIsAlreadyEnrolled = evento!.Evento_Participacao_Aluno.Any(p => p.Aluno_Id == aluno.Id);

			if (alunoIsAlreadyEnrolled)
			{
				return new ResponseModel { Message = "Aluno já está inscrito neste evento" };
			}

			int amountOfAlunosEnrolled = evento.Evento_Participacao_Aluno.Count;

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
					bool alunoAlreadyParticipated = _db.Evento_Participacao_Aluno
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
					_db.Aluno.Update(aluno);
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

			_db.Aluno_Historico.Add(new Aluno_Historico
			{
				Aluno_Id = aluno.Id,
				Descricao = $"Aluno foi inscrito no evento '{evento.Descricao}' do dia {evento.Data:G} - Evento é do tipo '{evento.Evento_Tipo.Nome}'",
				Account_Id = _account!.Id,
				Data = TimeFunctions.HoraAtualBR(),
			});

			_db.Evento_Participacao_Aluno.Add(newParticipacao);
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
			Evento_Participacao_Aluno? participacao = _db.Evento_Participacao_Aluno.Find(request.Participacao_Id);

			if (participacao is null)
			{
				return new ResponseModel { Message = "Participação não encontrada" };
			}

			Apostila? apostilaAbaco = _db.Apostila.Find(request.Apostila_Abaco_Id);

			if (request.Apostila_Abaco_Id.HasValue && apostilaAbaco is null)
			{
				return new ResponseModel { Message = "Apostila Ábaco não encontrada" };
			}

			Apostila? apostilaAh = _db.Apostila.Find(request.Apostila_AH_Id);

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

			_db.Evento_Participacao_Aluno.Update(participacao);
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
			Evento_Participacao_Aluno? participacao = _db.Evento_Participacao_Aluno.Include(e => e.Aluno).FirstOrDefault(p => p.Id == participacaoId);

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

			_db.Evento_Participacao_Aluno.Remove(participacao);
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
			Evento_Participacao_Aluno? participacao = _db.Evento_Participacao_Aluno.Include(e => e.Aluno).FirstOrDefault(p => p.Id == request.Participacao_Id);

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

			_db.Evento_Participacao_Aluno.Update(participacao);
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
			Evento? evento = _db.Evento
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
			List<Evento_Participacao_Aluno> participacoes = _db.Evento_Participacao_Aluno
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

			_db.Evento.Update(evento);
			_db.SaveChanges();

			var responseObject = _db.Evento.Where(e => e.Id == evento.Id)
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

	public ResponseModel Finalizar(FinalizarEventoRequest request)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Evento? evento = _db.Evento
				.Include(e => e.Evento_Participacao_Aluno)
				.ThenInclude(e => e.Aluno)
				.ThenInclude(e => e.Aluno_Checklist_Items)
				.Include(e => e.Evento_Participacao_Professor)
				.FirstOrDefault(e => e.Id == request.Evento_Id);

			var eventValidation = ValidateEvent(evento);

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
				.ToDictionary(x => x.Evento_Id, x => x);

			var participacaoProfessorPorId = evento.Evento_Participacao_Professor
				.ToDictionary(x => x.Evento_Id, x => x);

			var checklistOficinaPorAlunoId = evento.Evento_Participacao_Aluno
				.SelectMany(x => x.Aluno.Aluno_Checklist_Items)
				.Where(x => (x.Checklist_Item_Id == (int)ChecklistItemId.Comparecimento1Oficina
								|| x.Checklist_Item_Id == (int)ChecklistItemId.Comparecimento2Oficina)
								&& x.DataFinalizacao == null)
				.ToLookup(x => x.Aluno_Id);

			var checklistSuperacaoPorAlunoId = evento.Evento_Participacao_Aluno
				.SelectMany(x => x.Aluno.Aluno_Checklist_Items)
				.Where(x => x.Checklist_Item_Id == (int)ChecklistItemId.Comparecimento1Superacao 
								&& x.DataFinalizacao == null)
				.ToLookup(x => x.Aluno_Id);

			var historicosInserir = new List<Aluno_Historico>() { };
			var checklistAtualizar = new List<Aluno_Checklist_Item>() { };
			var alunosAtualizar = new List<Aluno>() { };
			// 
			// Atualiza alunos
			//
			foreach (ParticipacaoAlunoModel participacaoModel in request.Alunos)
			{
				participacaoAlunoPorId.TryGetValue(participacaoModel.Participacao_Id, out var participacao);

				if (participacao is null)
					return new ResponseModel { Message = $"Participação de aluno no evento ID: '{evento.Id}' Participacao_Id: '{participacaoModel.Participacao_Id}' não foi encontrada" };

				var validateParticipacao = ValidateParticipacao(participacaoModel, evento, existingApostilas);
				if (!validateParticipacao.Success)
					return validateParticipacao;

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
					if (evento.Evento_Tipo_Id == (int)EventoTipo.Superacao)
					{
						var item = checklistSuperacaoPorAlunoId[participacao.Aluno_Id].FirstOrDefault();
						if (item is not null)
						{
							item.Account_Finalizacao_Id = _account?.Id ?? 1;
							item.DataFinalizacao = TimeFunctions.HoraAtualBR();
							item.Observacoes = $"Checklist finalizado automaticamente. <br> Aluno compareceu na superacao do dia ${evento?.Data.ToString("dd/MM/yyyy HH:mm")}.";
							checklistAtualizar.Add(item);
						}
					}
					else if (evento.Evento_Tipo_Id == (int)EventoTipo.Oficina)
					{
						var item = checklistOficinaPorAlunoId[participacao.Aluno_Id].FirstOrDefault();
						if (item is not null)
						{
							item.Account_Finalizacao_Id = _account?.Id ?? 1;
							item.DataFinalizacao = TimeFunctions.HoraAtualBR();
							item.Observacoes = $"Checklist finalizado automaticamente. <br> Aluno compareceu na oficina do dia ${evento?.Data.ToString("dd/MM/yyyy HH:mm")}.";
							checklistAtualizar.Add(item);
						}

					}
				}
				else
				{
					historicosInserir.Add(new Aluno_Historico
					{
						Account_Id = _account!.Id,
						Aluno_Id = participacao.Aluno_Id,
						Data = evento.Data,
						Descricao = $"Aluno faltou no evento '{evento.Descricao}' no dia {evento.Data:G}"
					});
				}
			}

			// 
			// Atualiza professores
			//
			foreach (ParticipacaoProfessorModel participacaoModel in request.Professores)
			{
				participacaoProfessorPorId.TryGetValue(participacaoModel.Participacao_Id, out var participacao);

				if (participacao is null)
				{
					return new ResponseModel { Message = $"Participação de professor no evento ID: '{evento.Id}' Participacao_Id: '{participacaoModel.Participacao_Id}' não foi encontrada" };
				}

				participacao.Observacao = participacaoModel.Observacao;
				participacao.Presente = participacaoModel.Presente;

			}

			//
			// Atualiza evento
			//
			evento!.Observacao = request.Observacao;
			evento.Finalizado = true;
			evento.LastUpdated = TimeFunctions.HoraAtualBR();

			_db.Aluno_Historico.AddRange(historicosInserir);
			_db.Aluno_Checklist_Item.UpdateRange(checklistAtualizar);
			_db.Aluno.UpdateRange(alunosAtualizar);

			_db.Evento_Participacao_Aluno.UpdateRange(evento.Evento_Participacao_Aluno);
			_db.Evento_Participacao_Professor.UpdateRange(evento.Evento_Participacao_Professor);

			_db.Evento.Update(evento);

			if (evento.Evento_Aula is not null)
			{
				_db.Evento_Aula.Update(evento.Evento_Aula);
			}
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

			var alunosQueryable =
					from aluno in _db.Aluno
					join participacao in request.Alunos
						on aluno.Id equals participacao.Aluno_Id
					select aluno;

			var alunosChecklistItemQueryable =
					from checklist in _db.Aluno_Checklist_Item
					join participacao in request.Alunos
					on checklist.Aluno_Id equals participacao.Aluno_Id
					where checklist.Checklist_Item_Id == (int)ChecklistItemId.ComparecimentoAulaZero
						&& checklist.DataFinalizacao == null
					select checklist;

			var vigenciasQueryable =
				from vigencia in _db.Aluno_Turma_Vigencia
				join participacao in request.Alunos
					on vigencia.Aluno_Id equals participacao.Aluno_Id
				select vigencia;

			// Materialização

			var evento = _db.Evento.FirstOrDefault(x => x.Id == request.Evento_Id);

			if (evento is null)
			{
				throw new Exception("Evento não encontrado");
			}

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

			// Dicionarios

			var kitApostilaPorId = kitApostila
				.ToDictionary(x => x.Id, x => x);

			var alunosPorId = alunos
				.ToDictionary(x => x.Id, x => x);

			var checklistPorAluno = alunosChecklistItemQueryable
				.ToLookup(x => x.Aluno_Id);

			var vigenciasPorAluno = vigencias
				.ToLookup(x => x.Aluno_Id);

			foreach (var participacao in request.Alunos)
			{
				if (alunosPorId.TryGetValue(participacao.Aluno_Id, out var aluno))
				{
					DateTime hoje = TimeFunctions.HoraAtualBR();

					//
					// Insere Histórico
					//
					#region historico

					_db.Aluno_Historico.Add(new Aluno_Historico
					{
						Account_Id = _account?.Id ?? 1,
						Aluno_Id = participacao.Aluno_Id,
						Descricao = participacao.Presente ?
								$"Aluno compareceu na aula zero agendada no dia ${evento.Data.ToString("dd/MM/yyyy HH:mm")}" :
								$"Aluno NÃO compareceu na aula zero agendada no dia ${evento.Data.ToString("dd/MM/yyyy HH:mm")}",
						Data = hoje,
					});

					#endregion

					if (participacao.Presente == true)
					{
						//
						// Salva os dados no aluno
						//
						#region salva aluno
						Apostila_Kit? kit;
						kitApostilaPorId.TryGetValue(participacao.Apostila_Kit_Id, out kit);

						if (kit is null)
							throw new Exception($"Kit de apostila não encontrada para o aluno: ${aluno.Id}");

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

						aluno.PerfilCognitivo_Id = participacao.PerfilCognitivo_Id;
						aluno.Apostila_Kit_Id = participacao.Apostila_Kit_Id;
						aluno.Turma_Id = participacao.Turma_Id;
						aluno.Apostila_Abaco_Id = apostilaAbaco?.Id;
						aluno.Apostila_AH_Id = apostilaAH?.Id;
						aluno.NumeroPaginaAbaco = 0;
						aluno.NumeroPaginaAH = 0;


						#endregion

						//
						// Atualiza checklist "comparecimento aula zero"
						//
						#region checklist

						var item = checklistPorAluno[aluno.Id].FirstOrDefault();
						if (item is not null)
						{
							item.Account_Finalizacao_Id = _account?.Id ?? 1;
							item.DataFinalizacao = TimeFunctions.HoraAtualBR();
							item.Observacoes = $"Checklist finalizado automaticamente. <br> Aluno compareceu na aula zero do dia ${evento?.Data.ToString("dd/MM/yyyy HH:mm")}.";
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
							Aluno_Id = participacao.Aluno_Id,
							Turma_Id = participacao.Turma_Id,
							DataInicioVigencia = hoje,
						});

						#endregion
					}
					else
					{
						aluno.AulaZero_Id = null;
					}

					_db.Aluno.Update(aluno);
				}
			}


			//
			// Finaliza Aula Zero
			//
			evento.Observacao = request.Observacao;
			evento.Finalizado = true;

			_db.Evento.Update(evento);

			_db.SaveChanges();

			response.Success = true;
			response.Message = "Aula zero finalizada com sucesso.";

		}
		catch (Exception e)
		{
			response.Message = "Não foi possível finalizar aula zero: " + e.Message;
			response.Success = false;
		}

		return response;
	}

	public async Task<CalendarioEventoList> GetEventoById(int eventoId)
	{
		CalendarioEventoList? evento = _db.CalendarioEventoLists.FirstOrDefault(e => e.Id == eventoId);

		if (evento is null)
		{
			throw new Exception("Evento não encontrado");
		}



		evento.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == evento.Id).ToList();
		evento.Professores = _db.CalendarioProfessorLists.Where(e => e.Evento_Id == evento.Id).ToList();

		var PerfisCognitivos = _db.Evento_Aula_PerfilCognitivo_Rel
			.Where(p => p.Evento_Aula_Id == evento.Id)
			.Include(p => p.PerfilCognitivo)
			.Select(p => p.PerfilCognitivo)
			.ToList();

		evento.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(PerfisCognitivos);

		var feriados = await this.GetFeriados(evento.Data.Year);
		var feriado = feriados.FirstOrDefault(x => x.date.Date == evento.Data.Date);
		evento.Feriado = feriado;

		if (feriado is not null && evento.Active == true)
		{
			evento.Observacao = "Cancelamento Automático. <br> Feriado: " + feriado.name;
			evento.Deactivated = feriado.date;
		}
		return evento;
	}

	public async Task<CalendarioEventoList> GetPseudoAula(PseudoEventoRequest request)
	{
		CalendarioEventoList? eventoAula = _db.CalendarioEventoLists.FirstOrDefault(x =>
				  x.Data == request.DataHora
				  && x.Turma_Id == request.Turma_Id);

		if (eventoAula != null)
		{
			eventoAula = await this.GetEventoById(eventoAula.Id);
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

			var professorTurma = _db.ProfessorLists.FirstOrDefault(x => x.Id == turma.Professor_Id);
			if (professorTurma is null)
			{
				throw new Exception("Professor não encontrado!");
			}

			var roteirosTask = _roteiroService.GetAllAsync(request.DataHora.Year);
			var feriadosTask = this.GetFeriados(request.DataHora.Year);

			await Task.WhenAll(roteirosTask, feriadosTask);

			var roteiros = roteirosTask.Result;
			var feriados = feriadosTask.Result;

			var data = request.DataHora.Date;

			var roteiro = roteiros.FirstOrDefault(x => x.DataInicio.Date <= data && x.DataFim.Date >= data);
			var feriado = feriados.FirstOrDefault(x => x.date.Date == data);


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
			};

			if (feriado is not null)
			{
				pseudoAula.Feriado = feriado;
				pseudoAula.Deactivated = feriado.date;
				pseudoAula.Observacao = "Cancelamento Automático. <br> Feriado: " + feriado.name;
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
				feriados = JsonSerializer.Deserialize<List<FeriadoResponse>>(responseContent) ?? feriados;
				feriados = feriados!.OrderBy(x => x.date).ToList();

			}
			catch (Exception e)
			{
				Console.WriteLine("\nException Caught!");
				Console.WriteLine("Message :{0} ", e.Message);
			}

			return feriados;
		}
	}
	
	public ResponseModel AgendarReposicao(ReposicaoRequest model)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Aluno? aluno = _db.Aluno
				.Include(a => a.Pessoa)
				.FirstOrDefault(a => a.Id == model.Aluno_Id);

			if (aluno is null)
			{
				return new ResponseModel { Message = "Aluno não encontrado" };
			}

			if (aluno.Pessoa is null)
			{
				return new ResponseModel { Message = "Pessoa não encontrada" };
			}

			if (aluno.Active == false)
			{
				return new ResponseModel { Message = "O aluno está desativado" };
			}

			Evento? eventoSource = _db.Evento
				.Include(e => e.Evento_Aula)
				.Include(e => e.Evento_Participacao_Aluno)
				.FirstOrDefault(e => e.Id == model.Source_Aula_Id);

			if (eventoSource is null)
			{
				return new ResponseModel { Message = "Evento original não encontrado" };
			}

			if (eventoSource.Evento_Aula is null)
			{
				return new ResponseModel { Message = "Aula original não encontrada" };
			}

			Evento? eventoDest = _db.Evento
				.Include(e => e.Evento_Participacao_Aluno)
				.Include(e => e.Evento_Aula!)
				.ThenInclude(e => e.Turma)
				.FirstOrDefault(e => e.Evento_Aula != null && e.Id == model.Dest_Aula_Id);

			if (eventoDest is null)
			{
				return new ResponseModel { Message = "Evento destino não encontrada" };
			}

			if (eventoDest.Evento_Aula is null)
			{
				return new ResponseModel { Message = "Aula destino não encontrada" };
			}

			if (model.Source_Aula_Id == model.Dest_Aula_Id)
			{
				return new ResponseModel { Message = "Aula original e aula destino não podem ser iguais" };
			}

			if (eventoDest.Evento_Aula.Turma_Id.HasValue)
			{
				if (eventoSource.Evento_Aula.Turma_Id == eventoDest.Evento_Aula.Turma_Id)
				{
					return new ResponseModel { Message = "Aluno não pode repor aula na própria turma" };
				}
			}

			if (eventoDest.Finalizado)
			{
				return new ResponseModel { Message = "Não é possível marcar reposição para uma aula finalizada" };
			}

			//if (eventoDest.Data < TimeFunctions.HoraAtualBR()) {
			//    return new ResponseModel { Message = "Não é possível marcar reposição para uma aula no passado" };
			//}

			if (eventoDest.Deactivated != null)
			{
				return new ResponseModel { Message = "Não é possível marcar reposição em uma aula desativada" };
			}

			if (Math.Abs((eventoDest.Data - eventoSource.Data).TotalDays) > 30)
			{
				return new ResponseModel { Message = "A data da aula destino não pode ultrapassar 30 dias de diferença da aula original" };
			}

			bool registroAlreadyExists = eventoDest.Evento_Participacao_Aluno.Any(p => p.Aluno_Id == aluno.Id);

			if (registroAlreadyExists)
			{
				return new ResponseModel { Message = "Aluno já está cadastrado no evento destino" };
			}

			// A aula destino e o aluno devem compartilhar pelo menos um perfil cognitivo
			bool perfilCognitivoMatches = _db.Evento_Aula_PerfilCognitivo_Rel
				.Any(ep =>
					ep.Evento_Aula_Id == eventoDest.Id &&
					ep.PerfilCognitivo_Id == aluno.PerfilCognitivo_Id);

			if (perfilCognitivoMatches == false)
			{
				return new ResponseModel { Message = "O perfil cognitivo da aula não é adequado para este aluno" };
			}

			int registrosAtivos = eventoDest.Evento_Participacao_Aluno.Count(p => p.Deactivated == null);

			// O evento deve ter espaço para comportar o aluno
			if (registrosAtivos >= eventoDest.CapacidadeMaximaAlunos)
			{
				return new ResponseModel { Message = "Esse evento de aula já está em sua capacidade máxima" };
			}

			Evento_Participacao_Aluno? registroSource = eventoSource.Evento_Participacao_Aluno.FirstOrDefault(p =>
				p.Deactivated == null
				&& p.Aluno_Id == aluno.Id
				&& p.Evento_Id == eventoSource.Id);

			if (registroSource is null)
			{
				return new ResponseModel { Message = "Registro do aluno não foi encontrado na aula original" };
			}

			if (registroSource.Presente == true)
			{
				return new ResponseModel { Message = "Não é possível de marcar uma reposição de aula se o aluno estava presente na aula original" };
			}

			// Validations passed

			// Se for a primeira aula do aluno, atualizar a data de primeira aula para a data da aula destino
			if (eventoSource.Id == aluno.PrimeiraAula_Id)
			{
				aluno.PrimeiraAula_Id = eventoDest.Id;
			}

			_db.Aluno.Update(aluno);

			// Amarrar o novo registro à aula sendo reposta
			Evento_Participacao_Aluno registroDest = new()
			{
				Aluno_Id = aluno.Id,
				Evento_Id = eventoDest.Id,
				ReposicaoDe_Evento_Id = eventoSource.Id,
				Observacao = model.Observacao,
				Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
				NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
				Apostila_AH_Id = aluno.Apostila_AH_Id,
				NumeroPaginaAH = aluno.NumeroPaginaAH,
			};

			// Se a reposição for feita após o horário da aula, ocasiona falta
			if (TimeFunctions.HoraAtualBR() > eventoSource.Data)
			{
				registroSource.Presente = false;
			}

			// Desativar o registro da aula
			registroSource.Deactivated = TimeFunctions.HoraAtualBR();
			registroSource.StatusContato_Id = (int)StatusContato.REPOSICAO_AGENDADA;

			_db.Evento_Participacao_Aluno.Update(registroSource);
			_db.Evento_Participacao_Aluno.Add(registroDest);

			_db.Aluno_Historico.Add(new Aluno_Historico
			{
				Aluno_Id = aluno.Id,
				Descricao = $"O aluno agendou reposição do dia '{eventoSource.Data:G}' para o dia '{eventoDest.Data:G}' com a turma {eventoDest.Evento_Aula?.Turma_Id.ToString() ?? "Extra"}",
				Account_Id = _account!.Id,
				Data = TimeFunctions.HoraAtualBR(),
			});

			_db.SaveChanges();

			response.Success = true;
			response.Object = _db.CalendarioAlunoLists.FirstOrDefault(r => r.Id == registroDest.Id);
			response.Message = "Reposição agendada com sucesso";
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao inserir reposição de aula do aluno: {ex}";
		}

		return response;
	}

	public ResponseModel AgendarPrimeiraAula(PrimeiraAulaRequest model)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Aluno? aluno = _db.Aluno
				.Include(a => a.PrimeiraAula)
				.FirstOrDefault(a => a.Id == model.Aluno_Id);

			if (aluno is null)
			{
				return new ResponseModel { Message = "Aluno não encontrado" };
			}

			if (aluno.Deactivated != null)
			{
				return new ResponseModel { Message = "O aluno está desativado" };
			}

			Evento? evento = _db.Evento
				.Include(e => e.Evento_Participacao_Aluno)
				.FirstOrDefault(e => e.Id == model.Evento_Id);

			if (evento is null)
			{
				return new ResponseModel { Message = "Evento não encontrado" };
			}

			if (evento.Finalizado == true)
			{
				return new ResponseModel { Message = "Não foi possível continuar. Este evento já está finalizado." };
			}

			if (evento.Deactivated != null)
			{
				return new ResponseModel { Message = "Não foi possível continuar. Este evento se encontra desativado." };
			}

			if (aluno.PrimeiraAula != null)
			{
				return new ResponseModel { Message = $"Aluno já possui uma primeira aula marcada dia: {aluno.PrimeiraAula.Data}" };
			}

			// O aluno deve se encaixar em um dos perfis cognitivos do evento
			bool perfilCognitivoMatches = _db.Evento_Aula_PerfilCognitivo_Rel
				.Any(ep =>
					ep.Evento_Aula_Id == evento.Id &&
					ep.PerfilCognitivo_Id == aluno.PerfilCognitivo_Id);

			if (perfilCognitivoMatches == false)
			{
				return new ResponseModel { Message = "O perfil cognitivo da aula não é adequado para este aluno" };
			}

			int registrosAtivos = evento.Evento_Participacao_Aluno.Count(p => p.Deactivated == null);

			// O evento deve ter espaço para comportar o aluno
			if (registrosAtivos >= evento.CapacidadeMaximaAlunos)
			{
				return new ResponseModel { Message = "Esse evento já está em sua capacidade máxima" };
			}

			// Se o aluno já estiver no evento, precisa apenas marcar como primeira aula
			bool alunoIsAlreadyInEvent = evento.Evento_Participacao_Aluno.Any(a => a.Aluno_Id == aluno.Id);

			// Validations passed
			if (!alunoIsAlreadyInEvent)
			{
				Evento_Participacao_Aluno participacao = new()
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

			Aluno_Historico historico = new()
			{
				Aluno_Id = aluno.Id,
				Descricao = $"O aluno teve primeira aula agendada para o dia '{evento.Data:G}'",
				Account_Id = _account!.Id,
				Data = TimeFunctions.HoraAtualBR(),
			};

			_db.Aluno_Historico.Add(historico);

			aluno.PrimeiraAula_Id = evento.Id;
			_db.Aluno.Update(aluno);

			_db.SaveChanges();

			response.Success = true;
			response.Object = _db.CalendarioAlunoLists.FirstOrDefault(r => r.Id == aluno.Id);
			response.Message = "Primeira aula agendada com sucesso";
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao inserir primeira aula do aluno: {ex}";
		}

		return response;
	}

	// Metodos de validacao/suporte

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
		Evento_Participacao_Aluno? participacao = evento.Evento_Participacao_Aluno
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
