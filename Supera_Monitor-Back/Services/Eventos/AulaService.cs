using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Eventos;
using Supera_Monitor_Back.Models.Eventos.Aula;
using Supera_Monitor_Back.Models.Eventos.Participacao;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Supera_Monitor_Back.Services.Eventos;

public interface IAulaService {
    public CalendarioEventoList? GetById(int aulaId);
    public List<EventoAulaModel> GetAll();
    public List<CalendarioParticipacaoAlunoList> AlunosAulas(int ano);
    public ResponseModel InsertAulaZero(CreateAulaZeroRequest request);
    public ResponseModel InsertAulaExtra(CreateAulaExtraRequest request);
    public ResponseModel InsertAulaForTurma(CreateAulaTurmaRequest request);
    public ResponseModel Update(UpdateAulaRequest request);
}

public class AulaService : IAulaService {
    private readonly DataContext _db;
    private readonly IMapper _mapper;
    private readonly IProfessorService _professorService;
    private readonly ISalaService _salaService;
    private readonly IEventoService _eventoService;

    private readonly Account? _account;

    public AulaService(
		DataContext db, 
		IMapper mapper, 
		IProfessorService professorService, 
		ISalaService salaService, 
		IEventoService eventoService, 
		IHttpContextAccessor httpContextAccessor
	) {
        _db = db;
        _mapper = mapper;
        _professorService = professorService;
        _salaService = salaService;
        _eventoService = eventoService;
        _account = (Account?)httpContextAccessor.HttpContext?.Items["Account"];
    }

    public CalendarioEventoList? GetById(int aulaId) {
        var eventoAula = _db.CalendarioEventoLists.FirstOrDefault(e => e.Id == aulaId);

        if (eventoAula is not null) {
            var aulaPerfisCognitivos = _db.Evento_Aula_PerfilCognitivo_Rels
                .Include(p => p.PerfilCognitivo)
                .Where(e => e.Evento_Aula_Id == aulaId)
                .Select(e => e.PerfilCognitivo)
                .ToList();

            eventoAula.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(aulaPerfisCognitivos);
            eventoAula.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == aulaId).ToList();
            eventoAula.Professores = _db.CalendarioProfessorLists.Where(p => p.Evento_Id == aulaId).ToList();
        }

