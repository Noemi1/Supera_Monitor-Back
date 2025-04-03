using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Eventos;
using Supera_Monitor_Back.Models.Eventos.Aula;

namespace Supera_Monitor_Back.Services.Eventos;

public interface IEventoService {
    public ResponseModel Insert(CreateEventoRequest request, int eventoTipoId);
    public ResponseModel Update(UpdateEventoRequest request);
    public ResponseModel Cancelar(int eventoId);

    public ResponseModel EnrollAluno(EnrollAlunoRequest request);
    public List<CalendarioEventoList> GetCalendario(CalendarioRequest request);
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

            string eventoTipo;

            switch (eventoTipoId) {
            case ( int )EventoTipo.Reuniao:
                if (request.Alunos.Count != 0) {
                    return new ResponseModel { Message = "Um evento de reunião não pode ter alunos associados" };
                }
                eventoTipo = "Reunião";
                break;

            case ( int )EventoTipo.Oficina:
                eventoTipo = "Oficina";
                break;

            case ( int )EventoTipo.AulaZero:
                if (request.Alunos.Count != 1) {
                    return new ResponseModel { Message = "Um evento de aula zero deve ter exatamente um aluno associado" };
                }

                if (request.Professores.Count != 1) {
                    return new ResponseModel { Message = "Um evento de aula zero deve ter exatamente um professor associado" };
                }
                eventoTipo = "Aula Zero";
                break;

            case ( int )EventoTipo.Superacao:
                if (request.Alunos.Count != 1) {
                    return new ResponseModel { Message = "Um evento de Superação só pode ter um aluno associado" };
                }
                eventoTipo = "Superação";
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
            var duracaoBefore = request.Data.AddMinutes(-( int )request.DuracaoMinutos);
            var duracaoAfter = request.Data.AddMinutes(( int )request.DuracaoMinutos);

            bool isSalaOccupied = _db.Eventos.Any(e =>
                e.Deactivated == null
                && e.Sala_Id == request.Sala_Id
                && e.Data.Date == request.Data.Date
                && e.Data > duracaoBefore
                && e.Data < duracaoAfter);

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

            foreach (var participacao in participacoesAlunos) {
                _db.Evento_Participacao_Alunos.Add(participacao);

                _db.Aluno_Historicos.Add(new Aluno_Historico {
                    Account_Id = _account.Id,
                    Aluno_Id = participacao.Aluno_Id,
                    Data = evento.Data,
                    Descricao = $"Aluno se inscreveu em um evento de '{eventoTipo}' no dia {evento.Data:G}"
                });
            }

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
            var duracaoBefore = request.Data.AddMinutes(-( int )request.DuracaoMinutos);
            var duracaoAfter = request.Data.AddMinutes(( int )request.DuracaoMinutos);

            bool isSalaOccupied = _db.Eventos.Any(e =>
                e.Id != evento.Id
                && e.Deactivated == null
                && e.Sala_Id == request.Sala_Id
                && e.Data.Date == request.Data.Date
                && e.Data > duracaoBefore
                && e.Data < duracaoAfter);

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

    public List<CalendarioEventoList> GetCalendario(CalendarioRequest request)
    {
        DateTime now = TimeFunctions.HoraAtualBR();

        request.IntervaloDe ??= GetThisWeeksMonday(now); // Se não passar data inicio, considera a segunda-feira da semana atual
        request.IntervaloAte ??= GetThisWeeksSaturday(( DateTime )request.IntervaloDe); // Se não passar data fim, considera o sábado da semana da data inicio

        if (request.IntervaloAte < request.IntervaloDe) {
            throw new Exception("Final do intervalo não pode ser antes do seu próprio início");
        }

        IQueryable<CalendarioEventoList> eventos = _db.CalendarioEventoLists
            .Where(e => e.Data.Date >= request.IntervaloDe.Value.Date && e.Data.Date <= request.IntervaloAte.Value.Date);

        IQueryable<Turma> turmas = _db.Turmas
            .Where(t => t.Deactivated == null)
            .Include(t => t.Professor!)
            .ThenInclude(t => t.Account)
            .Include(t => t.Sala);

        IQueryable<Professor> professores = _db.Professors
            .Include(p => p.Account)
            .Where(p => p.Account.Deactivated == null);

        // TODO: Filtro de perfil cognitivo
        if (request.Perfil_Cognitivo_Id.HasValue) {
            // Eventos que contem o perfil cognitivo 
            var eventosContemPerfilCognitivo = _db.Evento_Aula_PerfilCognitivo_Rels.Where(x => x.PerfilCognitivo_Id == request.Perfil_Cognitivo_Id);

            eventos = eventos.Where(e => eventosContemPerfilCognitivo.Any(x => x.Evento_Aula_Id == e.Id));
            turmas = turmas.Where(e => e.Turma_PerfilCognitivo_Rels.Any(x => x.Id == request.Perfil_Cognitivo_Id));
        }

        if (request.Turma_Id.HasValue) {
            eventos = eventos.Where(e => e.Turma_Id != null && e.Turma_Id == request.Turma_Id);
            turmas = turmas.Where(t => t.Id == request.Turma_Id);
        }

        if (request.Professor_Id.HasValue) {
            // Busca o professor em evento.Professor_Id e evento.Evento_Participacao_Professor
            var eventosContemProfessor = _db.Evento_Participacao_Professors.Where(x => x.Professor_Id == request.Professor_Id.Value);
            eventos = eventos.Where(e => e.Professor_Id != null && (e.Professor_Id == request.Professor_Id || eventosContemProfessor.Any(x => x.Evento_Id == e.Id)));
            turmas = turmas.Where(t => t.Professor_Id == request.Professor_Id);
            professores = professores.Where(x => x.Id == request.Professor_Id.Value);
        }

        if (request.Aluno_Id.HasValue) {
            var aluno = _db.Alunos.FirstOrDefault(a => a.Id == request.Aluno_Id);

            if (aluno is not null) {
                turmas = turmas.Where(t => t.Id == aluno.Turma_Id);
                // Busca o aluno em evento.Evento_Participacao_Aluno fora do where de eventos, para fazer menos filtros
                var eventosContemAlunos = _db.Evento_Participacao_Alunos.Where(x => x.Aluno_Id == request.Aluno_Id.Value);
                eventos = eventos.Where(e => eventosContemAlunos.Any(p => p.Evento_Id == e.Id));
            }
        }

        // Adicionar aulas instanciadas ao retorno
        List<CalendarioEventoList> calendarioResponse = eventos.ToList();

        // Adicionar os alunos e perfis cognitivos às aulas instanciadas
        foreach (CalendarioEventoList evento in calendarioResponse) {
            evento.Alunos = _db.CalendarioAlunoLists.Where(a => a.Evento_Id == evento.Id).ToList();
            evento.Professores = _db.CalendarioProfessorLists.Where(e => e.Evento_Id == evento.Id).ToList();

            if (evento.Evento_Tipo_Id == ( int )EventoTipo.Aula || evento.Evento_Tipo_Id == ( int )EventoTipo.AulaExtra) {
                var perfisCognitivos = _db.Evento_Aula_PerfilCognitivo_Rels
                    .Where(p => p.Evento_Aula_Id == evento.Id)
                    .Include(p => p.PerfilCognitivo)
                    .Select(p => p.PerfilCognitivo)
                    .ToList();

                evento.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfisCognitivos);
            }
        }

        // Carrega lista de roteiros no intervalo selecionado
        IQueryable<Roteiro> roteiros = _db.Roteiros.Where(x => x.DataInicio.Date <= request.IntervaloDe.Value.Date && x.DataFim.Date <= request.IntervaloAte.Value.Date);

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
                    .FirstOrDefault(a => a.Data.Date == data.Date);

                // Não usar mais o continue porque o método adiciona outros pseudo eventos
                if (eventoOficina is null) {
                    var roteiro = roteiros.FirstOrDefault(x => data.Date <= x.DataInicio.Date && data >= x.DataFim);
                    CalendarioEventoList pseudoOficina = new() {
                        Id = -1,
                        Evento_Tipo_Id = ( int )EventoTipo.Oficina,
                        Evento_Tipo = "Pseudo-Oficina",

                        Descricao = "Oficina - Tema indefinido",
                        DuracaoMinutos = 60,

                        Roteiro_Id = roteiro is null ? -1 : roteiro.Id,
                        Semana = roteiro?.Semana,
                        Tema = roteiro?.Tema,

                        Data = new DateTime(data.Year, data.Month, data.Day, 10, 0, 0),
                        Finalizado = false,
                        Sala_Id = -1,
                    };
                    calendarioResponse.Add(pseudoOficina);
                }

            }

