using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Eventos;
using Supera_Monitor_Back.Models.Eventos.Aula;

namespace Supera_Monitor_Back.Services.Eventos;

public interface IEventoService {
    public ResponseModel Insert(CreateEventoRequest request, int eventoTipoId);
    public ResponseModel Update(UpdateEventoRequest request);
}

public class EventoService : IEventoService {
    private readonly DataContext _db;
    private readonly IMapper _mapper;
    private readonly IProfessorService _professorService;

    private readonly Account? _account;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public EventoService(DataContext db, IMapper mapper, IProfessorService professorService, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _mapper = mapper;
        _professorService = professorService;
        _httpContextAccessor = httpContextAccessor;
        _account = ( Account? )_httpContextAccessor.HttpContext?.Items["Account"];
    }

    public ResponseModel Insert(CreateEventoRequest request, int eventoTipoId)
    {
        ResponseModel response = new() { Success = false };

        try {
            // Validação de quantidades de alunos/professores para cada tipo de evento
            switch (eventoTipoId) {
            case ( int )EventoTipo.Reuniao:
                if (request.Alunos.Count != 0) {
                    return new ResponseModel { Message = "Um evento de reunião não pode ter alunos associados" };
                }
                break;

            case ( int )EventoTipo.Oficina:
                break;

            case ( int )EventoTipo.Superacao:
                if (request.Alunos.Count != 1) {
                    return new ResponseModel { Message = "Um evento de Superação só pode ter um aluno associado" };
                }
                break;

            default:
                return new ResponseModel { Message = "Internal Server Error : 'Tipo de evento inválido'" };
            };

            IQueryable<Aluno> alunosInRequest = _db.Alunos.Where(a => a.Deactivated == null && request.Alunos.Contains(a.Id));

            if (alunosInRequest.Count() != request.Alunos.Count) {
                return new ResponseModel { Message = "Aluno(s) não encontrado(s)" };
            }

            IQueryable<Professor> professoresInRequest = _db.Professors.Include(p => p.Account).Where(p => p.Account.Deactivated == null && request.Professores.Contains(p.Id));

            if (professoresInRequest.Count() != request.Professores.Count) {
                return new ResponseModel { Message = "Professor(es) não encontrado(s)" };
            }

            if (request.Data < TimeFunctions.HoraAtualBR()) {
                return new ResponseModel { Message = "Data do evento não pode ser no passado" };
            }

            foreach (var professor in professoresInRequest) {
                bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
                    professorId: professor.Id,
                    DiaSemana: ( int )request.Data.DayOfWeek,
                    Horario: request.Data.TimeOfDay,
                    IgnoredTurmaId: null
                );

                if (hasTurmaConflict) {
                    return new ResponseModel { Message = $"Professor: '{professor.Account.Name}' possui uma turma nesse mesmo horário" };
                }

                bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
                    professorId: professor.Id,
                    Data: request.Data,
                    IgnoredParticipacaoId: null
                );

                if (hasParticipacaoConflict) {
                    return new ResponseModel { Message = $"Professor: {professor.Account.Name} possui participação em outro evento nesse mesmo horário" };
                }
            }

            // Não devo poder registrar um evento em uma sala que não existe
            bool salaExists = _db.Salas.Any(s => s.Id == request.Sala_Id);

            if (!salaExists) {
                return new ResponseModel { Message = "Sala não encontrada" };
            }

            // Não devo poder registrar um evento em uma sala que está ocupada num intervalo de 2 horas antes ou depois
            var twoHoursBefore = request.Data.AddHours(-2);
            var twoHoursAfter = request.Data.AddHours(2);

            bool isSalaOccupied = _db.Eventos.Any(e =>
                e.Deactivated == null
                && e.Sala_Id == request.Sala_Id
                && e.Data.Date == request.Data.Date
                && e.Data > twoHoursBefore
                && e.Data < twoHoursAfter);