        return eventoAula;
    }

    public List<EventoAulaModel> GetAll() {
        List<EventoAulaModel> aulas = _db.Eventos
            .Where(e => e.Evento_Tipo_Id == (int)EventoTipo.Aula)
            .ProjectTo<EventoAulaModel>(_mapper.ConfigurationProvider)
            .AsNoTracking()
            .ToList();

        return aulas;
    }

    public ResponseModel InsertAulaForTurma(CreateAulaTurmaRequest request) {
        ResponseModel response = new() { Success = false };

        try {
            // Se Turma_Id passado na requisição for NÃO NULO, a turma deve existir
            Turma? turma = _db.Turmas.Find(request.Turma_Id);

            if (turma is null) {
                return new ResponseModel { Message = "Turma não encontrada" };
            }

            // Não devo poder registrar uma aula com um professor que não existe
            Professor? professor = _db.Professors
                .Include(p => p.Account)
                .FirstOrDefault(p => p.Id == request.Professor_Id);

            if (professor is null) {
                return new ResponseModel { Message = "Professor não encontrado" };
            }

            // Não devo poder registrar uma aula com um professor que está desativado
            if (professor.Account.Deactivated is not null) {
                return new ResponseModel { Message = "Este professor está desativado" };
            }

            // Não devo poder registrar uma aula em uma sala que não existe
            bool salaExists = _db.Salas.Any(s => s.Id == request.Sala_Id);

            if (!salaExists) {
                return new ResponseModel { Message = "Sala não encontrada" };
            }

            // Sala deve estar livre no horário do evento
            bool isSalaOccupied = _salaService.IsSalaOccupied(request.Sala_Id, request.Data, request.DuracaoMinutos, null);

            if (isSalaOccupied) {
                return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };
            }


            bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
                professorId: professor.Id,
                DiaSemana: (int)request.Data.DayOfWeek,
                Horario: request.Data.TimeOfDay,
                IgnoredTurmaId: request.Turma_Id
            );

            if (hasTurmaConflict) {
                return new ResponseModel { Message = $"Professor: '{professor.Account.Name}' possui uma turma nesse mesmo horário" };
            }

            bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
                professorId: professor.Id,
                Data: request.Data,
                DuracaoMinutos: request.DuracaoMinutos,
                IgnoredEventoId: null
            );

            if (hasParticipacaoConflict) {
                return new ResponseModel { Message = $"Professor: {professor.Account.Name} possui participação em outro evento nesse mesmo horário" };
            }

            Roteiro? roteiro;
            if (request.Roteiro_Id.HasValue && request.Roteiro_Id.Value != -1) {
                roteiro = _db.Roteiros.Find(request.Roteiro_Id);

                // Não devo poder criar aula de turma com um roteiro que não existe
                if (roteiro is null) {
                    return new ResponseModel { Message = "Roteiro não encontrado" };
                }
            }
            else {
                roteiro = _db.Roteiros.FirstOrDefault(r => request.Data.Date >= r.DataInicio.Date && request.Data.Date <= r.DataFim.Date);
            }

            // Validations passed

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
                    Roteiro_Id = roteiro?.Id,
                    Turma_Id = turma.Id,
                    Professor_Id = request.Professor_Id,
                },

                Created = TimeFunctions.HoraAtualBR(),
                LastUpdated = null,
                Deactivated = null,
                Finalizado = false,
                ReagendamentoDe_Evento_Id = null,
                Account_Created_Id = _account!.Id,
            };

            _db.Add(evento);
            _db.SaveChanges();

      

			// Em pseudo-aulas, adicionar só os alunos da turma original
			// e após o início de sua vigência
			// e que tenham sido desativado só depois da data da aula
			List<Aluno> alunos = _db.Alunos
				.Where(x => x.Turma_Id == request.Turma_Id
				&& x.DataInicioVigencia.Date <= request.Data.Date
					&& (x.DataFimVigencia == null || x.DataFimVigencia.Value.Date >= request.Data.Date)
					&& (x.Deactivated == null || x.Deactivated.Value.Date > request.Data.Date))
				.ToList();

			// Inserir participação do professor
			Evento_Participacao_Professor participacaoProfessor = new()
            {
                Evento_Id = evento.Id,
                Professor_Id = professor.Id,
            };

            _db.Evento_Participacao_Professors.Add(participacaoProfessor);
            _db.SaveChanges();

            IEnumerable<Evento_Participacao_Aluno> registros = alunos.Select(aluno => new Evento_Participacao_Aluno
            {
                Evento_Id = evento.Id,
                Aluno_Id = aluno.Id,
                Presente = null,

                Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
                NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
                Apostila_AH_Id = aluno.Apostila_AH_Id,
                NumeroPaginaAH = aluno.NumeroPaginaAH,
            });

            _db.Evento_Participacao_Alunos.AddRange(registros);
            _db.SaveChanges();

            var turmaPerfisCognitivos = _db.Turma_PerfilCognitivo_Rels
                .Where(t => t.Turma_Id == turma.Id)
                .Select(t => t.PerfilCognitivo_Id)
                .ToList();

            // Pegar os perfis cognitivos da turma e criar as entidades de Aula_PerfilCognitivo
            IEnumerable<Evento_Aula_PerfilCognitivo_Rel> eventoAulaPerfisCognitivos = turmaPerfisCognitivos.Select(perfilCognitivoId => new Evento_Aula_PerfilCognitivo_Rel
            {
                PerfilCognitivo_Id = perfilCognitivoId,
                Evento_Aula_Id = evento.Id,
            });

            _db.Evento_Aula_PerfilCognitivo_Rels.AddRange(eventoAulaPerfisCognitivos);
            _db.SaveChanges();


			CalendarioEventoList? responseObject = this.GetById(evento.Id);

			response.Message = $"Evento de aula para a turma '{turma.Nome}' registrado com sucesso";
            response.Object = responseObject;
            response.Success = true;
        }
        catch (Exception ex) {
            response.Message = $"Falha ao registrar evento de aula: {ex}";
        }

        return response;
    }

    public ResponseModel InsertAulaExtra(CreateAulaExtraRequest request) {
        ResponseModel response = new() { Success = false };

        try {
            // Não devo poder registrar uma aula com um professor que não existe
            Professor? professor = _db.Professors
                .Include(p => p.Account)
                .FirstOrDefault(p => p.Id == request.Professor_Id);

            if (professor is null) {
                return new ResponseModel { Message = "Professor não encontrado" };
            }

            // Não devo poder registrar uma aula com um professor que está desativado
            if (professor.Account.Deactivated is not null) {
                return new ResponseModel { Message = "Este professor está desativado" };
            }

            // Não devo poder registrar uma aula em uma sala que não existe
            bool salaExists = _db.Salas.Any(s => s.Id == request.Sala_Id);

            if (!salaExists) {
                return new ResponseModel { Message = "Sala não encontrada" };
            }

            // Sala deve estar livre no horário do evento
            bool isSalaOccupied = _salaService.IsSalaOccupied(request.Sala_Id, request.Data, request.DuracaoMinutos, null);

            if (isSalaOccupied) {
                return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };
            }


            // O professor associado não pode possuir conflitos de horário

            bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
                professorId: professor.Id,
                DiaSemana: (int)request.Data.DayOfWeek,
                Horario: request.Data.TimeOfDay,
                IgnoredTurmaId: null
            );

            if (hasTurmaConflict) {
                return new ResponseModel { Message = $"Professor: '{professor.Account.Name}' possui uma turma nesse mesmo horário" };
            }

            bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
                professorId: professor.Id,
                Data: request.Data,
                DuracaoMinutos: request.DuracaoMinutos,
                IgnoredEventoId: null
            );

            if (hasParticipacaoConflict) {
                return new ResponseModel { Message = $"Professor: {professor.Account.Name} possui participação em outro evento nesse mesmo horário" };
            }

            List<int> alunoIds = request.Alunos.Select(a => a.Aluno_Id).ToList();

            IQueryable<Aluno> alunosInRequest = _db.Alunos.Where(a => a.Deactivated == null && alunoIds.Contains(a.Id));

            if (alunosInRequest.Count() != request.Alunos.Count) {
                return new ResponseModel { Message = "Aluno(s) não encontrado(s)" };
            }

            if (alunosInRequest.Count() > request.CapacidadeMaximaAlunos) {
                return new ResponseModel { Message = "Número máximo de alunos excedido" };
            }


            Roteiro? roteiro;
            if (request.Roteiro_Id.HasValue && request.Roteiro_Id.Value != -1) {
                roteiro = _db.Roteiros.Find(request.Roteiro_Id);

                // Não devo poder criar aula de turma com um roteiro que não existe
                if (roteiro is null) {
                    return new ResponseModel { Message = "Roteiro não encontrado" };
                }
            }
            else {
                roteiro = _db.Roteiros.FirstOrDefault(r => request.Data.Date >= r.DataInicio.Date && request.Data.Date <= r.DataFim.Date);
            }

            // Validations passed

            Evento? eventoReagendado = _db.Eventos
                .Include(e => e.Evento_Participacao_Professors)
                .Include(e => e.Evento_Participacao_Alunos)
                .FirstOrDefault(e => e.Id == request.ReagendamentoDe_Evento_Id);

            if (eventoReagendado is not null) {
                eventoReagendado.Deactivated = TimeFunctions.HoraAtualBR();

                // Desativar participação dos alunos e professores
                foreach (var alunoReagendado in eventoReagendado.Evento_Participacao_Alunos) {
                    alunoReagendado.Deactivated = TimeFunctions.HoraAtualBR();
                }

                foreach (var professorReagendado in eventoReagendado.Evento_Participacao_Professors) {
                    professorReagendado.Deactivated = TimeFunctions.HoraAtualBR();
                }

                _db.Update(eventoReagendado);
            }

            Evento evento = new()
            {
                Data = request.Data,
                Descricao = request.Descricao ?? "Turma extra",
                Observacao = request.Observacao,
                Sala_Id = request.Sala_Id,
                DuracaoMinutos = request.DuracaoMinutos,
                CapacidadeMaximaAlunos = request.CapacidadeMaximaAlunos,

                Evento_Tipo_Id = (int)EventoTipo.AulaExtra,
                Evento_Aula = new Evento_Aula
                {
                    Turma_Id = null,
                    Roteiro_Id = roteiro?.Id,
                    Professor_Id = request.Professor_Id,
                },

                Created = TimeFunctions.HoraAtualBR(),
                LastUpdated = null,
                Deactivated = null,
                Finalizado = false,
                ReagendamentoDe_Evento_Id = eventoReagendado?.Id,
                Account_Created_Id = _account!.Id,
            };

            _db.Add(evento);
            _db.SaveChanges();

            // Inserir participação do professor
            Evento_Participacao_Professor participacaoProfessor = new()
            {
                Evento_Id = evento.Id,
                Professor_Id = professor.Id,
            };

            _db.Evento_Participacao_Professors.Add(participacaoProfessor);

            // Inserir os registros dos alunos passados na requisição
            IEnumerable<Evento_Participacao_Aluno> registros = alunosInRequest
                .AsEnumerable()
                .Select(aluno => new Evento_Participacao_Aluno
                {
                    Aluno_Id = aluno.Id,
                    Evento_Id = evento.Id,
                    Presente = null,
                    ReposicaoDe_Evento_Id = request.Alunos
                        .Where(a => a.Aluno_Id == aluno.Id)
                        .Select(a => a.ReposicaoDe_Evento_Id)
                        .FirstOrDefault(),

                    Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
                    NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
                    Apostila_AH_Id = aluno.Apostila_AH_Id,
                    NumeroPaginaAH = aluno.NumeroPaginaAH,
                });

            _db.Evento_Participacao_Alunos.AddRange(registros);

            // Pegar os perfis cognitivos passados na requisição e criar as entidades de Aula_PerfilCognitivo
            IEnumerable<Evento_Aula_PerfilCognitivo_Rel> eventoAulaPerfisCognitivos =
                request.PerfilCognitivo.Select(perfilId => new Evento_Aula_PerfilCognitivo_Rel
                {
                    Evento_Aula_Id = evento.Id,
                    PerfilCognitivo_Id = perfilId
                });

            _db.Evento_Aula_PerfilCognitivo_Rels.AddRange(eventoAulaPerfisCognitivos);
            _db.SaveChanges();

            var responseObject = _db.CalendarioEventoLists.First(e => e.Id == evento.Id);

            responseObject.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == evento.Id).ToList();
            responseObject.Professores = _db.CalendarioProfessorLists.Where(p => p.Evento_Id == evento.Id).ToList();
            responseObject.PerfilCognitivo = _db.PerfilCognitivos
                .Where(p => eventoAulaPerfisCognitivos.Select(e => e.PerfilCognitivo_Id).Contains(p.Id))
                .ProjectTo<PerfilCognitivoModel>(_mapper.ConfigurationProvider)
                .ToList();

            response.Message = "Aula extra criada com sucesso";
            response.Object = responseObject;
            response.Success = true;
        }
        catch (Exception ex) {
            response.Message = $"Falha ao registrar aula extra: {ex}";
        }

        return response;
    }

    public ResponseModel InsertAulaZero(CreateAulaZeroRequest request) {
        ResponseModel response = new() { Success = false };

        try {
            IQueryable<Aluno> alunosInRequest = _db.Alunos.Where(a => a.Deactivated == null && request.Alunos.Contains(a.Id));

            if (alunosInRequest.Count() != request.Alunos.Count) {
                return new ResponseModel { Message = "Aluno(s) não encontrado(s)" };
            }

            // Não devo poder registrar uma aula com um professor que não existe
            Professor? professor = _db.Professors
                .Include(p => p.Account)
                .FirstOrDefault(p => p.Id == request.Professor_Id);

            if (professor is null) {
                return new ResponseModel { Message = "Professor não encontrado" };
            }

            // Não devo poder registrar uma aula com um professor que está desativado
            if (professor.Account.Deactivated is not null) {
                return new ResponseModel { Message = "Este professor está desativado" };
            }

            // Não devo poder registrar uma aula em uma sala que não existe
            bool salaExists = _db.Salas.Any(s => s.Id == request.Sala_Id);

            if (!salaExists) {
                return new ResponseModel { Message = "Sala não encontrada" };
            }

			// Se o aluno já tiver participado de uma aula zero -> participacao.presente = true e aulazero.finalizado = true
			foreach (var aluno in alunosInRequest) {
                if (aluno.AulaZero_Id != null) {
					CalendarioEventoList? aulaZero = _db.CalendarioEventoLists.FirstOrDefault(x => x.Id == aluno.AulaZero_Id);
					CalendarioAlunoList? participacao = _db.CalendarioAlunoLists.FirstOrDefault(x => x.Aluno_Id == aluno.Id && x.Evento_Id == aluno.AulaZero_Id);
					
					if (aulaZero is not null && participacao is not null ) {
						if (aulaZero.Finalizado == true && participacao.Presente == true) {
                    		return new ResponseModel { Message = $"Aluno: '{participacao.Aluno}' já participou de aula zero no dia {aulaZero.Data.ToString("dd/MM/yyyy HH:mm")}." };
						}
					}
                }
            }
			
            // // Se algum aluno já participou de alguma aula zero, não deve ser possível inscrevê-lo novamente
            // foreach (var aluno in alunosInRequest) {
            //     if (aluno.AulaZero_Id != null) {
            //         return new ResponseModel { Message = $"Aluno: '{aluno.Id}' já participou de aula zero ou possui uma agendada." };
            //     }
            // }

            // Sala deve estar livre no horário do evento
            bool isSalaOccupied = _salaService.IsSalaOccupied(request.Sala_Id, request.Data, request.DuracaoMinutos, null);

            if (isSalaOccupied) {
                return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };
            }

            // O professor associado não pode possuir conflitos de horário

            bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
                professorId: professor.Id,
                DiaSemana: (int)request.Data.DayOfWeek,
                Horario: request.Data.TimeOfDay,
                IgnoredTurmaId: null
            );

            if (hasTurmaConflict) {
                return new ResponseModel { Message = $"Professor: '{professor.Account.Name}' possui uma turma nesse mesmo horário" };
            }

            bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
                professorId: professor.Id,
                Data: request.Data,
                DuracaoMinutos: request.DuracaoMinutos,
                IgnoredEventoId: null
            );

            if (hasParticipacaoConflict) {
                return new ResponseModel { Message = $"Professor: {professor.Account.Name} possui participação em outro evento nesse mesmo horário" };
            }

            // Validations passed

            Roteiro? roteiro = _db.Roteiros.FirstOrDefault(r => request.Data.Date >= r.DataInicio.Date && request.Data.Date <= r.DataFim.Date);

            Evento evento = new()
            {
                Data = request.Data,
                Descricao = request.Descricao ?? "Aula Zero",
                Observacao = request.Observacao,
                Sala_Id = request.Sala_Id,
                DuracaoMinutos = request.DuracaoMinutos,

                Evento_Tipo_Id = (int)EventoTipo.AulaZero,
                CapacidadeMaximaAlunos = alunosInRequest.Count(),
                Evento_Aula = new Evento_Aula
                {
                    Turma_Id = null,
                    Roteiro_Id = roteiro?.Id,
                    Professor_Id = request.Professor_Id,
                },

                Created = TimeFunctions.HoraAtualBR(),
                LastUpdated = null,
                Deactivated = null,
                Finalizado = false,
                ReagendamentoDe_Evento_Id = null,
                Account_Created_Id = _account!.Id,
            };

            _db.Add(evento);
            _db.SaveChanges();

            List<Evento_Participacao_Aluno> participacoesAlunos = [];
            List<Aluno_Historico> historicos = [];

            // Inserir progressos dos alunos no evento, associar evento à aula zero e gerar entidade de log
            foreach (var aluno in alunosInRequest) {

				// Novo:
				// Se o aluno jjá tiver uma aula zero agendada, cancela a participacao e se necessário a aula zero também
				if (aluno.AulaZero_Id != null) {
					CalendarioEventoList? aulaZero = _db.CalendarioEventoLists.FirstOrDefault(x => x.Id == aluno.AulaZero_Id);
					List<CalendarioAlunoList> participacoes = _db.CalendarioAlunoLists.Where(x => x.Evento_Id == aluno.AulaZero_Id).ToList();
					CalendarioAlunoList? participacao = participacoes.FirstOrDefault(x => x.Aluno_Id == aluno.Id);

					// Cancela aula zero	
					if (aulaZero is not null && participacoes.Count == 1) {
						_eventoService.Cancelar(new CancelarEventoRequest() {
							Id = aulaZero.Id,
							Observacao = $"Cancelamento automático. <br> Uma nova aula zero foi agendada para o dia {request.Data.ToString("dd/MM/yyyy HH:mm")}"
						});
					}
					// Cancela participacao
					if (participacao is not null) {
						_eventoService.CancelarParticipacao(new CancelarParticipacaoRequest() {
							Participacao_Id = participacao.Id,
							Observacao = $"Cancelamento automático. <br> Uma nova aula zero foi agendada para o dia {request.Data.ToString("dd/MM/yyyy HH:mm")}",
							ContatoObservacao = null,
							AlunoContactado = null,
							StatusContato_Id = 9,
							ReposicaoDe_Evento_Id = null
						});
						
					}
				}
				
                participacoesAlunos.Add(new Evento_Participacao_Aluno
                {
                    Aluno_Id = aluno.Id,
                    Evento_Id = evento.Id,
                    Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
                    NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
                    Apostila_AH_Id = aluno.Apostila_AH_Id,
                    NumeroPaginaAH = aluno.NumeroPaginaAH,
                });

                historicos.Add(new Aluno_Historico
                {
                    Account_Id = _account.Id,
                    Aluno_Id = aluno.Id,
                    Data = evento.Data,
                    Descricao = $"Aluno foi inscrito em um evento 'Aula Zero' no dia {evento.Data:G}"
                });

                aluno.AulaZero_Id = evento.Id;
                _db.Alunos.Update(aluno);
            }

            // Inserir participação do professor
            Evento_Participacao_Professor participacaoProfessor = new()
            {
                Evento_Id = evento.Id,
                Professor_Id = professor.Id,
            };

            _db.Evento_Participacao_Professors.Add(participacaoProfessor);
            _db.Evento_Participacao_Alunos.AddRange(participacoesAlunos);
            _db.Aluno_Historicos.AddRange(historicos);

            _db.SaveChanges();

            var responseObject = _db.CalendarioEventoLists.First(e => e.Id == evento.Id);
            responseObject.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == evento.Id).ToList();
            responseObject.Professores = _db.CalendarioProfessorLists.Where(p => p.Evento_Id == evento.Id).ToList();

            response.Message = "Aula zero criada com sucesso";
            response.Object = responseObject;
            response.Success = true;
        }
        catch (Exception ex) {
            response.Message = $"Falha ao registrar aula zero: {ex}";
        }

        return response;
    }

    public ResponseModel Update(UpdateAulaRequest request) {
        ResponseModel response = new() { Success = false };

        try {
            Evento? evento = _db.Eventos
                .Include(e => e.Evento_Aula)
                .Include(e => e.Evento_Participacao_Alunos)
                .FirstOrDefault(e => e.Id == request.Id);

            // Não devo poder atualizar uma aula que não existe
            if (evento == null) {
                return new ResponseModel { Message = "Evento não encontrado" };
            }

            if (evento.Evento_Aula == null) {
                return new ResponseModel { Message = "Aula não encontrada" };
            }

            Professor? professor = _db.Professors
                .Include(p => p.Account)
                .FirstOrDefault(p => p.Id == request.Professor_Id);

            // Não devo poder atualizar turma com um professor que não existe
            if (professor is null) {
                return new ResponseModel { Message = "Professor não encontrado" };
            }

            // Não devo poder atualizar turma com uma sala que não existe
            bool salaExists = _db.Salas.Any(s => s.Id == request.Sala_Id);

            if (salaExists == false) {
                return new ResponseModel { Message = "Sala não encontrada" };
            }

            // Sala deve estar livre no horário do evento
            bool isSalaOccupied = _salaService.IsSalaOccupied(request.Sala_Id, request.Data, request.DuracaoMinutos, evento.Id);

            if (isSalaOccupied) {
                return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };
            }


            // Não devo poder atualizar turma com um professor que está desativado
            if (professor.Account.Deactivated is not null) {
                return new ResponseModel { Message = "Professor está desativado" };
            }

            // Se estou trocando de professor, o novo professor não pode estar ocupado nesse dia da semana / horário
            if (evento.Evento_Aula.Professor_Id != request.Professor_Id) {
                bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
                    professorId: professor.Id,
                    DiaSemana: (int)request.Data.DayOfWeek,
                    Horario: request.Data.TimeOfDay,
                    IgnoredTurmaId: evento.Evento_Aula.Turma_Id
                );

                if (hasTurmaConflict) {
                    return new ResponseModel { Message = $"Professor: '{professor.Account.Name}' possui uma turma nesse mesmo horário" };
                }

                Evento_Participacao_Professor? participacaoProfessor = _db.Evento_Participacao_Professors.FirstOrDefault(p => p.Evento_Id == evento.Id);

                bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
                    professorId: professor.Id,
                    Data: request.Data,
                    DuracaoMinutos: request.DuracaoMinutos,
                    IgnoredEventoId: evento.Id
                );

                if (hasParticipacaoConflict) {
                    return new ResponseModel { Message = $"Professor: {professor.Account.Name} possui participação em outro evento nesse mesmo horário" };
                }
            }

            // Alguns tipos de evento não precisam validar quantidade de alunos
            if (evento.Evento_Tipo_Id != (int)EventoTipo.AulaZero && evento.Evento_Tipo_Id != (int)EventoTipo.Superacao) {
                int alunosInEvento = evento.Evento_Participacao_Alunos.Count(e => e.Deactivated == null);

                if (request.CapacidadeMaximaAlunos < alunosInEvento) {
                    return new ResponseModel { Message = "Número máximo de alunos excedido" };
                }
            }

            Roteiro? roteiro;
            if (request.Roteiro_Id.HasValue && request.Roteiro_Id.Value != -1) {
                roteiro = _db.Roteiros.Find(request.Roteiro_Id);

                // Não devo poder criar aula de turma com um roteiro que não existe
                if (roteiro is null) {
                    return new ResponseModel { Message = "Roteiro não encontrado" };
                }
            }
            else {
                roteiro = _db.Roteiros.FirstOrDefault(r => request.Data.Date >= r.DataInicio.Date && request.Data.Date <= r.DataFim.Date);
            }

            // Validations passed

            evento.Observacao = request.Observacao ?? request.Observacao;
            evento.Descricao = request.Descricao ?? evento.Descricao;
            evento.Sala_Id = request.Sala_Id;
            evento.Data = request.Data;
            evento.DuracaoMinutos = request.DuracaoMinutos;
            evento.CapacidadeMaximaAlunos = request.CapacidadeMaximaAlunos;

            evento.LastUpdated = TimeFunctions.HoraAtualBR();

            evento.Evento_Aula!.Professor_Id = request.Professor_Id;
            evento.Evento_Aula.Turma_Id = request.Turma_Id;
            evento.Evento_Aula.Roteiro_Id = roteiro?.Id;

            _db.Update(evento);
            _db.SaveChanges();

            // Por simplicidade, remover os perfis cognitivos anteriores
            List<Evento_Aula_PerfilCognitivo_Rel> perfisToRemove = _db.Evento_Aula_PerfilCognitivo_Rels
                .Where(p => p.Evento_Aula_Id == evento.Id)
                .ToList();

            _db.RemoveRange(perfisToRemove);
            _db.SaveChanges();

            // Pegar os perfis cognitivos passados no request e criar as entidades de Aula_PerfilCognitivo
            IEnumerable<Evento_Aula_PerfilCognitivo_Rel> eventoAulaPerfisCognitivos = request.PerfilCognitivo.Select(perfilId => new Evento_Aula_PerfilCognitivo_Rel
            {
                Evento_Aula_Id = evento.Id,
                PerfilCognitivo_Id = perfilId
            });

            _db.AddRange(eventoAulaPerfisCognitivos);
            _db.SaveChanges();

            Evento_Participacao_Professor? participacaoToRemove = _db.Evento_Participacao_Professors.FirstOrDefault(p => p.Evento_Id == evento.Id && p.Professor_Id == professor.Id);

            if (participacaoToRemove is not null) {
                _db.Evento_Participacao_Professors.Remove(participacaoToRemove);
            }

            // Remover participação antiga e inserir nova participação do professor
            Evento_Participacao_Professor newParticipacaoProfessor = new()
            {
                Evento_Id = evento.Id,
                Professor_Id = professor.Id,
            };

            _db.Evento_Participacao_Professors.Add(newParticipacaoProfessor);

            _db.SaveChanges();

            var responseObject = _db.CalendarioEventoLists.First(e => e.Id == evento.Id);

            responseObject.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == evento.Id).ToList();
            responseObject.Professores = _db.CalendarioProfessorLists.Where(p => p.Evento_Id == evento.Id).ToList();

            responseObject.PerfilCognitivo = _db.PerfilCognitivos
                .Where(p => eventoAulaPerfisCognitivos.Select(e => e.PerfilCognitivo_Id).Contains(p.Id))
                .ProjectTo<PerfilCognitivoModel>(_mapper.ConfigurationProvider)
                .ToList();

            response.Message = "Evento de aula atualizado com sucesso";
            response.Object = responseObject;
            response.Success = true;
        }
        catch (Exception ex) {
            response.Message = $"Falha ao atualizar evento de aula: {ex}";
        }

        return response;
    }

    public List<CalendarioParticipacaoAlunoList> AlunosAulas(int ano) {
        DateTime intervaloDe = new(ano, 1, 1);
        DateTime intervaloAte = intervaloDe.AddYears(1);

        List<CalendarioParticipacaoAlunoList> response = _db.CalendarioParticipacaoAlunoLists
            .Where(x => x.Data.Year == ano)
            .ToList();

        List<Turma> turmas = _db.Turmas
            .Where(t => t.Deactivated == null)
            .Include(t => t.Alunos!).ThenInclude(x => x.Apostila_Abaco)
            .Include(t => t.Alunos!).ThenInclude(x => x.Apostila_AH)
            .Include(t => t.Professor!).ThenInclude(t => t.Account)
            .Include(t => t.Sala)
            .ToList();

        List<AlunoList> alunos = _db.AlunoLists.Where(t => t.Deactivated == null)
            .ToList();

        List<Roteiro> roteiros = _db.Roteiros
            .Where(x => x.DataInicio.Date >= intervaloDe.Date && x.DataFim.Date <= intervaloAte.Date)
            .ToList();

        #region Roteiros
        List<string> meses = new() { "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho", "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro" };
        int index = 1;

        //
        // Monta os roteiros
        // Se o mês tiver menos que 4 roteiros cadastrados, completa com 4
        //
        foreach (string mes in meses) {
            var roteirosMes = roteiros.Where(x => x.DataInicio.Month == index).ToList();
            Roteiro lastRoteiro;
            int lastSemana;
            List<DateTime> lastIntervalo = new List<DateTime>();


            if (roteirosMes.Count > 0) {
                lastRoteiro = roteirosMes[roteirosMes.Count - 1];
                lastSemana = lastRoteiro.Semana;
                lastIntervalo = new List<DateTime>() { lastRoteiro.DataInicio, lastRoteiro.DataFim };
            }
            else {
                DateTime inicio = new DateTime(ano, index, 1);
                DateTime fim = inicio.AddDays(7);
                lastIntervalo = new List<DateTime>() { inicio, fim };
                lastSemana = 0;
            }

            if (roteirosMes.Count < 4) {
                int diff = 4 - roteirosMes.Count;

                for (int i = 1; i <= diff; i++) {
                    roteiros.Add(new Roteiro()
                    {
                        Id = -1,
                        Account_Created_Id = -1,
                        CorLegenda = "black",
                        Semana = roteiros.Max(r => r.Semana) + 1,
                        Tema = "Tema indefinido",
                        Created = DateTime.Now,
                        LastUpdated = null,
                        Deactivated = null,
                        DataInicio = lastIntervalo[0].AddDays(7),
                        DataFim = lastIntervalo[1].AddDays(7),
                        Evento_Aulas = new List<Evento_Aula>() { }
                    });
                }
            }
            index += 1;
        }
        #endregion


        roteiros = roteiros.OrderBy(x => x.DataInicio).ToList();

        List<int> semanas = roteiros.Select(r => r.Semana).ToList();

        foreach (Roteiro roteiro in roteiros) {

            foreach (Turma turma in turmas) {
                CalendarioParticipacaoAlunoList? existe = response.FirstOrDefault(a =>
                    a.Roteiro_Id == roteiro.Id &&
                    a.Turma_Id == turma.Id);

                if (existe is null) {
                    int diasAteDiaSemana = ((int)turma.DiaSemana - (int)roteiro.DataInicio.DayOfWeek + 7) % 7;
                    DateTime proximoDia = roteiro.DataInicio.AddDays(diasAteDiaSemana == 0 ? 7 : diasAteDiaSemana);
                    var data = proximoDia;

                    var alunosTurma = alunos.Where(x => x.Turma_Id == turma.Id).ToList();
                    foreach (var aluno in alunosTurma) {
                        //if ((!aluno.DataInicioVigencia.HasValue || aluno.DataInicioVigencia.Value.Date <= data.Date)
                        //    && (!aluno.DataFimVigencia.HasValue || aluno.DataFimVigencia.Value.Date >= data.Date)) {
                        var datx = new DateTime(data.Year, data.Month, data.Day, turma!.Horario!.Value.Hours, turma.Horario.Value.Minutes, 0);

                        CalendarioParticipacaoAlunoList pseudoParticipacao = new CalendarioParticipacaoAlunoList
                        {
                            Id = -1,
                            Aluno_Id = aluno.Id,
                            Aluno = aluno.Nome!,
                            Checklist_Id = aluno.Checklist_Id,
                            Checklist = aluno.Checklist,
                            Evento_Id = -1,
                            Evento_Tipo_Id = (int)EventoTipo.Aula,
                            Data = new DateTime(data.Year, data.Month, data.Day, turma!.Horario!.Value.Hours, turma.Horario.Value.Minutes, 0),
                            Descricao = turma.Nome,
                            DuracaoMinutos = 120,
                            Finalizado = false,
                            Roteiro_Id = roteiro.Id,

                            Presente = null,
                            ReposicaoDe_Evento_Id = null,
                            ReagendamentoDe_Evento_Id = null,
                            Deactivated = null,
                            Observacao = null,

                            Apostila_Abaco = aluno.Apostila_Abaco,
                            Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
                            NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,

                            Apostila_AH = aluno.Apostila_AH,
                            Apostila_AH_Id = aluno.Apostila_AH_Id,
                            NumeroPaginaAH = aluno.NumeroPaginaAH,

                            Turma_Id = turma.Id,
                            Turma = turma.Nome,
                            CapacidadeMaximaAlunos = turma.CapacidadeMaximaAlunos,

                            Professor_Id = turma?.Professor_Id,
                            Professor = turma?.Professor is not null ? turma.Professor.Account.Name : "Professor indefinido",
                            CorLegenda = turma?.Professor is not null ? turma.Professor.CorLegenda : "#000",

                            Sala_Id = turma?.Sala_Id,
                            NumeroSala = turma?.Sala?.NumeroSala,
                            Andar = turma?.Sala?.Andar,

                        };
                        response.Add(pseudoParticipacao);
                        //}
                    }

                }
            }

        }


        response = response.OrderBy(x => x.Data).ToList();

        return response;
    }
}
