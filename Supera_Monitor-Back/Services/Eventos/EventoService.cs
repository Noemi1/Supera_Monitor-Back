using System.Globalization;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Eventos;
using Supera_Monitor_Back.Models.Eventos.Dtos;

namespace Supera_Monitor_Back.Services.Eventos;

public interface IEventoService {
    public CalendarioEventoList GetEventoById(int eventoId);
    public List<CalendarioEventoList> GetOficinas();
    public List<CalendarioEventoList> GetCalendario(CalendarioRequest request);
    public ResponseModel Insert(CreateEventoRequest request, int eventoTipoId);
    public ResponseModel Update(UpdateEventoRequest request);
    public ResponseModel Cancelar(CancelarEventoRequest request);
    public ResponseModel Finalizar(FinalizarEventoRequest request);
    public ResponseModel Reagendar(ReagendarEventoRequest request);

    public ResponseModel Create(NewEventoRequest request);

    public ResponseModel CreateEvent(CreateEventDto dto);
    public ResponseModel CreateAulaZeroEvent(CreateEventDto dto);
    public ResponseModel CreateAulaTurmaEvent(CreateClassEventDto dto);
    public ResponseModel CreateAulaExtraEvent(CreateClassEventDto dto);

    public ResponseModel InsertParticipacao(InsertParticipacaoRequest request);
    public ResponseModel RemoveParticipacao(int participacaoId);

    public Dashboard Dashboard(DashboardRequest request);
}

public class EventoService : IEventoService {
    private readonly DataContext _db;
    private readonly IMapper _mapper;
    private readonly IProfessorService _professorService;
    private readonly ISalaService _salaService;

    private readonly Account? _account;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public EventoService(DataContext db, IMapper mapper, IProfessorService professorService, ISalaService salaService, IHttpContextAccessor httpContextAccessor) {
        _db = db;
        _mapper = mapper;
        _professorService = professorService;
        _salaService = salaService;
        _httpContextAccessor = httpContextAccessor;
        _account = (Account?)_httpContextAccessor.HttpContext?.Items["Account"];
    }