            if (isSalaOccupied) {
                return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };
            }

            // Validations passed

            Evento evento = new() {
                Data = request.Data,
                Descricao = request.Descricao ?? "Evento sem descrição",
                Observacao = request.Observacao ?? "Sem observação",
                DuracaoMinutos = request.DuracaoMinutos,
                Finalizado = false,

                Sala_Id = request.Sala_Id,

                Created = TimeFunctions.HoraAtualBR(),
                LastUpdated = null,
                Deactivated = null,
                Evento_Tipo_Id = eventoTipoId,
                Account_Created_Id = _account.Id
            };

            _db.Eventos.Add(evento);
            _db.SaveChanges();

            // Adicionar as participações dos envolvidos no evento - Alunos e Professores
            var participacoesAlunos = alunosInRequest.Select(aluno => new Evento_Participacao_Aluno {
                Aluno_Id = aluno.Id,
                Evento_Id = evento.Id,
            });

            if (participacoesAlunos.Any()) {
                _db.Evento_Participacao_Alunos.AddRange(participacoesAlunos);
            }

            var participacoesProfessores = professoresInRequest.Select(aluno => new Evento_Participacao_Professor {
                Professor_Id = aluno.Id,
                Evento_Id = evento.Id,
            });

            if (participacoesProfessores.Any()) {
                _db.Evento_Participacao_Professors.AddRange(participacoesProfessores);
            }

            _db.SaveChanges();

            var responseObject = _db.Eventos.Where(e => e.Id == evento.Id)
                .ProjectTo<EventoModel>(_mapper.ConfigurationProvider)
                .AsNoTracking()
                .First();

            response.Success = true;
            response.Message = $"Evento de '{responseObject.Evento_Tipo}' registrado com sucesso";
            response.Object = responseObject;
        } catch (Exception ex) {
            response.Message = $"Falha ao inserir evento de tipo '{( int )eventoTipoId}': {ex}";
        }

        return response;
    }

    public ResponseModel Update(UpdateEventoRequest request)
    {
        ResponseModel response = new() { Success = false };

        try {
            Evento? evento = _db.Eventos.FirstOrDefault(e => e.Id == request.Id);

            if (evento is null) {
                return new ResponseModel { Message = "Evento não encontrado" };
            }

            int eventoTipoId = evento.Evento_Tipo_Id;

            // Validação de quantidades de alunos/professores para cada tipo de evento
            switch (eventoTipoId) {
            case ( int )EventoTipo.Reuniao:
                if (request.Alunos.Count != 0) {
                    return new ResponseModel { Message = "Um evento de reunião não pode ter alunos associados" };
                }
                break;

            case ( int )EventoTipo.Oficina:
                break;

            case ( int )EventoTipo.Superacao:
                if (request.Alunos.Count != 1) {
                    return new ResponseModel { Message = "Um evento de Superação só pode ter um aluno associado" };
                }
                break;

            default:
                return new ResponseModel { Message = "Internal Server Error : 'Tipo de evento inválido'" };
            };

            IQueryable<Aluno> alunosInRequest = _db.Alunos.Where(a => a.Deactivated == null && request.Alunos.Contains(a.Id));

            if (alunosInRequest.Count() != request.Alunos.Count) {
                return new ResponseModel { Message = "Aluno(s) não encontrado(s)" };
            }

            IQueryable<Professor> professoresInRequest = _db.Professors.Include(p => p.Account).Where(p => p.Account.Deactivated == null && request.Professores.Contains(p.Id));

            if (professoresInRequest.Count() != request.Professores.Count) {
                return new ResponseModel { Message = "Professor(es) não encontrado(s)" };
            }

            if (request.Data < TimeFunctions.HoraAtualBR()) {
                return new ResponseModel { Message = "Data do evento não pode ser no passado" };
            }

            foreach (var professor in professoresInRequest) {
                bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
                    professorId: professor.Id,
                    DiaSemana: ( int )request.Data.DayOfWeek,
                    Horario: request.Data.TimeOfDay,
                    IgnoredTurmaId: null
                );

                if (hasTurmaConflict) {
                    return new ResponseModel { Message = $"Professor: '{professor.Account.Name}' possui uma turma nesse mesmo horário" };
                }

                Evento_Participacao_Professor? participacaoProfessor = _db.Evento_Participacao_Professors.FirstOrDefault(p => p.Evento_Id == evento.Id);

                bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
                    professorId: professor.Id,
                    Data: request.Data,
                    IgnoredParticipacaoId: participacaoProfessor?.Id
                );

                if (hasParticipacaoConflict) {
                    return new ResponseModel { Message = $"Professor: {professor.Account.Name} possui participação em outro evento nesse mesmo horário" };
                }
            }

            // Não devo poder registrar um evento em uma sala que não existe
            bool salaExists = _db.Salas.Any(s => s.Id == request.Sala_Id);

            if (!salaExists) {
                return new ResponseModel { Message = "Sala não encontrada" };
            }

            // Não devo poder registrar um evento em uma sala que está ocupada num intervalo de 2 horas antes ou depois
            var twoHoursBefore = request.Data.AddHours(-2);
            var twoHoursAfter = request.Data.AddHours(2);

            bool isSalaOccupied = _db.Eventos.Any(e =>
                e.Id != evento.Id
                && e.Deactivated == null
                && e.Sala_Id == request.Sala_Id
                && e.Data.Date == request.Data.Date
                && e.Data > twoHoursBefore
                && e.Data < twoHoursAfter);

            if (isSalaOccupied) {
                return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };
            }

            // Validations passed

            var oldObject = _db.Eventos.Where(e => e.Id == request.Id)
                .ProjectTo<EventoModel>(_mapper.ConfigurationProvider)
                .AsNoTracking()
                .First();

            evento.Observacao = request.Observacao ?? request.Observacao;
            evento.Descricao = request.Descricao ?? evento.Descricao;
            evento.Sala_Id = request.Sala_Id;
            evento.Data = request.Data;
            evento.DuracaoMinutos = request.DuracaoMinutos;
            evento.LastUpdated = TimeFunctions.HoraAtualBR();

            _db.Eventos.Update(evento);
            _db.SaveChanges();

            // Reinserir as participações dos envolvidos no evento por simplicidade

            var responseObject = _db.Eventos.Where(e => e.Id == evento.Id)
                .ProjectTo<EventoAulaModel>(_mapper.ConfigurationProvider)
                .AsNoTracking()
                .First();

            response.Message = $"Evento de '{evento.Evento_Tipo}' atualizado com sucesso";
            response.OldObject = oldObject;
            response.Object = responseObject;
            response.Success = true;
        } catch (Exception ex) {
            response.Message = $"Falha ao atualizar evento ID: '{request.Id}' | {ex}";
        }

        return response;
    }
}