            //
            // Adiciona Reunião - Se a já existe uma reunião agendada para a data, não vai adicionar
            //
            if (diaSemana == DayOfWeek.Monday || diaSemana == DayOfWeek.Tuesday || diaSemana == DayOfWeek.Friday) {
                CalendarioEventoList? eventoReuniao = calendarioResponse.FirstOrDefault(a => a.Data.Date == data.Date);

                // Não usar mais o continue porque o método adiciona outros pseudo eventos
                if (eventoReuniao is null) {
                    var descricao = diaSemana == DayOfWeek.Monday ? "Reunião Geral" : // Segunda-feira
                                    diaSemana == DayOfWeek.Tuesday ? "Reunião Monitoramento" : // Terça-feira
                                    "Reunião Pedagógica"; // Sexta-feira

                    CalendarioEventoList pseudoReuniao = new() {
                        Id = -1,
                        Evento_Tipo_Id = ( int )EventoTipo.Reuniao,
                        Evento_Tipo = "Pseudo-Reuniao",
                        Data = new DateTime(data.Year, data.Month, data.Day, 12, 0, 0),
                        Descricao = descricao,
                        DuracaoMinutos = 60,
                        Finalizado = false,
                        Sala_Id = -1,
                        Professores = professores.Select(professor => new CalendarioProfessorList {
                            Evento_Id = -1,
                            Professor_Id = professor.Id,
                            Nome = professor.Account.Name,
                            CorLegenda = professor.CorLegenda,
                            Account_Id = professor.Account.Id,
                        }).ToList()
                    };
                    calendarioResponse.Add(pseudoReuniao);
                }
            }


