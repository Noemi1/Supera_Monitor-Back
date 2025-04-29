using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Eventos.Aula;

namespace Supera_Monitor_Back.Services.Eventos;

public interface IAulaService {
    public EventoAulaModel GetById(int aulaId);
    public List<EventoAulaModel> GetAll();
    public List<CalendarioParticipacaoAlunoList> AlunosAulas(int ano);

    public ResponseModel InsertAulaZero(CreateAulaZeroRequest request);
    public ResponseModel InsertAulaExtra(CreateAulaExtraRequest request);
    public ResponseModel InsertAulaForTurma(CreateAulaTurmaRequest request);
    public ResponseModel Update(UpdateAulaRequest request);

    public ResponseModel Chamada(ChamadaRequest request);
}

public class AulaService : IAulaService {
    private readonly DataContext _db;
    private readonly IMapper _mapper;
    private readonly IProfessorService _professorService;
    private readonly ISalaService _salaService;

    private readonly Account? _account;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AulaService(DataContext db, IMapper mapper, IProfessorService professorService, ISalaService salaService, IHttpContextAccessor httpContextAccessor) {
        _db = db;
        _mapper = mapper;
        _professorService = professorService;
        _salaService = salaService;
        _httpContextAccessor = httpContextAccessor;
        _account = (Account?)_httpContextAccessor.HttpContext?.Items["Account"];
    }