    public ResponseModel Insert(CreateEventoRequest request, int eventoTipoId) {
        ResponseModel response = new() { Success = false };

        try {
            // Validação de quantidades de alunos/professores para cada tipo de evento

            string eventoTipo;

            switch (eventoTipoId) {
                case (int)EventoTipo.Reuniao:
                    if (request.Alunos.Count != 0) {
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

            if (alunosInRequest.Count() != request.Alunos.Count) {
                return new ResponseModel { Message = "Aluno(s) não encontrado(s)" };
            }

            IQueryable<Professor> professoresInRequest = _db.Professors.Include(p => p.Account).Where(p => p.Account.Deactivated == null && request.Professores.Contains(p.Id));

            if (professoresInRequest.Count() != request.Professores.Count) {
                return new ResponseModel { Message = "Professor(es) não encontrado(s)" };
            }

            //if (request.Data < TimeFunctions.HoraAtualBR()) {
            //    return new ResponseModel { Message = "Data do evento não pode ser no passado" };
            //}

            foreach (var professor in professoresInRequest) {
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
            }

            // Não devo poder registrar um evento em uma sala que não existe
            bool salaExists = _db.Salas.Any(s => s.Id == request.Sala_Id);

            if (!salaExists) {
                return new ResponseModel { Message = "Sala não encontrada" };
            }

            // Sala deve estar livre no horário do evento
            bool isSalaOccupied = _salaService.IsSalaOccupied(request.Sala_Id, request.Data, request.DuracaoMinutos, null);

            if (isSalaOccupied) {
                return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };
            }

            if (request.CapacidadeMaximaAlunos < 0) {
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

            foreach (var participacao in participacoesAlunos) {
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

            if (participacoesProfessores.Any()) {
                _db.Evento_Participacao_Professors.AddRange(participacoesProfessores);
            }

            foreach (var professor in professoresInRequest) {
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
            }

            _db.SaveChanges();

            var responseObject = _db.CalendarioEventoLists.First(e => e.Id == evento.Id);
            responseObject.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == evento.Id).ToList();
            responseObject.Professores = _db.CalendarioProfessorLists.Where(p => p.Evento_Id == evento.Id).ToList();

            response.Success = true;
            response.Message = $"Evento de '{responseObject.Evento_Tipo}' registrado com sucesso";
            response.Object = responseObject;
        }
        catch (Exception ex) {
            response.Message = $"Falha ao inserir evento de tipo '{(int)eventoTipoId}': {ex}";
        }

        return response;
    }

    public ResponseModel Update(UpdateEventoRequest request) {
        ResponseModel response = new() { Success = false };

        try {
            Evento? evento = _db.Eventos
                .Include(e => e.Evento_Tipo)
                .Include(e => e.Evento_Participacao_Alunos)
                .FirstOrDefault(e => e.Id == request.Id);

            if (evento is null) {
                return new ResponseModel { Message = "Evento não encontrado" };
            }

            IQueryable<Professor> professoresInRequest = _db.Professors.Include(p => p.Account).Where(p => p.Account.Deactivated == null && request.Professores.Contains(p.Id));

            if (professoresInRequest.Count() != request.Professores.Count) {
                return new ResponseModel { Message = "Professor(es) não encontrado(s)" };
            }

            //if (request.Data < TimeFunctions.HoraAtualBR()) {
            //    return new ResponseModel { Message = "Data do evento não pode ser no passado" };
            //}

            foreach (var professor in professoresInRequest) {
                bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
                    professorId: professor.Id,
                    DiaSemana: (int)evento.Data.DayOfWeek,
                    Horario: evento.Data.TimeOfDay,
                    IgnoredTurmaId: null
                );

                if (hasTurmaConflict) {
                    return new ResponseModel { Message = $"Professor: '{professor.Account.Name}' possui uma turma nesse mesmo horário" };
                }

                Evento_Participacao_Professor? participacaoProfessor = _db.Evento_Participacao_Professors.FirstOrDefault(p => p.Evento_Id == evento.Id);

                bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
                    professorId: professor.Id,
                    Data: evento.Data,
                    DuracaoMinutos: request.DuracaoMinutos,
                    IgnoredEventoId: evento.Id
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

            // Sala deve estar livre no horário do evento
            bool isSalaOccupied = _salaService.IsSalaOccupied(request.Sala_Id, evento.Data, request.DuracaoMinutos, evento.Id);

            if (isSalaOccupied) {
                return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };
            }

            int alunosInEvento = evento.Evento_Participacao_Alunos.Count(e => e.Deactivated == null);

            if (request.CapacidadeMaximaAlunos < alunosInEvento) {
                return new ResponseModel { Message = "Número máximo de alunos excedido" };
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

            foreach (var participacao in participacoesToDeactivate) {
                participacao.Deactivated = TimeFunctions.HoraAtualBR();
                _db.Evento_Participacao_Professors.Update(participacao);
            }

            foreach (int professorId in participacoesToAdd) {
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
        catch (Exception ex) {
            response.Message = $"Falha ao atualizar evento ID: '{request.Id}' | {ex}";
        }

        return response;
    }


    public List<CalendarioEventoList> GetCalendario(CalendarioRequest request) {
        DateTime now = TimeFunctions.HoraAtualBR();

        request.IntervaloDe ??= GetThisWeeksMonday(now); // Se não passar data inicio, considera a segunda-feira da semana atual
        request.IntervaloAte ??= GetThisWeeksSaturday((DateTime)request.IntervaloDe); // Se não passar data fim, considera o sábado da semana da data inicio

        if (request.IntervaloAte < request.IntervaloDe) {
            throw new Exception("Final do intervalo não pode ser antes do seu próprio início");
        }

        IQueryable<CalendarioEventoList> eventosQueryable = _db.CalendarioEventoLists
            .Where(e => e.Data.Date >= request.IntervaloDe.Value.Date && e.Data.Date <= request.IntervaloAte.Value.Date);

        IQueryable<Turma> turmasQueryable = _db.Turmas
            .Where(t => t.Deactivated == null)
            .Include(t => t.Professor!)
            .ThenInclude(t => t.Account)
            .Include(t => t.Sala);

        IQueryable<Professor> professoresQueryable = _db.Professors
            .Include(p => p.Account)
            .Where(p => p.Account.Deactivated == null);

        if (request.Perfil_Cognitivo_Id.HasValue) {
            // Eventos que contem o perfil cognitivo 
            var eventosContemPerfilCognitivo = _db.Evento_Aula_PerfilCognitivo_Rels.Where(x => x.PerfilCognitivo_Id == request.Perfil_Cognitivo_Id);
            var turmasContemPerfilCognitivo = _db.Turma_PerfilCognitivo_Rels.Where(x => x.PerfilCognitivo_Id == request.Perfil_Cognitivo_Id);

            eventosQueryable = eventosQueryable.Where(e => eventosContemPerfilCognitivo.Any(x => x.Evento_Aula_Id == e.Id));
            turmasQueryable = turmasQueryable.Where(t => turmasContemPerfilCognitivo.Any(x => x.Turma_Id == t.Id));
        }

        if (request.Turma_Id.HasValue) {
            eventosQueryable = eventosQueryable.Where(e => e.Turma_Id != null && e.Turma_Id == request.Turma_Id);
            turmasQueryable = turmasQueryable.Where(t => t.Id == request.Turma_Id);
        }

        if (request.Professor_Id.HasValue) {
            // Busca o professor em evento.Professor_Id e evento.Evento_Participacao_Professor
            var eventosContemProfessor = _db.Evento_Participacao_Professors.Where(x => x.Professor_Id == request.Professor_Id.Value);
            eventosQueryable = eventosQueryable.Where(e => e.Professor_Id != null && (e.Professor_Id == request.Professor_Id || eventosContemProfessor.Any(x => x.Evento_Id == e.Id)));
            turmasQueryable = turmasQueryable.Where(t => t.Professor_Id == request.Professor_Id);
            professoresQueryable = professoresQueryable.Where(x => x.Id == request.Professor_Id.Value);
        }

        if (request.Aluno_Id.HasValue) {
            var aluno = _db.Alunos.FirstOrDefault(a => a.Id == request.Aluno_Id);

            if (aluno is not null) {
                turmasQueryable = turmasQueryable.Where(t => t.Id == aluno.Turma_Id);
                // Busca o aluno em evento.Evento_Participacao_Aluno fora do where de eventos, para fazer menos filtros
                var eventosContemAlunos = _db.Evento_Participacao_Alunos.Where(x => x.Aluno_Id == request.Aluno_Id.Value);
                eventosQueryable = eventosQueryable.Where(e => eventosContemAlunos.Any(p => p.Evento_Id == e.Id));
            }
        }

        // Pré request de professores, turmas e perfis cognitivos das turmas
        List<Turma> turmas = turmasQueryable.ToList();

        List<Professor> professores = professoresQueryable.ToList();

        List<int> turmaIds = turmas.Select(t => t.Id).ToList();

        List<AlunoList> alunosFromTurmas = _db.AlunoLists
            .Where(a => turmaIds.Contains(a.Turma_Id))
            .ToList();

        List<Turma_PerfilCognitivo_Rel> perfilCognitivoRelFromTurmas = _db.Turma_PerfilCognitivo_Rels
            .Include(p => p.PerfilCognitivo)
            .Where(p => turmaIds.Contains(p.Turma_Id))
            .ToList();

        // Adicionar aulas instanciadas ao retorno
        List<CalendarioEventoList> calendarioResponse = eventosQueryable.ToList();

        //Adicionar os alunos e perfis cognitivos às aulas instanciadas
        //foreach (CalendarioEventoList evento in calendarioResponse) {
        //    evento.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == evento.Id).OrderBy(a => a.Aluno).ToList();
        //    evento.Professores = _db.CalendarioProfessorLists.Where(e => e.Evento_Id == evento.Id).ToList();

        //    if (evento.Evento_Tipo_Id == (int)EventoTipo.Aula || evento.Evento_Tipo_Id == (int)EventoTipo.AulaExtra) {
        //        var perfisCognitivos = _db.Evento_Aula_PerfilCognitivo_Rels
        //            .Where(p => p.Evento_Aula_Id == evento.Id)
        //            .Include(p => p.PerfilCognitivo)
        //            .Select(p => p.PerfilCognitivo)
        //            .ToList();

        //        evento.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfisCognitivos);
        //    }
        //}
        PopulateCalendarioEvents(calendarioResponse);

        // Carrega lista de roteiros no intervalo selecionado
        List<Roteiro> roteiros = _db.Roteiros.Where(x => x.DataInicio.Date <= request.IntervaloDe.Value.Date && x.DataFim.Date <= request.IntervaloAte.Value.Date).ToList();

        // Adicionar aulas não instanciadas ao retorno
        DateTime data = request.IntervaloDe.Value;

        // Adicionar todas as aulas não instanciadas - Aulas de turmas que tem horário marcado
        while (data < request.IntervaloAte) {
            //
            // Adiciona eventos recorrentes para reuniões e oficinas
            //
            var diaSemana = data.DayOfWeek;

            //
            // Adiciona Oficina - Se a já existe uma oficina agendada para segunda-feira, não vai adicionar
            //
            if (diaSemana == DayOfWeek.Monday) {
                CalendarioEventoList? eventoOficina = calendarioResponse
                    .FirstOrDefault(a =>
                        a.Data.Date == data.Date
                        && a.Evento_Tipo_Id == (int)EventoTipo.Oficina);

                // Não usar mais o continue porque o método adiciona outros pseudo eventos
                if (eventoOficina is null) {
                    var roteiro = roteiros.FirstOrDefault(x => data.Date <= x.DataInicio.Date && data >= x.DataFim);

                    CalendarioEventoList pseudoOficina = new()
                    {
                        Id = -1,
                        Evento_Tipo_Id = (int)EventoTipo.Oficina,
                        Evento_Tipo = "Pseudo-Oficina",
                        CapacidadeMaximaAlunos = 12,

                        Descricao = "Oficina - Tema indefinido",
                        DuracaoMinutos = 60,

                        Roteiro_Id = roteiro?.Id,
                        Semana = roteiro?.Semana,
                        Tema = roteiro?.Tema,

                        Data = new DateTime(data.Year, data.Month, data.Day, 10, 0, 0),
                        Finalizado = false,
                        Sala_Id = null,
                    };

                    // Se está aplicado um filtro de professor ou turma, não deve mostrar pseudo-oficinas, já que esses dados ainda não estão definidos
                    if (request.Professor_Id is null && request.Turma_Id is null && request.Aluno_Id is null) {
                        calendarioResponse.Add(pseudoOficina);
                    }
                }
            }

            //
            // Adiciona Reunião - Se a já existe uma reunião agendada para a data, não vai adicionar
            //
            if (diaSemana == DayOfWeek.Monday || diaSemana == DayOfWeek.Tuesday || diaSemana == DayOfWeek.Friday) {
                CalendarioEventoList? eventoReuniao = calendarioResponse.FirstOrDefault(a =>
                    a.Data.Date == data.Date
                    && a.Evento_Tipo_Id == (int)EventoTipo.Reuniao);

                // Não usar mais o continue porque o método adiciona outros pseudo eventos
                if (eventoReuniao is null) {
                    var descricao = diaSemana == DayOfWeek.Monday ? "Reunião Geral" : // Segunda-feira
                                    diaSemana == DayOfWeek.Tuesday ? "Reunião Monitoramento" : // Terça-feira
                                    "Reunião Pedagógica"; // Sexta-feira

                    CalendarioEventoList pseudoReuniao = new()
                    {
                        Id = -1,
                        Evento_Tipo_Id = (int)EventoTipo.Reuniao,
                        Evento_Tipo = "Pseudo-Reuniao",
                        Data = new DateTime(data.Year, data.Month, data.Day, 12, 0, 0),
                        Descricao = descricao,
                        DuracaoMinutos = 60,
                        Finalizado = false,
                        Sala_Id = null,
                        CapacidadeMaximaAlunos = 0,
                        Professores = professores.Select(professor => new CalendarioProfessorList
                        {
                            Evento_Id = -1,
                            Professor_Id = professor.Id,
                            Nome = professor.Account.Name,
                            CorLegenda = professor.CorLegenda,
                            Account_Id = professor.Account.Id,
                            Telefone = professor.Account.Phone,
                            ExpedienteFim = professor.ExpedienteFim,
                            ExpedienteInicio = professor.ExpedienteInicio,
                        }).ToList()
                    };

                    if (request.Turma_Id is null && request.Aluno_Id is null) {
                        calendarioResponse.Add(pseudoReuniao);
                    }
                }
            }

            // Adicionar aulas da turma do dia que ainda não foram instanciadas
            List<Turma> turmasDoDia = turmas.Where(t => t.DiaSemana == (int)data.DayOfWeek).ToList();

            foreach (Turma turma in turmasDoDia) {
                var originalTurmaDatetime = new DateTime(data.Year, data.Month, data.Day, turma.Horario!.Value.Hours, turma.Horario!.Value.Minutes, turma.Horario!.Value.Seconds);

                // Se a turma já tem uma aula instanciada no mesmo horário, é uma aula repetida, então ignora e passa pra proxima
                CalendarioEventoList? eventoAula = calendarioResponse.FirstOrDefault(a =>
                    a.Data.Date == data.Date
                    && a.Turma_Id == turma.Id
                    && a.Data == originalTurmaDatetime);

                // Não usar mais o continue porque o método adiciona outros pseudo eventos
                if (eventoAula is null) {
                    var roteiro = roteiros.FirstOrDefault(x => data.Date <= x.DataInicio.Date && data >= x.DataFim);

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

                        Data = originalTurmaDatetime,

                        Turma_Id = turma.Id,
                        Turma = turma.Nome,
                        CapacidadeMaximaAlunos = turma.CapacidadeMaximaAlunos,

                        Professor_Id = turma?.Professor_Id,
                        Professor = turma?.Professor is not null ? turma.Professor.Account.Name : "Professor indefinido",
                        CorLegenda = turma?.Professor is not null ? turma.Professor.CorLegenda : "#000",

                        Finalizado = false,
                        Sala_Id = turma?.Sala?.Id,
                        NumeroSala = turma?.Sala?.NumeroSala,
                        Andar = turma?.Sala?.Andar,
                    };

                    // Em pseudo-aulas, adicionar só os alunos da turma original após o início de sua vigência
                    List<AlunoList> alunos = alunosFromTurmas
                        .Where(
                            a => a.Turma_Id == turma.Id
                            && a.DataInicioVigencia.Value.Date <= data.Date)
                        .OrderBy(a => a.Nome)
                        .ToList();

                    pseudoAula.Alunos = _mapper.Map<List<CalendarioAlunoList>>(alunos).OrderBy(a => a.Aluno).ToList();

                    pseudoAula.Professores.Add(new CalendarioProfessorList
                    {
                        Id = null,
                        Evento_Id = pseudoAula.Id,
                        Professor_Id = (int)turma.Professor_Id,
                        Nome = turma.Professor.Account.Name,
                        CorLegenda = turma.Professor.CorLegenda,
                        Presente = null,
                        Observacao = "",
                        Account_Id = turma.Professor.Account.Id,
                        Telefone = turma.Professor.Account.Phone,
                        ExpedienteFim = turma.Professor.ExpedienteFim,
                        ExpedienteInicio = turma.Professor.ExpedienteInicio,
                    });

                    List<PerfilCognitivo> perfisCognitivos = perfilCognitivoRelFromTurmas
                        .Where(p => p.Turma_Id == pseudoAula.Turma_Id)
                        .Select(p => p.PerfilCognitivo)
                        .ToList();

                    pseudoAula.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfisCognitivos);

                    calendarioResponse.Add(pseudoAula);
                }
            }

            data = data.AddDays(1);
        }

        calendarioResponse = calendarioResponse.OrderBy(e => e.Data).ToList();

        return calendarioResponse;
    }

    private static DateTime GetThisWeeksMonday(DateTime date) {
        var response = date.AddDays(-(int)date.DayOfWeek);
        return response.AddDays(1);
    }

    private static DateTime GetThisWeeksSaturday(DateTime date) {
        var response = date.AddDays(-(int)date.DayOfWeek);
        return response.AddDays(6);
    }

    public ResponseModel InsertParticipacao(InsertParticipacaoRequest request) {
        ResponseModel response = new() { Success = false };

        try {
            Evento? evento = _db.Eventos.Include(e => e.Evento_Aula).FirstOrDefault(e => e.Id == request.Evento_Id);

            if (evento is null) {
                return new ResponseModel { Message = "Evento não encontrado" };
            }

            if (evento.Deactivated.HasValue) {
                return new ResponseModel { Message = "Evento está desativado" };
            }

            if (evento.Finalizado) {
                return new ResponseModel { Message = "Evento já foi finalizado" };
            }

            Aluno? aluno = _db.Alunos.Find(request.Aluno_Id);

            if (aluno is null) {
                return new ResponseModel { Message = "Aluno não encontrado" };
            }

            // Se aluno já está inscrito, não deve poder ser inscrito novamente
            bool alunoIsAlreadyEnrolled = _db.Evento_Participacao_Alunos.Any(a => a.Evento_Id == evento.Id && a.Aluno_Id == aluno.Id);

            if (alunoIsAlreadyEnrolled) {
                return new ResponseModel { Message = "Aluno já está inscrito neste evento" };
            }

            int amountOfAlunosEnrolled = _db.Evento_Participacao_Alunos.Count(a => a.Evento_Id == evento.Id);

            switch (evento.Evento_Tipo_Id) {
                case (int)EventoTipo.Aula:
                    if (amountOfAlunosEnrolled >= evento.CapacidadeMaximaAlunos) {
                        return new ResponseModel { Message = "Este evento de aula se encontra lotado." };
                    }
                    break;

                case (int)EventoTipo.AulaExtra:
                    if (amountOfAlunosEnrolled >= evento.CapacidadeMaximaAlunos) {
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

                    if (alunoAlreadyParticipated) {
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

            _db.Evento_Participacao_Alunos.Add(newParticipacao);
            _db.SaveChanges();

            response.Message = $"Aluno foi inscrito no evento com sucesso";
            response.Object = newParticipacao;
            response.Success = true;
        }
        catch (Exception ex) {
            response.Message = $"Falha ao inscrever aluno no evento: {ex}";
        }

        return response;
    }

    public ResponseModel RemoveParticipacao(int participacaoId) {
        ResponseModel response = new() { Success = false };

        try {
            Evento_Participacao_Aluno? participacao = _db.Evento_Participacao_Alunos.FirstOrDefault(p => p.Id == participacaoId);

            if (participacao is null) {
                return new ResponseModel { Message = "Participação do aluno em evento não encontrada" };
            }

            if (participacao.Deactivated.HasValue) {
                return new ResponseModel { Message = "Não é possível remove uma participação que está desativada" };
            }

            // Validations passed

            _db.Evento_Participacao_Alunos.Remove(participacao);
            _db.SaveChanges();

            response.Message = "Participação do aluno removida com sucesso";
            response.Success = true;
        }
        catch (Exception ex) {
            response.Message = $"Falha ao remover participação aluno em evento: {ex}";
        }

        return response;
    }


    public ResponseModel Cancelar(CancelarEventoRequest request) {
        ResponseModel response = new() { Success = false };

        try {
            Evento? evento = _db.Eventos.FirstOrDefault(e => e.Id == request.Id);

            if (evento is null) {
                return new ResponseModel { Message = "Evento não encontrado." };
            }

            if (evento.Deactivated.HasValue) {
                return new ResponseModel { Message = "Evento já foi cancelado" };
            }

            // Validations passed

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
        catch (Exception ex) {
            response.Message = $"Falha ao cancelar evento: {ex}";
        }

        return response;
    }

    public ResponseModel Reagendar(ReagendarEventoRequest request) {
        ResponseModel response = new() { Success = false };

        try {
            Evento? evento = _db.Eventos
                .Include(e => e.Evento_Participacao_Alunos)
                .Include(e => e.Evento_Participacao_Professors)
                .Include(e => e.Evento_Aula)
                .FirstOrDefault(e => e.Id == request.Evento_Id);

            if (evento is null) {
                return new ResponseModel { Message = "Evento não encontrado." };
            }

            if (evento.Deactivated.HasValue) {
                return new ResponseModel { Message = "Evento está desativado." };
            }

            if (request.Data <= TimeFunctions.HoraAtualBR()) {
                return new ResponseModel { Message = "Não é possível reagendar um evento para uma data no passado." };
            }

            // Sala deve estar livre no horário do evento
            bool isSalaOccupied = _salaService.IsSalaOccupied(request.Sala_Id, request.Data, evento.DuracaoMinutos, evento.Id);

            if (isSalaOccupied) {
                return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };
            }

            // Todos os professores devem estar livres no horário do evento

            List<Evento_Participacao_Professor> professorParticipacoes = evento.Evento_Participacao_Professors.ToList();

            foreach (var participacao in professorParticipacoes) {
                bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
                    professorId: participacao.Professor_Id,
                    DiaSemana: (int)request.Data.DayOfWeek,
                    Horario: request.Data.TimeOfDay,
                    IgnoredTurmaId: null
                );

                if (hasTurmaConflict) {
                    return new ResponseModel { Message = $"Professor ID: '{participacao.Professor_Id}' possui uma turma nesse mesmo horário" };
                }

                bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
                    professorId: participacao.Professor_Id,
                    Data: request.Data,
                    DuracaoMinutos: evento.DuracaoMinutos,
                    IgnoredEventoId: evento.Id
                );

                if (hasParticipacaoConflict) {
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

            if (evento.Evento_Aula is not null) {
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
            foreach (var participacao in evento.Evento_Participacao_Alunos) {
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
            foreach (var participacao in evento.Evento_Participacao_Professors) {
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
        catch (Exception ex) {
            response.Message = $"Falha ao reagendar evento: {ex}";
        }

        return response;
    }

    public ResponseModel Finalizar(FinalizarEventoRequest request) {
        ResponseModel response = new() { Success = false };

        try {
            Evento? evento = _db.Eventos
                .Include(e => e.Evento_Participacao_Alunos)
                .ThenInclude(e => e.Aluno)
                .Include(e => e.Evento_Participacao_Professors)
                .FirstOrDefault(e => e.Id == request.Evento_Id);

            if (evento is null) {
                return new ResponseModel { Message = "Evento não encontrado." };
            }

            if (evento.Deactivated.HasValue) {
                return new ResponseModel { Message = $"Este evento foi cancelado às {evento.Deactivated.Value:g}" };
            }

            // Não devo poder realizar a chamada em um evento que está finalizado
            if (evento.Finalizado) {
                return new ResponseModel { Message = "Evento já está finalizado" };
            }

            // Valida que as apostilas existem
            List<Apostila> existingApostilas = _db.Apostilas.ToList();

            List<int> apostilasAbacoIds = request.Alunos.Select(p => p.Apostila_Abaco_Id).ToList();
            List<int> apostilasAhIds = request.Alunos.Select(p => p.Apostila_Ah_Id).ToList();

            // Verifica se as apostilas passadas na requisição existem
            foreach (int apostilaId in apostilasAbacoIds) {
                if (!existingApostilas.Any(a => a.Id == apostilaId)) {
                    return new ResponseModel { Message = $"Apostila Abaco ID: '{apostilaId}' não existe" };
                }
            }

            foreach (int apostilaId in apostilasAhIds) {
                if (!existingApostilas.Any(a => a.Id == apostilaId)) {
                    return new ResponseModel { Message = $"Apostila AH ID: '{apostilaId}' não existe" };
                }
            }

            // Validations passed

            evento.Observacao = request.Observacao;
            evento.Finalizado = true;
            evento.LastUpdated = TimeFunctions.HoraAtualBR();

            _db.Eventos.Update(evento);

            foreach (ParticipacaoAlunoModel partAluno in request.Alunos) {
                Evento_Participacao_Aluno? participacao = evento.Evento_Participacao_Alunos
                    .FirstOrDefault(p => p.Id == partAluno.Participacao_Id);

                if (participacao is null) {
                    return new ResponseModel { Message = $"Participação de aluno no evento ID: '{evento.Id}' Participacao_Id: '{partAluno.Participacao_Id}' não foi encontrada" };
                }

                if (participacao.Deactivated.HasValue) {
                    return new ResponseModel { Message = $"Participação de aluno no evento ID: '{evento.Id}' Participacao_Id: '{partAluno.Participacao_Id}' está desativada" };
                }

                // Alunos devem possuir as apostilas em que estão tentando marcar progresso

                var alunoApostilaKitId = participacao.Aluno.Apostila_Kit_Id;

                if (alunoApostilaKitId == null) {
                    return new ResponseModel { Message = $"Aluno ID: '{participacao.Aluno_Id}' não possui kit de apostilas" };
                }

                bool alunoHasApostilaAbaco = _db.Apostila_Kit_Rels.Any(a =>
                   a.Apostila_Kit_Id == alunoApostilaKitId
                   && a.Apostila_Id == partAluno.Apostila_Abaco_Id);

                bool alunoHasApostilaAh = _db.Apostila_Kit_Rels.Any(a =>
                    a.Apostila_Kit_Id == alunoApostilaKitId
                    && a.Apostila_Id == partAluno.Apostila_Ah_Id);

                // Para poder atualizar, o kit de apostilas do aluno deve possuir a apostila Abaco e a apostila AH passadas na requisição

                if (!alunoHasApostilaAbaco) {
                    return new ResponseModel { Message = $"Aluno ID: '{participacao.Aluno_Id}' não possui a apostila Abaco ID: '{partAluno.Apostila_Abaco_Id}'" };
                }

                if (!alunoHasApostilaAh) {
                    return new ResponseModel { Message = $"Aluno ID: '{participacao.Aluno_Id}' não possui a apostila AH ID: '{partAluno.Apostila_Ah_Id}'" };
                }

                // Desativado: Se a apostila não muda, não deve permitir que o aluno regresse nas páginas
                //bool apostilaAbacoChanged = participacao.Apostila_Abaco_Id != partAluno.Apostila_Abaco_Id;
                //if (!apostilaAbacoChanged) {
                //    if (partAluno.NumeroPaginaAbaco < participacao.NumeroPaginaAbaco) {
                //        return new ResponseModel { Message = $"Progresso da Apostila Abaco não pode regredir: Participacao ID {participacao.Id}" };
                //    }
                //}

                //bool apostilaAhChanged = participacao.Apostila_AH_Id != partAluno.Apostila_Ah_Id;
                //if (!apostilaAhChanged) {
                //    if (partAluno.NumeroPaginaAh < participacao.NumeroPaginaAH) {
                //        return new ResponseModel { Message = $"Progresso da Apostila AH não pode regredir: Participacao ID {participacao.Id}" };
                //    }
                //}

                // Não deve ser possível atualizar além do tamanho máximo da apostila
                int totalPaginasAbaco = existingApostilas.Find(a => a.Id == partAluno.Apostila_Abaco_Id)!.NumeroTotalPaginas;
                int totalPaginasAh = existingApostilas.Find(a => a.Id == partAluno.Apostila_Ah_Id)!.NumeroTotalPaginas;

                if (partAluno.NumeroPaginaAbaco > totalPaginasAbaco) {
                    return new ResponseModel { Message = $"Número de páginas da apostila Abaco não pode ser maior que o total de páginas: Participacao ID {participacao.Id}" };
                }

                if (partAluno.NumeroPaginaAh > totalPaginasAh) {
                    return new ResponseModel { Message = $"Número de páginas da apostila AH não pode ser maior que o total de páginas: Participacao ID {participacao.Id}" };
                }

                // Atualizar tanto a participação quanto o aluno
                participacao.Apostila_Abaco_Id = partAluno.Apostila_Abaco_Id;
                participacao.NumeroPaginaAbaco = partAluno.NumeroPaginaAbaco;
                participacao.Aluno.Apostila_Abaco_Id = partAluno.Apostila_Abaco_Id;
                participacao.Aluno.NumeroPaginaAbaco = partAluno.NumeroPaginaAbaco;

                participacao.Apostila_AH_Id = partAluno.Apostila_Ah_Id;
                participacao.NumeroPaginaAH = partAluno.NumeroPaginaAh;
                participacao.Aluno.Apostila_AH_Id = partAluno.Apostila_Ah_Id;
                participacao.Aluno.NumeroPaginaAH = partAluno.NumeroPaginaAh;

                // Se o evento for a primeira aula do aluno e ocorrer uma falta, deve atualizar a data da PrimeiraAula do aluno para a proxima aula da turma a partir da data do evento atual
                //if (participacao.Aluno.PrimeiraAula == evento.Data && partAluno.Presente == false) {
                //    Turma turma = _db.Turmas.Single(t => t.Id == participacao.Aluno.Turma_Id);

                //    DateTime data = evento.Data;

                //    do {
                //        data = data.AddDays(1);
                //    } while ((int)data.DayOfWeek != turma.DiaSemana);

                //    participacao.Aluno.PrimeiraAula = data;
                //}

                participacao.Observacao = partAluno.Observacao;
                participacao.Presente = partAluno.Presente;

                _db.Update(participacao);
            }

            foreach (ParticipacaoProfessorModel partProfessor in request.Professores) {
                Evento_Participacao_Professor? participacao = evento.Evento_Participacao_Professors.FirstOrDefault(p => p.Id == partProfessor.Participacao_Id);

                if (participacao is null) {
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
        catch (Exception ex) {
            response.Message = $"Falha ao finalizar evento: {ex}";
        }

        return response;
    }

    public List<CalendarioEventoList> GetOficinas() {
        var oficinas = _db.CalendarioEventoLists
            .Where(e =>
                e.Evento_Tipo_Id == (int)EventoTipo.Oficina
                && e.Data > TimeFunctions.HoraAtualBR())
            .OrderBy(e => e.Data)
            .ToList();

        foreach (var evento in oficinas) {
            evento.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == evento.Id).ToList();

            evento.Professores = _db.CalendarioProfessorLists.Where(e => e.Evento_Id == evento.Id).ToList();
        }

        return oficinas;
    }

    public CalendarioEventoList GetEventoById(int eventoId) {
        CalendarioEventoList evento = _db.CalendarioEventoLists.FirstOrDefault(e => e.Id == eventoId) ?? throw new Exception("Evento não encontrado");

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

    public Dashboard Dashboard(DashboardRequest request) {

        DateTime intervaloDe = new DateTime(request.Ano, 1, 1);
        DateTime intervaloAte = intervaloDe.AddYears(1).AddDays(-1);


        IQueryable<CalendarioEventoList> eventosQueryable = _db.CalendarioEventoLists
                                            .Where(x => x.Data.Date >= intervaloDe.Date
                                                        && x.Data.Date <= intervaloAte.Date
                                                        && (x.Evento_Tipo_Id == (int)EventoTipo.Aula
                                                        || x.Evento_Tipo_Id == (int)EventoTipo.AulaExtra))
                                            .AsQueryable();

        IQueryable<CalendarioAlunoList> participacoesQueryable = _db.CalendarioAlunoLists
            .AsQueryable();

        IQueryable<TurmaList> turmasQueryable = _db.TurmaLists
            .Where(t => t.Deactivated == null);

        IQueryable<AlunoList> alunosQueryable = _db.AlunoLists
            .Where(t => t.Deactivated == null);


        if (request.Turma_Id.HasValue) {
            alunosQueryable = alunosQueryable.Where(x => x.Turma_Id == request.Turma_Id.Value);
        }


        if (request.Aluno_Id.HasValue) {
            alunosQueryable = alunosQueryable.Where(x => x.Id == request.Aluno_Id.Value);
            participacoesQueryable = participacoesQueryable.Where(x => x.Aluno_Id == request.Aluno_Id.Value);
        }


        if (request.Professor_Id.HasValue) {
            alunosQueryable = alunosQueryable.Where(x => x.Professor_Id == request.Professor_Id.Value);
        }

        List<AlunoList> alunos = alunosQueryable.OrderBy(x => x.Nome).ToList();
        List<TurmaList> turmas = turmasQueryable.OrderBy(x => x.Nome).ToList();
        List<CalendarioAlunoList> participacoes = participacoesQueryable.ToList();
        List<CalendarioEventoList> eventos = eventosQueryable.OrderBy(x => x.Data).ToList();

        List<Dashboard_Aula_Participacao> aulas = new List<Dashboard_Aula_Participacao>() { };

        List<Roteiro> roteiros = _db.Roteiros
            .Where(x => x.DataInicio.Year == request.Ano)
            .OrderBy(x => x.DataInicio)
            .ToList();


        DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
        int weeksInYear = dfi.Calendar.GetWeekOfYear(intervaloAte, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
        Roteiro[] roteirosArray = new Roteiro[weeksInYear];

        roteiros.ForEach(roteiro =>
        {
            var week = dfi.Calendar.GetWeekOfYear(roteiro.DataInicio, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
            roteirosArray[week - 1] = roteiro;
        });

        for (int index = 0; index < roteirosArray.Length; index++) {
            Roteiro roteiro = roteirosArray[index];

            // Se não tiver roteiro salva pseudo roteiros
            if (roteiro == null) {
                int semana;
                DateTime dataInicio;
                DateTime dataFim;

                if (index == 0) // Se for a semana 0
                {
                    semana = 0;
                    dataInicio = intervaloDe; // .AddDays((int)intervaloDe.DayOfWeek - 5); // Segunda-feira
                }
                else {
                    Roteiro lastRoteiro = roteirosArray[index - 1];
                    DateTime domingo = lastRoteiro.DataInicio.AddDays(7 - (int)lastRoteiro.DataInicio.DayOfWeek);
                    dataInicio = domingo.AddDays(1); // Segunda-feira
                    semana = lastRoteiro.Semana + 1;
                }

                dataFim = dataInicio.AddDays(6 - (int)dataInicio.DayOfWeek);

                roteiro = new Roteiro
                {
                    Id = -1,
                    DataInicio = dataInicio,
                    DataFim = dataFim,
                    Semana = semana,
                    Tema = "Tema Indefinido"
                };
                roteirosArray[index] = roteiro;

            }

            // Insere as aulas das turmas para cada roteiro
            foreach (TurmaList turma in turmas) {
                // Encontra a Data da aula da turma naquela semana do roteiro
                DayOfWeek roteiroWeek = roteiro.DataInicio.DayOfWeek;

                int diff = 7 - (int)roteiroWeek + turma.DiaSemana;
                DateTime data = roteiro.DataInicio.AddDays(diff);

                // Se a data calculada estiver fora do roteiro
                // Procura a data mais próxima a partir do domingo
                if (data.Date < roteiro.DataInicio.Date || data.Date > roteiro.DataFim.Date) {
                    DateTime domingo = roteiro.DataInicio.AddDays(-(int)roteiroWeek);
                    data = domingo.AddDays(turma.DiaSemana);
                }


                List<CalendarioEventoList> aulasDoRoteiro = new List<CalendarioEventoList>();
                if (roteiro.Id == -1)
                    aulasDoRoteiro = eventos.Where(x => x.Data.Date >= roteiro.DataInicio && x.Data.Date <= roteiro.DataFim).ToList();
                else
                    aulasDoRoteiro = eventos.Where(x => x.Turma_Id == turma.Id && x.Roteiro_Id == roteiro.Id).ToList();

                // Se houver aula, só insere na listagem
                if (aulasDoRoteiro.Count > 0) {
                    foreach (CalendarioEventoList aula in aulasDoRoteiro) {
                        List<CalendarioAlunoList> participacoesAula = participacoes.Where(x => x.Evento_Id == aula.Id).ToList();

                        foreach (CalendarioAlunoList participacao in participacoesAula) {
                            aulas.Add(new Dashboard_Aula_Participacao
                            {
                                Show = true,
                                Aula = _mapper.Map<Dashboard_Aula>(aula),
                                Participacao = _mapper.Map<Dashboard_Participacao>(participacao),
                            });
                        }
                    }
                }
                // Se não houver aula, cria pseudo aulas e pseudo participações e insere na listagem
                else {

                    Dashboard_Aula pseudoAula = new()
                    {
                        Id = -1,
                        Data = new DateTime(data.Year, data.Month, data.Day, turma!.Horario!.Value.Hours, turma.Horario.Value.Minutes, 0),
                        Descricao = turma.Nome,
                        Evento_Tipo_Id = (int)EventoTipo.Aula,

                        DuracaoMinutos = 120,
                        Finalizado = false,
                        Roteiro_Id = roteiro.Id,
                        ReagendamentoDe_Evento_Id = null,
                        Deactivated = null,
                        Observacao = null,

                        Professor_Id = turma?.Professor_Id,
                        Professor = turma?.Professor ?? "Professor indefinido",
                        CorLegenda = turma?.Professor ?? "#000",

                        Sala_Id = turma?.Sala_Id,
                        NumeroSala = turma?.NumeroSala,
                        Andar = turma?.Andar,

                        Turma_Id = turma?.Id,
                        Turma = turma?.Nome,
                        CapacidadeMaximaAlunos = turma?.CapacidadeMaximaAlunos ?? 12,
                    };

                    List<AlunoList> alunosTurma = alunos.Where(x => x.Turma_Id == turma.Id).ToList();

                    foreach (AlunoList aluno in alunosTurma) {
                        Dashboard_Participacao pseudoParticipacao = new()
                        {
                            Id = -1,
                            Evento_Id = -1,
                            Aluno_Id = aluno.Id,

                            Apostila_Abaco = aluno.Apostila_Abaco,
                            Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
                            NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,

                            Apostila_AH = aluno.Apostila_AH,
                            Apostila_AH_Id = aluno.Apostila_AH_Id,
                            NumeroPaginaAH = aluno.NumeroPaginaAH,
                        };

                        var date = data.Date;
                        // Se estiver no invervalo do Roteiro
                        bool intervaloRoteiro = data >= roteiro.DataInicio.Date && data <= roteiro.DataFim.Date;
                        // Se o aluno estiver vigente naquela data
                        bool alunoVigente = (aluno.DataInicioVigencia.HasValue && date >= aluno.DataInicioVigencia.Value.Date) && (!aluno.DataFimVigencia.HasValue || date <= aluno.DataFimVigencia.Value.Date);

                        Dashboard_Aula_Participacao aula = new()
                        {
                            Participacao = pseudoParticipacao,
                            Aula = pseudoAula,
                            Show = alunoVigente && intervaloRoteiro
                        };

                        aulas.Add(aula);

                    }
                }
            }
        }


        Dashboard response = new Dashboard();
        response.Roteiros = _mapper.Map<List<Dashboard_Roteiro>>(roteirosArray);
        response.Alunos = alunos.Select(aluno =>
        {
            Dashboard_Aluno dashAluno = _mapper.Map<Dashboard_Aluno>(aluno);
            dashAluno.Aulas = aulas.Where(x => x.Participacao.Aluno_Id == aluno.Id)
                                        .OrderBy(x => x.Aula.Data)
                                        .ToList();
            return dashAluno;
        }).ToList();

        return response;
    }

    public ResponseModel Create(NewEventoRequest request) {
        ResponseModel response = new();

        try {
            // Verificar se todos alunos e professores passados na requisição existem
            IQueryable<Aluno> alunosInRequest = _db.Alunos
                .Where(a =>
                    a.Deactivated == null
                    && request.Alunos.Contains(a.Id));

            if (alunosInRequest.Count() != request.Alunos.Count) {
                return new ResponseModel { Message = "Aluno(s) não encontrado(s)" };
            }

            if (request.CapacidadeMaximaAlunos < alunosInRequest.Count()) {
                return new ResponseModel { Message = $"A capacidade máxima do evento ({request.CapacidadeMaximaAlunos}) não suporta a quantidade de alunos requisitada ({alunosInRequest.Count()})" };
            }

            IQueryable<Professor> professoresInRequest = _db.Professors
                .Include(p => p.Account)
                .Where(p =>
                    request.Professores.Contains(p.Id)
                    && p.Account.Deactivated == null);

            if (professoresInRequest.Count() != request.Professores.Count) {
                return new ResponseModel { Message = "Professor(es) não encontrado(s)" };
            }

            // Avaliar os tipos de evento, organizando o método para seguir as regras de cada um
            bool shouldCreateEventoAula = false;
            Evento_Tipo? eventoTipo = _db.Evento_Tipos.Find(request.Evento_Tipo_Id);

            if (eventoTipo is null) {
                return new ResponseModel { Message = "Tipo de evento não encontrado" };
            }

            switch (request.Evento_Tipo_Id) {
                case (int)EventoTipo.Oficina:
                    break;

                case (int)EventoTipo.Superacao:
                    break;

                case (int)EventoTipo.AulaExtra:
                    shouldCreateEventoAula = true;

                    // Aula Extra é independente de turma, independente do que for passado na requisição
                    request.Turma_Id = null;
                    break;

                case (int)EventoTipo.AulaZero:
                    shouldCreateEventoAula = true;

                    // Aula Zero é independente de turma, independente do que for passado na requisição
                    request.Turma_Id = null;

                    // Se o aluno já participou de alguma aula zero, então não deve ser possível inscrevê-lo novamente
                    foreach (var aluno in alunosInRequest) {
                        bool alunoAlreadyParticipated = _db.Evento_Participacao_Alunos
                            .Include(p => p.Evento)
                            .Any(p =>
                                p.Aluno_Id == aluno.Id
                                && p.Evento.Evento_Tipo_Id == (int)EventoTipo.AulaZero
                                && p.Evento.Deactivated == null);

                        if (alunoAlreadyParticipated) {
                            return new ResponseModel { Message = $"Aluno ID: '{aluno.Id}' já participou de aula zero ou possui uma agendada." };
                        }
                    }
                    break;

                case (int)EventoTipo.Aula:
                    shouldCreateEventoAula = true;

                    Turma? turmaInRequest = _db.Turmas
                        .Include(t => t.Alunos)
                        .FirstOrDefault(t => t.Id == request.Turma_Id);

                    if (turmaInRequest is null) {
                        return new ResponseModel { Message = "Turma não encontrada" };
                    }

                    // Ignorar alunos da requisição, pois os alunos estão cadastrados na turma
                    request.Alunos = turmaInRequest.Alunos
                        .Where(a => a.Deactivated == null)
                        .Select(a => a.Id)
                        .ToList();

                    break;

                case (int)EventoTipo.Reuniao:
                    if (request.Alunos.Count > 0) {
                        return new ResponseModel { Message = "Não é possível inscrever alunos em uma reunião." };
                    }

                    // Reunião é um evento entre professores (sem alunos), independente do que for passado na requisição
                    request.CapacidadeMaximaAlunos = 0;
                    break;

                default:
                    return new ResponseModel { Message = "Tipo de evento inválido" };
            }

            // Verificar se todos perfis cognitivos passados na requisição existem
            IQueryable<PerfilCognitivo> perfisCognitivosInRequest = _db.PerfilCognitivos
                .Where(p => request.PerfilCognitivo.Contains(p.Id));

            if (perfisCognitivosInRequest.Count() != request.PerfilCognitivo.Count) {
                return new ResponseModel { Message = "Ao menos um dos perfis cognitivos não existe." };
            }

            // Professores na requisição não podem ter conflitos de horário em outros eventos e/ou turmas recorrentes
            foreach (var professor in professoresInRequest) {
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
            }

            // Não devo poder registrar um evento em uma sala que não existe
            bool salaExists = _db.Salas.Any(s => s.Id == request.Sala_Id);

            if (!salaExists) {
                return new ResponseModel { Message = "Sala não encontrada" };
            }

            bool isSalaOccupied = _salaService.IsSalaOccupied(request.Sala_Id, request.Data, request.DuracaoMinutos, null);

            if (isSalaOccupied) {
                return new ResponseModel { Message = "Esta sala se encontra ocupada neste horário" };
            }

            Turma? turma = _db.Turmas.Find(request.Turma_Id);

            // Se há uma turma na requisição, mas ela não existe no banco de dados, retornar erro
            if (request.Turma_Id.HasValue && turma is null) {
                return new ResponseModel { Message = "Turma não encontrada" };
            }

            // Eventos de aula podem possuir roteiro
            Roteiro? roteiro = _db.Roteiros.FirstOrDefault(r => request.Data.Date >= r.DataInicio.Date && request.Data.Date <= r.DataFim.Date);

            // condição questionável
            //if (shouldCreateEventoAula && roteiro is null) {
            //    return new ResponseModel { Message = "Roteiro não encontrado" };
            //}

            Evento? eventoReagendado = _db.Eventos
                .Include(e => e.Evento_Participacao_Professors)
                .Include(e => e.Evento_Participacao_Alunos)
                .FirstOrDefault(e => e.Id == request.ReagendamentoDe_Evento_Id);

            if (eventoReagendado is not null) {
                eventoReagendado.Deactivated = TimeFunctions.HoraAtualBR();

                // Desativar participação dos alunos e professores no evento reagendado
                foreach (var alunoReagendado in eventoReagendado.Evento_Participacao_Alunos) {
                    alunoReagendado.Deactivated = TimeFunctions.HoraAtualBR();
                }

                foreach (var professorReagendado in eventoReagendado.Evento_Participacao_Professors) {
                    professorReagendado.Deactivated = TimeFunctions.HoraAtualBR();
                }

                _db.Update(eventoReagendado);
            }

            Evento newEvento = new()
            {
                Data = request.Data,
                Descricao = request.Descricao ?? turma?.Nome ?? $"Evento '{eventoTipo!.Nome}' sem título.",
                Observacao = request.Observacao,
                DuracaoMinutos = request.DuracaoMinutos,
                CapacidadeMaximaAlunos = request.CapacidadeMaximaAlunos,

                Sala_Id = request.Sala_Id,
                Account_Created_Id = _account!.Id,
                Evento_Tipo_Id = request.Evento_Tipo_Id,

                ReagendamentoDe_Evento_Id = request.ReagendamentoDe_Evento_Id,

                Evento_Aula = shouldCreateEventoAula ? new Evento_Aula
                {
                    Professor_Id = professoresInRequest.First().Id,
                    Turma_Id = turma?.Id,
                    Roteiro_Id = roteiro?.Id,
                } : null,

                Finalizado = false,
                Created = TimeFunctions.HoraAtualBR(),
                Deactivated = null,
                LastUpdated = null,
            };

            _db.Add(newEvento);
            _db.SaveChanges();

            // Adicionar participações de alunos, professores e os perfis cognitivos aceitos no evento

            IEnumerable<Evento_Participacao_Professor> participacoesProfessores = professoresInRequest.Select(professor => new Evento_Participacao_Professor
            {
                Evento_Id = newEvento.Id,
                Professor_Id = professor.Id,
                Observacao = null,
                Presente = null,
            });

            _db.Evento_Participacao_Professors.AddRange(participacoesProfessores);

            IEnumerable<Evento_Participacao_Aluno> participacoesAlunos = alunosInRequest.Select(aluno => new Evento_Participacao_Aluno
            {
                Aluno_Id = aluno.Id,
                Evento_Id = newEvento.Id,
                Presente = null,

                Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
                NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
                Apostila_AH_Id = aluno.Apostila_AH_Id,
                NumeroPaginaAH = aluno.NumeroPaginaAH,
            });

            IEnumerable<Aluno_Historico> historicosAlunos = alunosInRequest.Select(aluno => new Aluno_Historico
            {
                Aluno_Id = aluno.Id,
                Account_Id = _account.Id,
                Descricao = $"Aluno foi inscrito no evento de {eventoTipo.Nome} '{newEvento.Descricao}' às {newEvento.Data:g}",
                Data = newEvento.Data,
            });

            _db.Evento_Participacao_Alunos.AddRange(participacoesAlunos);
            _db.Aluno_Historicos.AddRange(historicosAlunos);

            // Pegar os perfis cognitivos passados na requisição e mapear as entidades de Evento_Aula_PerfilCognitivo
            IEnumerable<Evento_Aula_PerfilCognitivo_Rel> eventoAulaPerfisCognitivos = perfisCognitivosInRequest.Select(perfil => new Evento_Aula_PerfilCognitivo_Rel
            {
                Evento_Aula_Id = newEvento.Id,
                PerfilCognitivo_Id = perfil.Id,
            });

            // Mas apenas criar PerfilCognitivo_Rel se for um Evento_Aula
            if (shouldCreateEventoAula) {
                _db.Evento_Aula_PerfilCognitivo_Rels.AddRange(eventoAulaPerfisCognitivos);
            }

            _db.SaveChanges();

            var responseObject = _db.CalendarioEventoLists.First(e => e.Id == newEvento.Id);
            responseObject.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == newEvento.Id).ToList();
            responseObject.Professores = _db.CalendarioProfessorLists.Where(p => p.Evento_Id == newEvento.Id).ToList();
            responseObject.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfisCognitivosInRequest.ToList());

            response.Success = true;
            response.Message = $"Evento de '{responseObject.Evento_Tipo}' registrado com sucesso";
            response.Object = responseObject;
        }
        catch (Exception ex) {
            response.Message = $"Falha ao criar evento: {ex}";
        }

        return response;
    }

    public ResponseModel CreateEventValidation(CreateEventDto dto) {
        ResponseModel response = new();

        try {
            bool eventoTipoExists = _db.Evento_Tipos.Any(e => e.Id == dto.Evento_Tipo_Id);

            if (!eventoTipoExists) {
                throw new Exception("Tipo de evento não encontrado");
            }

            if (dto.CapacidadeMaximaAlunos < 0) {
                throw new Exception("Capacidade máxima de alunos não pode ser menor que 0");
            }

            bool salaExists = _db.Salas.Any(s => s.Id == dto.Sala_Id);

            if (!salaExists) {
                throw new Exception("Sala não encontrada");
            }

            bool isSalaOccupied = _salaService.IsSalaOccupied(dto.Sala_Id, dto.Data, dto.DuracaoMinutos, null);

            if (isSalaOccupied) {
                throw new Exception("Esta sala se encontra ocupada neste horário");
            }

            if (dto.DuracaoMinutos <= 0) {
                throw new Exception("Duração inválida: Valor deve ser maior que zero");
            }

            if (dto.Turma_Id.HasValue) {
                bool turmaExists = _db.Turmas.Any(t => t.Id == dto.Turma_Id);

                if (!turmaExists) {
                    throw new Exception("Turma não encontrada");
                }
            }

            if (dto.Roteiro_Id.HasValue) {
                bool roteiroExists = _db.Roteiros.Any(r => r.Id == dto.Roteiro_Id);

                if (!roteiroExists) {
                    throw new Exception("Roteiro não encontrado");
                }
            }

            IQueryable<Aluno> alunosInRequest = _db.Alunos
                .Where(a => dto.Alunos.Contains(a.Id) && a.Deactivated == null);

            if (alunosInRequest.Count() != dto.Alunos.Count) {
                throw new Exception("Aluno(s) não encontrado(s)");
            }

            IQueryable<Professor> professoresInRequest = _db.Professors
                .Include(p => p.Account)
                .Where(p => dto.Professores.Contains(p.Id) && p.Account.Deactivated == null);

            if (professoresInRequest.Count() != dto.Professores.Count) {
                throw new Exception("Professor(es) não encontrado(s)");
            }

            if (dto.PerfilCognitivo is not null) {
                IQueryable<PerfilCognitivo> perfisCognitivosInRequest = _db.PerfilCognitivos
                    .Where(p => dto.PerfilCognitivo.Contains(p.Id));

                if (perfisCognitivosInRequest.Count() != dto.PerfilCognitivo.Count) {
                    throw new Exception("Pelo menos um perfil cognitivo na requisição não foi encontrado");
                }

                HashSet<int> alunoPerfisSet = alunosInRequest
                    .Where(a => a.PerfilCognitivo_Id.HasValue)
                    .Select(a => a.PerfilCognitivo_Id!.Value).ToHashSet();

                HashSet<int> perfisCognitivosSet = perfisCognitivosInRequest
                    .Select(p => p.Id)
                    .ToHashSet();

                bool perfisCognitivosMatch = alunoPerfisSet.IsSubsetOf(perfisCognitivosSet);

                if (!perfisCognitivosMatch) {
                    throw new Exception("Algum aluno ou perfil cognitivo não é compatível");
                }
            }

            foreach (var professor in professoresInRequest) {
                bool hasTurmaConflict = _professorService.HasTurmaTimeConflict(
                    professorId: professor.Id,
                    DiaSemana: (int)dto.Data.DayOfWeek,
                    Horario: dto.Data.TimeOfDay,
                    IgnoredTurmaId: dto.Turma_Id ?? null
                );

                if (hasTurmaConflict) {
                    throw new Exception($"Professor: '{professor.Account.Name}' possui uma turma nesse mesmo horário");
                }

                bool hasParticipacaoConflict = _professorService.HasEventoParticipacaoConflict(
                    professorId: professor.Id,
                    Data: dto.Data,
                    DuracaoMinutos: dto.DuracaoMinutos,
                    IgnoredEventoId: null
                );

                if (hasParticipacaoConflict) {
                    throw new Exception($"Professor: {professor.Account.Name} possui participação em outro evento nesse mesmo horário");
                }
            }

            response.Success = true;
            response.Message = "Validação foi realizada com sucesso";
        }
        catch (Exception ex) {
            response.Success = false;
            response.Message = $"Não foi possível validar os dados da requisição: {ex.Message}";
        }

        return response;
    }

    public ResponseModel CreateEvent(CreateEventDto dto) {
        ResponseModel response = new();

        ResponseModel validationResponse = CreateEventValidation(dto);

        if (!validationResponse.Success) {
            return validationResponse;
        }

        try {
            Evento evento = new()
            {
                Data = dto.Data,
                Descricao = dto.Descricao ?? "Evento sem descrição",
                Observacao = dto.Observacao ?? "Sem observação",
                DuracaoMinutos = dto.DuracaoMinutos,
                CapacidadeMaximaAlunos = dto.CapacidadeMaximaAlunos,
                Finalizado = false,

                Sala_Id = dto.Sala_Id,

                Created = TimeFunctions.HoraAtualBR(),
                LastUpdated = null,
                Deactivated = null,
                Evento_Tipo_Id = dto.Evento_Tipo_Id,
                Account_Created_Id = _account!.Id,
            };

            _db.Eventos.Add(evento);
            _db.SaveChanges();

            IQueryable<Aluno> alunosInRequest = _db.Alunos
                .Where(a => a.Deactivated == null && dto.Alunos.Contains(a.Id));

            IQueryable<Professor> professoresInRequest = _db.Professors
                .Include(p => p.Account)
                .Where(p => p.Account.Deactivated == null && dto.Professores.Contains(p.Id));

            Evento_Tipo eventoTipo = _db.Evento_Tipos.Find(dto.Evento_Tipo_Id) ?? throw new Exception("Evento tipo não encontrado");

            // Adicionar as participações dos envolvidos no evento - Alunos e Professores
            foreach (Aluno aluno in alunosInRequest) {
                Evento_Participacao_Aluno participacao = new()
                {
                    Aluno_Id = aluno.Id,
                    Evento_Id = evento.Id,

                    // Inserir o progresso do aluno no evento
                    Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
                    NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
                    Apostila_AH_Id = aluno.Apostila_AH_Id,
                    NumeroPaginaAH = aluno.NumeroPaginaAH,
                };

                _db.Evento_Participacao_Alunos.Add(participacao);

                Aluno_Historico historico = new()
                {
                    Account_Id = _account.Id,
                    Aluno_Id = aluno.Id,
                    Data = evento.Data,
                    Descricao = $"Aluno se inscreveu em um evento de '{eventoTipo.Nome}' no dia {evento.Data:G}"
                };

                _db.Aluno_Historicos.Add(historico);
            }
            ;

            var participacoesProfessores = professoresInRequest.Select(aluno => new Evento_Participacao_Professor
            {
                Professor_Id = aluno.Id,
                Evento_Id = evento.Id,
            });

            _db.Evento_Participacao_Professors.AddRange(participacoesProfessores);

            _db.SaveChanges();

            response.Success = true;
            response.Object = evento;
            response.Message = $"Evento de '{eventoTipo.Nome}' foi criado com sucesso para o dia {evento.Data:G}";
        }
        catch (Exception ex) {
            response.Success = false;
            response.Message = $"Falha ao criar evento: {ex.Message}";
        }

        return response;
    }

    public ResponseModel CreateAulaZeroEvent(CreateEventDto dto) {
        ResponseModel response = new() { Success = false };

        ResponseModel validationResponse = CreateEventValidation(dto);

        if (!validationResponse.Success) {
            return validationResponse;
        }

        try {
            if (dto.Professores.Count <= 0) {
                throw new Exception("Um professor é necessário para criar um evento");
            }

            IQueryable<Aluno> alunosInRequest = _db.Alunos
                .Where(a => dto.Alunos.Contains(a.Id) && a.Deactivated == null);

            IQueryable<Professor> professoresInRequest = _db.Professors
                .Include(p => p.Account)
                .Where(p => dto.Professores.Contains(p.Id) && p.Account.Deactivated == null);

            int mainProfessorId = dto.Professores.First();
            List<Evento_Participacao_Aluno> participacoesAlunos = [];
            List<Aluno_Historico> historicos = [];

            Evento evento = new()
            {
                Data = dto.Data,
                Descricao = dto.Descricao ?? "Aula Zero",
                Observacao = dto.Observacao,
                Sala_Id = dto.Sala_Id,
                DuracaoMinutos = dto.DuracaoMinutos,

                Evento_Tipo_Id = (int)EventoTipo.AulaZero,
                CapacidadeMaximaAlunos = 1,
                Evento_Aula = new Evento_Aula
                {
                    Turma_Id = null,
                    Roteiro_Id = dto.Roteiro_Id,
                    Professor_Id = mainProfessorId,
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

            // Inserir progressos dos alunos no evento, associar evento à aula zero e gerar entidade de log
            foreach (var aluno in alunosInRequest) {
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
                Professor_Id = mainProfessorId,
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

    public ResponseModel CreateAulaTurmaEvent(CreateClassEventDto dto) {
        ResponseModel response = new() { Success = false };

        ResponseModel validationResponse = CreateEventValidation(dto);

        if (!validationResponse.Success) {
            return validationResponse;
        }

        try {
            // Se tem valor, roteiro existe e é válido, buscá-lo
            // Se é null, buscar roteiro para a data - se encontrar OK, senão null
            Roteiro? roteiro = dto.Roteiro_Id.HasValue ?
                _db.Roteiros.Find(dto.Roteiro_Id) :
                _db.Roteiros.FirstOrDefault(r => dto.Data.Date >= r.DataInicio.Date && dto.Data.Date <= r.DataFim.Date);

            Turma? turma = _db.Turmas.Find(dto.Turma_Id) ?? throw new Exception("Tentando criar evento-aula de turma, porém turma não foi encontrada");

            if (dto.Professores.Count <= 0) {
                throw new Exception("Um professor é necessário para criar um evento");
            }

            Evento? reagendamento = _db.Eventos.Find(dto.ReagendamentoDe_Evento_Id);

            if (dto.ReagendamentoDe_Evento_Id.HasValue && reagendamento is null) {
                throw new Exception("Erro no reagendamento. Evento original não encontrado");
            }

            if (reagendamento is not null) {
                reagendamento.Deactivated = TimeFunctions.HoraAtualBR();

                // Desativar participação dos alunos e professores
                foreach (var alunoReagendado in reagendamento.Evento_Participacao_Alunos) {
                    alunoReagendado.Deactivated = TimeFunctions.HoraAtualBR();
                }

                foreach (var professorReagendado in reagendamento.Evento_Participacao_Professors) {
                    professorReagendado.Deactivated = TimeFunctions.HoraAtualBR();
                }

                _db.Update(reagendamento);
            }

            int mainProfessorId = dto.Professores.First();

            Evento evento = new()
            {
                Data = dto.Data,
                Descricao = turma.Nome ?? dto.Descricao ?? "Sem descrição",
                Observacao = dto.Observacao,
                DuracaoMinutos = dto.DuracaoMinutos,
                CapacidadeMaximaAlunos = turma.CapacidadeMaximaAlunos,

                Evento_Tipo_Id = (int)EventoTipo.Aula,
                Evento_Aula = new Evento_Aula
                {
                    Roteiro_Id = roteiro?.Id,
                    Turma_Id = turma.Id,
                    Professor_Id = mainProfessorId,
                },

                Sala_Id = dto.Sala_Id,
                Account_Created_Id = _account!.Id,
                ReagendamentoDe_Evento_Id = dto.ReagendamentoDe_Evento_Id,

                Created = TimeFunctions.HoraAtualBR(),
                LastUpdated = null,
                Deactivated = null,
                Finalizado = false,
            };

            _db.Add(evento);
            _db.SaveChanges();

            // Inserir os registros dos alunos originais da turma na aula recém criada
            List<Aluno> alunosInTurma = _db.Alunos.Where(a =>
                a.Turma_Id == turma.Id &&
                a.Deactivated == null)
            .ToList();

            // Esse evento ignora a lista de alunos passada na requisição, a ideia é que ele instancie pseudo-eventos
            // Pseudo-eventos puxam os alunos da turma

            foreach (int professorId in dto.Professores) {
                Evento_Participacao_Professor participacaoProfessor = new()
                {
                    Evento_Id = evento.Id,
                    Professor_Id = professorId,
                };

                _db.Evento_Participacao_Professors.Add(participacaoProfessor);
            }

            foreach (Aluno aluno in alunosInTurma) {
                var participacaoAluno = new Evento_Participacao_Aluno
                {
                    Evento_Id = evento.Id,
                    Aluno_Id = aluno.Id,
                    Presente = null,

                    Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
                    NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
                    Apostila_AH_Id = aluno.Apostila_AH_Id,
                    NumeroPaginaAH = aluno.NumeroPaginaAH,
                };

                _db.Evento_Participacao_Alunos.Add(participacaoAluno);
            }

            var turmaPerfisCognitivos = _db.Turma_PerfilCognitivo_Rels
                .Where(t => t.Turma_Id == turma.Id)
                .Select(t => t.PerfilCognitivo_Id)
                .ToList();

            // Pegar os perfis cognitivos da turma e criar as entidades de Aula_PerfilCognitivo
            IEnumerable<Evento_Aula_PerfilCognitivo_Rel> eventoAulaPerfisCognitivos =
                turmaPerfisCognitivos.Select(perfilCognitivoId => new Evento_Aula_PerfilCognitivo_Rel
                {
                    PerfilCognitivo_Id = perfilCognitivoId,
                    Evento_Aula_Id = evento.Id,
                });

            _db.Evento_Aula_PerfilCognitivo_Rels.AddRange(eventoAulaPerfisCognitivos);

            _db.SaveChanges();

            var responseObject = _db.CalendarioEventoLists.First(e => e.Id == evento.Id);

            responseObject.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == evento.Id).ToList();
            responseObject.Professores = _db.CalendarioProfessorLists.Where(p => p.Evento_Id == evento.Id).ToList();
            responseObject.PerfilCognitivo = _db.Evento_Aula_PerfilCognitivo_Rels
                .Include(eap => eap.PerfilCognitivo)
                .Where(eap => eap.Evento_Aula_Id == evento.Id)
                .Select(eap => _mapper.Map<PerfilCognitivoModel>(eap.PerfilCognitivo))
                .ToList();

            response.Message = $"Evento de aula para a turma '{turma.Nome}' registrado com sucesso";
            response.Object = responseObject;
            response.Success = true;
        }
        catch (Exception ex) {
            response.Message = $"Falha ao registrar evento de aula: {ex.Message}";
        }

        return response;
    }

    public ResponseModel CreateAulaExtraEvent(CreateClassEventDto dto) {
        ResponseModel response = new() { Success = false };

        ResponseModel validationResponse = CreateEventValidation(dto);

        if (!validationResponse.Success) {
            return validationResponse;
        }

        try {
            // Se tem valor, roteiro existe e é válido, buscá-lo
            // Se é null, buscar roteiro para a data - se encontrar OK, senão null
            Roteiro? roteiro = dto.Roteiro_Id.HasValue ?
                _db.Roteiros.Find(dto.Roteiro_Id) :
                _db.Roteiros.FirstOrDefault(r => dto.Data.Date >= r.DataInicio.Date && dto.Data.Date <= r.DataFim.Date);

            if (dto.Professores.Count <= 0) {
                throw new Exception("Um professor é necessário para criar um evento");
            }

            Evento? reagendamento = _db.Eventos.Find(dto.ReagendamentoDe_Evento_Id);

            if (dto.ReagendamentoDe_Evento_Id.HasValue && reagendamento is null) {
                throw new Exception("Erro no reagendamento. Evento original não encontrado");
            }

            if (reagendamento is not null) {
                reagendamento.Deactivated = TimeFunctions.HoraAtualBR();

                // Desativar participação dos alunos e professores
                foreach (var alunoReagendado in reagendamento.Evento_Participacao_Alunos) {
                    alunoReagendado.Deactivated = TimeFunctions.HoraAtualBR();
                }

                foreach (var professorReagendado in reagendamento.Evento_Participacao_Professors) {
                    professorReagendado.Deactivated = TimeFunctions.HoraAtualBR();
                }

                _db.Update(reagendamento);
            }

            int mainProfessorId = dto.Professores.First();

            Evento evento = new()
            {
                Data = dto.Data,
                Descricao = dto.Descricao ?? "Turma extra",
                Observacao = dto.Observacao,
                Sala_Id = dto.Sala_Id,
                DuracaoMinutos = dto.DuracaoMinutos,
                CapacidadeMaximaAlunos = dto.CapacidadeMaximaAlunos,

                Evento_Tipo_Id = (int)EventoTipo.AulaExtra,
                Evento_Aula = new Evento_Aula
                {
                    Turma_Id = null,
                    Roteiro_Id = dto.Roteiro_Id,
                    Professor_Id = mainProfessorId,
                },

                Created = TimeFunctions.HoraAtualBR(),
                LastUpdated = null,
                Deactivated = null,
                Finalizado = false,
                ReagendamentoDe_Evento_Id = reagendamento?.Id,
                Account_Created_Id = _account!.Id,
            };

            _db.Add(evento);
            _db.SaveChanges();

            foreach (int professorId in dto.Professores) {
                Evento_Participacao_Professor participacaoProfessor = new()
                {
                    Evento_Id = evento.Id,
                    Professor_Id = professorId,
                };

                _db.Evento_Participacao_Professors.Add(participacaoProfessor);
            }

            IQueryable<Aluno> alunosInRequest = _db.Alunos.Where(a => a.Deactivated == null && dto.Alunos.Contains(a.Id));

            foreach (Aluno aluno in alunosInRequest) {
                var participacaoAluno = new Evento_Participacao_Aluno
                {
                    Evento_Id = evento.Id,
                    Aluno_Id = aluno.Id,
                    Presente = null,

                    Apostila_Abaco_Id = aluno.Apostila_Abaco_Id,
                    NumeroPaginaAbaco = aluno.NumeroPaginaAbaco,
                    Apostila_AH_Id = aluno.Apostila_AH_Id,
                    NumeroPaginaAH = aluno.NumeroPaginaAH,
                };

                _db.Evento_Participacao_Alunos.Add(participacaoAluno);
            }

            if (dto.PerfilCognitivo is not null) {
                IQueryable<PerfilCognitivo> perfisCognitivosInRequest = _db.PerfilCognitivos
                    .Where(p => dto.PerfilCognitivo.Contains(p.Id));

                // Pegar os perfis cognitivos passados na requisição e criar as entidades de Aula_PerfilCognitivo
                IEnumerable<Evento_Aula_PerfilCognitivo_Rel> eventoAulaPerfisCognitivos =
                    perfisCognitivosInRequest.Select(p => new Evento_Aula_PerfilCognitivo_Rel
                    {
                        Evento_Aula_Id = evento.Id,
                        PerfilCognitivo_Id = p.Id
                    });

                _db.Evento_Aula_PerfilCognitivo_Rels.AddRange(eventoAulaPerfisCognitivos);
            }

            _db.SaveChanges();

            var responseObject = _db.CalendarioEventoLists.First(e => e.Id == evento.Id);

            responseObject.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == evento.Id).ToList();
            responseObject.Professores = _db.CalendarioProfessorLists.Where(p => p.Evento_Id == evento.Id).ToList();
            responseObject.PerfilCognitivo = _db.Evento_Aula_PerfilCognitivo_Rels
                .Include(eap => eap.PerfilCognitivo)
                .Where(eap => eap.Evento_Aula_Id == evento.Id)
                .Select(eap => _mapper.Map<PerfilCognitivoModel>(eap.PerfilCognitivo))
                .ToList();

            response.Message = $"Evento de aula extra registrado com sucesso";
            response.Object = responseObject;
            response.Success = true;
        }
        catch (Exception ex) {
            response.Message = $"Falha ao registrar evento de aula extra: {ex.Message}";
        }

        return response;
    }

    // Performance: Always 4 queries for n events
    private List<CalendarioEventoList> PopulateCalendarioEvents(List<CalendarioEventoList> events) {
        List<int> eventoIds = events.Select(e => e.Id).ToList();

        List<int> eventoAulaIds = events
            .Where(e => e.Evento_Tipo_Id == (int)EventoTipo.Aula || e.Evento_Tipo_Id == (int)EventoTipo.AulaExtra)
            .Select(e => e.Id)
            .ToList();

        List<CalendarioAlunoList> allAlunos = _db.CalendarioAlunoLists
            .Where(a => eventoIds.Contains(a.Evento_Id))
            .OrderBy(a => a.Aluno)
            .ToList();

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

        foreach (var calendarioEvent in events) {
            alunosDictionary.TryGetValue(calendarioEvent.Id, out var alunosInEvent);
            calendarioEvent.Alunos = alunosInEvent ?? new List<CalendarioAlunoList>();

            professorDictionary.TryGetValue(calendarioEvent.Id, out var professoresInEvent);
            calendarioEvent.Professores = professoresInEvent ?? new List<CalendarioProfessorList>();

            if (perfisDictionary.TryGetValue(calendarioEvent.Id, out var aulaPerfisInEvent)) {
                calendarioEvent.PerfilCognitivo = aulaPerfisInEvent;
            }
            else {
                calendarioEvent.PerfilCognitivo = new List<PerfilCognitivoModel>();
            }
        }

        return events;
    }
}