            //
            // Adicionar aulas da turma do dia que ainda não foram instanciadas
            //
            List<Turma> turmasDoDia = turmas.Where(t => t.DiaSemana == ( int )data.DayOfWeek).ToList();

            foreach (Turma turma in turmasDoDia) {

                // Se a turma já tem uma aula instanciada no mesmo horário, é uma aula repetida, então ignora e passa pra proxima
                CalendarioEventoList? eventoAula = calendarioResponse.FirstOrDefault(a =>
                    ( int )a.Data.DayOfWeek == turma.DiaSemana &&
                    a.Data.TimeOfDay == turma.Horario &&
                    a.Turma_Id == turma.Id);

                // Não usar mais o continue porque o método adiciona outros pseudo eventos
                if (eventoAula is null) {
                    var roteiro = roteiros.FirstOrDefault(x => data.Date <= x.DataInicio.Date && data >= x.DataFim);
                    CalendarioEventoList pseudoAula = new() {
                        Id = -1,
                        Evento_Tipo_Id = ( int )EventoTipo.Aula,
                        Evento_Tipo = "Pseudo-Aula",

                        Descricao = turma.Nome, // Pseudo aulas ganham o nome da turma
                        DuracaoMinutos = 120, // As pseudo aulas são de uma turma e duram 2h

                        Roteiro_Id = roteiro is null ? -1 : roteiro.Id,
                        Semana = roteiro?.Semana,
                        Tema = roteiro?.Tema,

                        Data = new DateTime(data.Year, data.Month, data.Day, turma.Horario!.Value.Hours, turma.Horario!.Value.Minutes, turma.Horario!.Value.Seconds),

                        Turma_Id = turma.Id,
                        Turma = turma.Nome,
                        CapacidadeMaximaAlunos = turma.CapacidadeMaximaAlunos,

                        Professor_Id = turma.Professor_Id ?? -1,
                        Professor = turma.Professor is not null ? turma.Professor.Account.Name : "Professor indefinido",
                        CorLegenda = turma.Professor is not null ? turma.Professor.CorLegenda : "#000",

                        Finalizado = false,
                        Sala_Id = turma.Sala?.Id ?? -1,
                        NumeroSala = turma.Sala?.NumeroSala,
                        Andar = turma.Sala?.Andar,
                    };

                    // Na pseudo-aula, adicionar só os alunos da turma original
                    List<AlunoList> alunos = _db.AlunoLists
                        .Where(a => a.Turma_Id == turma.Id)
                        .OrderBy(a => a.Nome)
                        .ToList();

                    pseudoAula.Alunos = _mapper.Map<List<CalendarioAlunoList>>(alunos);

                    pseudoAula.Professores.Add(new CalendarioProfessorList {
                        Evento_Id = pseudoAula.Id,
                        Professor_Id = ( int )turma.Professor_Id,
                        Nome = turma.Professor.Account.Name,
                        CorLegenda = turma.Professor.CorLegenda,
                        Presente = null,
                        Observacao = "",
                        Account_Id = turma.Professor.Account.Id,
                    });

                    List<PerfilCognitivo> perfisCognitivos = _db.Turma_PerfilCognitivo_Rels
                        .Where(p => p.Turma_Id == pseudoAula.Turma_Id)
                        .Include(p => p.PerfilCognitivo)
                        .Select(p => p.PerfilCognitivo)
                        .ToList();

                    pseudoAula.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfisCognitivos);

                    calendarioResponse.Add(pseudoAula);
                }


            }

            data = data.AddDays(1);
        }

        // Adicionar alunos associados a todos os eventos

        return calendarioResponse;
    }

    private static DateTime GetThisWeeksMonday(DateTime date)
    {
        var response = date.AddDays(-( int )date.DayOfWeek);
        return response.AddDays(1);
    }

    private static DateTime GetThisWeeksSaturday(DateTime date)
    {
        var response = date.AddDays(-( int )date.DayOfWeek);
        return response.AddDays(6);
    }

    public ResponseModel EnrollAluno(EnrollAlunoRequest request)
    {
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

            // Dependendo do tipo de evento, não deve poder inscrever mais um aluno
            switch (evento.Evento_Tipo_Id) {
            case ( int )EventoTipo.Aula:
                if (amountOfAlunosEnrolled >= evento.Evento_Aula!.CapacidadeMaximaAlunos) {
                    return new ResponseModel { Message = "Este evento de aula se encontra lotado." };
                }
                break;

            case ( int )EventoTipo.AulaExtra:
                if (amountOfAlunosEnrolled >= evento.Evento_Aula!.CapacidadeMaximaAlunos) {
                    return new ResponseModel { Message = "Este evento de aula extra se encontra lotado." };
                }
                break;

            case ( int )EventoTipo.AulaZero:
                if (amountOfAlunosEnrolled >= 1) {
                    return new ResponseModel { Message = "Este evento de aula zero se encontra lotado." };
                }
                break;

            case ( int )EventoTipo.Reuniao:
                return new ResponseModel { Message = "Não é possível inscrever alunos em uma reunião." };

            case ( int )EventoTipo.Superacao:
                if (amountOfAlunosEnrolled >= 1) {
                    return new ResponseModel { Message = "Este evento de Superação se encontra lotado." };
                }
                break;

            default:
                break;
            }

            // Validations passed

            Evento_Participacao_Aluno newParticipacao = new() {
                Evento_Id = evento.Id,
                Aluno_Id = aluno.Id,
            };

            _db.Evento_Participacao_Alunos.Add(newParticipacao);
            _db.SaveChanges();

            response.Message = $"Aluno foi inscrito no evento com sucesso";
            response.Object = newParticipacao;
            response.Success = true;
        } catch (Exception ex) {
            response.Message = $"Falha ao inscrever aluno no evento: {ex}";
        }

        return response;
    }

    public ResponseModel Cancelar(int eventoId)
    {
        ResponseModel response = new() { Success = false };

        try {
            Evento? evento = _db.Eventos.FirstOrDefault(e => e.Id == eventoId);

            if (evento is null) {
                return new ResponseModel { Message = "Evento não encontrado." };
            }

            if (evento.Deactivated.HasValue) {
                return new ResponseModel { Message = "Evento já foi cancelado" };
            }

            // Validations passed

            evento.Deactivated = TimeFunctions.HoraAtualBR();

            _db.Eventos.Update(evento);
            _db.SaveChanges();

            var responseObject = _db.Eventos.Where(e => e.Id == evento.Id)
                .ProjectTo<EventoModel>(_mapper.ConfigurationProvider)
                .AsNoTracking()
                .First();

            response.Message = $"Evento foi cancelado com sucesso";
            response.Object = responseObject;
            response.Success = true;
        } catch (Exception ex) {
            response.Message = $"Falha ao cancelar evento: {ex}";
        }

        return response;
    }
}