    public EventoAulaModel GetById(int aulaId) {
        var aula = _db.Eventos
            .Where(e => e.Id == aulaId)
            .ProjectTo<EventoAulaModel>(_mapper.ConfigurationProvider)
            .AsNoTracking()
            .FirstOrDefault();

        return aula is null ? throw new Exception("Aula não encontrada") : aula;
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

            // Não devo poder criar turma com um roteiro que não existe
            bool roteiroExists = _db.Roteiros.Any(r => r.Id == request.Roteiro_Id);

            if (!roteiroExists) {
                return new ResponseModel { Message = "Roteiro não encontrado" };
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

            // Validations passed

            Evento evento = new()
            {
                Data = request.Data,
                Descricao = turma.Nome ?? request.Descricao ?? "Sem descrição",
                Observacao = request?.Observacao,
                Sala_Id = request.Sala_Id,
                DuracaoMinutos = request.DuracaoMinutos,

                Evento_Tipo_Id = (int)EventoTipo.Aula,
                Evento_Aula = new Evento_Aula
                {
                    Roteiro_Id = request.Roteiro_Id,
                    Turma_Id = turma.Id,
                    CapacidadeMaximaAlunos = turma.CapacidadeMaximaAlunos,
                    Professor_Id = request.Professor_Id,
                },

                Created = TimeFunctions.HoraAtualBR(),
                LastUpdated = null,
                Deactivated = null,
                Finalizado = false,
                ReagendamentoDe_Evento_Id = null,
                Account_Created_Id = _account.Id,
            };

            _db.Add(evento);
            _db.SaveChanges();

            // Inserir os registros dos alunos originais da turma na aula recém criada
            List<Aluno> alunos = _db.Alunos.Where(a =>
                a.Turma_Id == turma.Id &&
                a.Deactivated == null)
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

            // Pegar os perfis cognitivos passados no request e criar as entidades de Aula_PerfilCognitivo
            IEnumerable<Evento_Aula_PerfilCognitivo_Rel> eventoAulaPerfisCognitivos = request.PerfilCognitivo.Select(perfilCognitivoId => new Evento_Aula_PerfilCognitivo_Rel
            {
                PerfilCognitivo_Id = perfilCognitivoId,
                Evento_Aula_Id = evento.Id,
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

            // Não devo poder criar turma com um roteiro que não existe
            bool roteiroExists = _db.Roteiros.Any(r => r.Id == request.Roteiro_Id);

            if (!roteiroExists) {
                return new ResponseModel { Message = "Roteiro não encontrado" };
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

            IQueryable<Aluno> alunosInRequest = _db.Alunos.Where(a => a.Deactivated == null && request.Alunos.Contains(a.Id));

            if (alunosInRequest.Count() != request.Alunos.Count) {
                return new ResponseModel { Message = "Aluno(s) não encontrado(s)" };
            }

            // Validations passed

            Evento evento = new()
            {
                Data = request.Data,
                Descricao = request.Descricao ?? "Turma extra",
                Observacao = request.Observacao,
                Sala_Id = request.Sala_Id,
                DuracaoMinutos = request.DuracaoMinutos,

                Evento_Tipo_Id = (int)EventoTipo.AulaExtra,
                Evento_Aula = new Evento_Aula
                {
                    Roteiro_Id = request.Roteiro_Id,
                    Turma_Id = null,
                    CapacidadeMaximaAlunos = request.CapacidadeMaximaAlunos,
                    Professor_Id = request.Professor_Id,
                },

                Created = TimeFunctions.HoraAtualBR(),
                LastUpdated = null,
                Deactivated = null,
                Finalizado = false,
                ReagendamentoDe_Evento_Id = null,
                Account_Created_Id = _account.Id,
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
            _db.SaveChanges();

            // Inserir os registros dos alunos passados na requisição
            IEnumerable<Evento_Participacao_Aluno> registros = alunosInRequest.Select(aluno => new Evento_Participacao_Aluno
            {
                Aluno_Id = aluno.Id,
                Evento_Id = evento.Id,
                Presente = null,

                Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
                NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
                Apostila_AH_Id = aluno.Apostila_AH_Id,
                NumeroPaginaAH = aluno.NumeroPaginaAH,
            });

            _db.Evento_Participacao_Alunos.AddRange(registros);

            // Pegar os perfis cognitivos passados na requisição e criar as entidades de Aula_PerfilCognitivo
            IEnumerable<Evento_Aula_PerfilCognitivo_Rel> eventoAulaPerfisCognitivos = request.PerfilCognitivo.Select(perfilId => new Evento_Aula_PerfilCognitivo_Rel
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

            Aluno? aluno = _db.Alunos.FirstOrDefault(a => a.Id == request.Aluno_Id);

            if (aluno is null) {
                return new ResponseModel { Message = "Aluno não encontrado" };
            }

            // Não devo poder registrar uma aula em uma sala que não existe
            bool salaExists = _db.Salas.Any(s => s.Id == request.Sala_Id);

            if (!salaExists) {
                return new ResponseModel { Message = "Sala não encontrada" };
            }

            // Se o aluno já participou de alguma aula zero, então não deve ser possível inscrevê-lo novamente
            bool alunoAlreadyParticipated = _db.Evento_Participacao_Alunos
                .Include(p => p.Evento)
                .Any(p =>
                    p.Aluno_Id == aluno.Id
                    && p.Evento.Evento_Tipo_Id == (int)EventoTipo.AulaZero
                    && p.Evento.Deactivated == null);

            if (alunoAlreadyParticipated) {
                return new ResponseModel { Message = "Aluno já participou de uma aula zero" };
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

            // Validations passed

            Roteiro? roteiro = _db.Roteiros.FirstOrDefault(r => request.Data.Date >= r.DataInicio.Date && request.Data.Date <= r.DataFim.Date);

            Evento evento = new()
            {
                Data = request.Data,
                Descricao = request.Descricao ?? "Aula Zero",
                Observacao = request?.Observacao,
                Sala_Id = request.Sala_Id,
                DuracaoMinutos = request.DuracaoMinutos,

                Evento_Tipo_Id = (int)EventoTipo.AulaZero,
                Evento_Aula = new Evento_Aula
                {
                    Turma_Id = null,
                    CapacidadeMaximaAlunos = 1,
                    Professor_Id = request.Professor_Id,
                    Roteiro_Id = roteiro?.Id,
                },

                Created = TimeFunctions.HoraAtualBR(),
                LastUpdated = null,
                Deactivated = null,
                Finalizado = false,
                ReagendamentoDe_Evento_Id = null,
                Account_Created_Id = _account.Id,
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

            // Inserir o registro do aluno passado na requisição
            Evento_Participacao_Aluno registro = new()
            {
                Aluno_Id = aluno.Id,
                Evento_Id = evento.Id,
                Presente = null,

                Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
                NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
                Apostila_AH_Id = aluno.Apostila_AH_Id,
                NumeroPaginaAH = aluno.NumeroPaginaAH,
            };

            _db.Evento_Participacao_Alunos.Add(registro);
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
            Evento? evento = _db.Eventos.Include(e => e.Evento_Aula).FirstOrDefault(e => e.Id == request.Id);

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

            // Não devo poder atualizar turma com um roteiro que não existe
            bool roteiroExists = _db.Roteiros.Any(r => r.Id == request.Roteiro_Id);

            if (!roteiroExists) {
                return new ResponseModel { Message = "Roteiro não encontrado" };
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

            // Validations passed

            evento.Observacao = request.Observacao ?? request.Observacao;
            evento.Descricao = request.Descricao ?? evento.Descricao;
            evento.Sala_Id = request.Sala_Id;
            evento.Data = request.Data;
            evento.DuracaoMinutos = request.DuracaoMinutos;

            evento.LastUpdated = TimeFunctions.HoraAtualBR();

            evento.Evento_Aula!.Professor_Id = request.Professor_Id;
            evento.Evento_Aula.Turma_Id = request.Turma_Id;

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

    public ResponseModel Chamada(ChamadaRequest request) {
        ResponseModel response = new() { Success = false };

        try {
            Evento? evento = _db.Eventos
                .Include(e => e.Evento_Aula)
                .Include(e => e.Evento_Participacao_Professors)
                .FirstOrDefault(e => e.Id == request.Evento_Id);

            // Não devo poder realizar a chamada em um evento de aula que não existe
            if (evento is null) {
                return new ResponseModel { Message = "Evento não encontrado" };
            }

            if (evento.Evento_Aula is null) {
                return new ResponseModel { Message = "Aula não encontrada" };
            }

            // Não devo poder realizar a chamada em uma aula que está finalizada
            if (evento.Finalizado) {
                return new ResponseModel { Message = "Aula já está finalizada" };
            }

            var participacaoProfessor = _db.Evento_Participacao_Professors.FirstOrDefault(p => p.Professor_Id == evento.Evento_Aula.Professor_Id);

            if (participacaoProfessor is null) {
                return new ResponseModel { Message = "Professor ministrante não encontrado" };
            }

            // Validations passed

            List<int> listParticipacoes = request.Registros.Select(r => r.Participacao_Id).ToList();

            Dictionary<int, Evento_Participacao_Aluno> registros = _db.Evento_Participacao_Alunos
                .Include(e => e.Aluno)
                .Where(e => listParticipacoes.Contains(e.Id))
                .ToDictionary(p => p.Id);

            // Processar os registros / alunos / apostilas
            foreach (var item in request.Registros) {
                // Pegar o registro do aluno na aula - Se existir, coloca na variável registro
                registros.TryGetValue(item.Participacao_Id, out var registro);

                if (registro is null) {
                    continue;
                }

                registro.Apostila_AH_Id = item.Apostila_Ah_Id;
                registro.NumeroPaginaAH = item.Numero_Pagina_Ah;
                registro.Apostila_Abaco_Id = item.Apostila_Abaco_Id;
                registro.NumeroPaginaAbaco = item.Numero_Pagina_Abaco;
                registro.Presente = registro.Presente;
                registro.Observacao = registro.Observacao;

                if (registro.Presente == true) {
                    registro.Aluno.Apostila_Abaco_Id = item.Apostila_Abaco_Id;
                    registro.Aluno.Apostila_AH_Id = item.Apostila_Ah_Id;
                    registro.Aluno.NumeroPaginaAbaco = item.Numero_Pagina_Abaco;
                    registro.Aluno.NumeroPaginaAH = item.Numero_Pagina_Ah;
                }

                _db.Update(registro);
            }

            evento.Observacao = request.Observacao;
            evento.Finalizado = true;

            _db.Eventos.Update(evento);

            participacaoProfessor.Presente = true;

            _db.Evento_Participacao_Professors.Update(participacaoProfessor);
            _db.SaveChanges();

            response.Message = "Chamada realizada com sucesso";
            response.Object = _db.CalendarioEventoLists.FirstOrDefault(e => e.Id == evento.Id);
            response.Success = true;
        }
        catch (Exception ex) {
            response.Message = $"Falha ao realizar a chamada: {ex}";
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
        int semanaCount = 0;

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

                if (existe is not null) {
                    Console.WriteLine("");
                }

                if (existe is null) {
                    int diasAteDiaSemana = ((int)turma.DiaSemana - (int)roteiro.DataInicio.DayOfWeek + 7) % 7;
                    DateTime proximoDia = roteiro.DataInicio.AddDays(diasAteDiaSemana == 0 ? 7 : diasAteDiaSemana);
                    var data = proximoDia;

                    Console.WriteLine($"ProximoDia: {proximoDia}");

                    var alunosTurma = alunos.Where(x => x.Turma_Id == turma.Id).ToList();
                    foreach (var aluno in alunosTurma) {
                        //if ((!aluno.DataInicioVigencia.HasValue || aluno.DataInicioVigencia.Value.Date <= data.Date)
                        //    && (!aluno.DataFimVigencia.HasValue || aluno.DataFimVigencia.Value.Date >= data.Date)) {
                        var datx = new DateTime(data.Year, data.Month, data.Day, turma!.Horario!.Value.Hours, turma.Horario.Value.Minutes, 0);
                        Console.WriteLine($"Criando pseudo participacao: {datx:g}");

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
