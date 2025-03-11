using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aula;

namespace Supera_Monitor_Back.Services {
    public interface IAulaService {
        CalendarioList Get(int aulaId);
        ResponseModel Insert(CreateAulaRequest model);
        ResponseModel Update(UpdateAulaRequest model);
        ResponseModel Delete(int aulaId);

        List<CalendarioList> GetAll();
        List<CalendarioList> GetAllByTurmaId(int turmaId);
        List<CalendarioList> GetAllByProfessorId(int professorId);

        List<CalendarioResponse> Calendario(CalendarioRequest request);

        ResponseModel RegisterChamada(RegisterChamadaRequest model);
        ResponseModel ReagendarAula(ReagendarAulaRequest model);
    }

    public class AulaService : IAulaService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;
        private readonly IProfessorService _professorService;

        public AulaService(DataContext db, IMapper mapper, IProfessorService professorService)
        {
            _db = db;
            _mapper = mapper;
            _professorService = professorService;
        }

        public CalendarioList Get(int aulaId)
        {
            CalendarioList? aula = _db.CalendarioList.FirstOrDefault(a => a.Aula_Id == aulaId);

            if (aula == null) {
                throw new Exception("Aula não encontrada");
            }

            return aula;
        }

        public List<CalendarioList> GetAll()
        {
            List<CalendarioList> aulas = _db.CalendarioList.ToList();

            return aulas;
        }

        public List<CalendarioList> GetAllByTurmaId(int turmaId)
        {
            List<CalendarioList> aulas = _db.CalendarioList.Where(a => a.Turma_Id == turmaId).ToList();

            return aulas;
        }

        public List<CalendarioList> GetAllByProfessorId(int professorId)
        {
            List<CalendarioList> aulas = _db.CalendarioList.Where(a => a.Professor_Id == professorId).ToList();

            return aulas;
        }

        public ResponseModel Insert(CreateAulaRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                // Se Turma_Id passado na requisição for NÃO NULO, a turma deve existir

                Turma? turma = null;

                if (model.Turma_Id.HasValue) {
                    turma = _db.Turma.Find(model.Turma_Id);

                    if (turma is null) {
                        return new ResponseModel { Message = "Turma não encontrada" };
                    }
                }

                // Não devo poder registrar uma aula com um professor que não existe
                Professor? professor = _db.Professor
                    .Include(p => p.Account)
                    .FirstOrDefault(p => p.Id == model.Professor_Id);

                if (professor is null) {
                    return new ResponseModel { Message = "Professor não encontrado" };
                }

                // Não devo poder registrar uma aula com um professor que está desativado
                if (professor.Account.Deactivated is not null) {
                    return new ResponseModel { Message = "Este professor está desativado" };
                }

                bool professorHasTurmaConflict = _professorService.HasTurmaTimeConflict(
                    model.Professor_Id,
                    ( int )model.Data.DayOfWeek,
                    model.Data.TimeOfDay,
                    IgnoredTurmaId: turma?.Id);

                if (professorHasTurmaConflict) {
                    return new ResponseModel { Message = "O professor já tem uma turma nesse horário" };
                }

                bool professorHasAulaConflict = _professorService.HasAulaTimeConflict(
                    model.Professor_Id,
                    model.Data,
                    IgnoredAulaId: null);

                if (professorHasAulaConflict) {
                    return new ResponseModel { Message = "O professor já tem uma aula nesse horário" };
                }

                // Não devo poder registrar uma aula em uma sala que não existe
                bool salaExists = _db.Sala.Any(s => s.Id == model.Sala_Id);

                if (!salaExists) {
                    return new ResponseModel { Message = "Sala não encontrada" };
                }

                // Validations passed

                Aula aula = new() {
                    Data = model.Data,
                    Observacao = model.Observacao,
                    Sala_Id = model.Sala_Id,
                    Professor_Id = model.Professor_Id,
                    Turma_Id = model.Turma_Id,
                    Created = TimeFunctions.HoraAtualBR(),
                    Descricao = !string.IsNullOrEmpty(model.Descricao) ? model.Descricao : turma?.Nome ?? "Aula independente",
                    Finalizada = false,
                };

                _db.Aula.Add(aula);
                _db.SaveChanges();

                // Inserir os registros dos alunos originais na aula recém criada
                // Se for uma aula sem Turma, então essa lista é vazia, e por padrão não será inserido nenhum registro de aluno
                List<Aluno> alunos = _db.Aluno.Where(a => a.Turma_Id == aula.Turma_Id).ToList();

                foreach (Aluno aluno in alunos) {
                    Aula_Aluno registro = new() {
                        Aula_Id = aula.Id,
                        Aluno_Id = aluno.Id,
                        Presente = null,
                    };

                    _db.Aula_Aluno.Add(registro);
                }

                _db.SaveChanges();

                response.Message = "Aula registrada com sucesso";
                response.Object = _db.CalendarioList.FirstOrDefault(a => a.Aula_Id == aula.Id);
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao registrar aula: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel Update(UpdateAulaRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                Aula? aula = _db.Aula.Find(model.Id);

                // Não devo poder atualizar uma aula que não existe
                if (aula == null) {
                    return new ResponseModel { Message = "Aula não encontrada" };
                }

                Professor? professor = _db.Professor
                    .Include(p => p.Account)
                    .FirstOrDefault(p => p.Id == model.Professor_Id);

                // Não devo poder atualizar turma com um professor que não existe
                if (professor is null) {
                    return new ResponseModel { Message = "Professor não encontrado" };
                }

                // Não devo poder atualizar turma com uma sala que não existe
                bool salaExists = _db.Sala.Any(s => s.Id == model.Sala_Id);

                if (salaExists == false) {
                    return new ResponseModel { Message = "Sala não encontrada" };
                }

                // Não devo poder atualizar turma com um professor que está desativado
                if (professor.Account.Deactivated is not null) {
                    return new ResponseModel { Message = "Professor está desativado" };
                }

                // Se estou trocando de professor, o novo professor não pode estar ocupado nesse dia da semana / horário
                if (aula.Professor_Id != model.Professor_Id) {
                    bool professorHasTurmaConflict = _professorService.HasTurmaTimeConflict(
                        model.Professor_Id,
                        ( int )aula.Data.DayOfWeek,
                        aula.Data.TimeOfDay,
                        IgnoredTurmaId: aula.Turma_Id);

                    if (professorHasTurmaConflict) {
                        return new ResponseModel { Message = "O professor já tem uma turma nesse horário" };
                    }

                    bool professorHasAulaConflict = _professorService.HasAulaTimeConflict(
                        model.Professor_Id,
                        aula.Data,
                        IgnoredAulaId: aula.Id);

                    if (professorHasAulaConflict) {
                        return new ResponseModel { Message = "O professor já tem uma aula nesse horário" };
                    }
                }

                // Validations passed

                response.OldObject = _db.CalendarioList.FirstOrDefault(a => a.Aula_Id == model.Id);

                aula.Sala_Id = model.Sala_Id;
                aula.Professor_Id = model.Professor_Id;
                aula.Observacao = model.Observacao;
                aula.Descricao = model.Descricao ?? aula.Descricao ?? "";
                aula.LastUpdated = TimeFunctions.HoraAtualBR();

                _db.Aula.Update(aula);
                _db.SaveChanges();

                response.Message = "Aula atualizada com sucesso";
                response.Object = _db.CalendarioList.FirstOrDefault(a => a.Aula_Id == model.Id);
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao atualizar a aula: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel Delete(int aulaId)
        {
            throw new NotImplementedException();
        }

        public List<CalendarioResponse> Calendario(CalendarioRequest request)
        {
            DateTime now = TimeFunctions.HoraAtualBR();

            // Se não passar data inicio, considera a segunda-feira da semana atual
            if (!request.IntervaloDe.HasValue) {
                // Retorna para o início da semana (domingo) e adiciona um dia para obter segunda-feira
                request.IntervaloDe = now.AddDays(-( int )now.DayOfWeek);
                request.IntervaloDe = request.IntervaloDe.Value.AddDays(1);
            }

            // Se não passar data fim, considera o sábado da semana da data inicio
            if (!request.IntervaloAte.HasValue) {
                // Retorna para o início da semana (domingo) e adiciona seis dias para obter sábado
                request.IntervaloAte = request.IntervaloDe.Value.AddDays(-( int )request.IntervaloDe.Value.DayOfWeek);
                request.IntervaloAte = request.IntervaloDe.Value.AddDays(6);
            }

            if (request.IntervaloAte.Value < request.IntervaloDe.Value) {
                throw new Exception("Final do intervalo não pode ser antes do seu próprio início");
            }

            // Pegar todas as aulas instanciadas no intervalo
            List<Aula> aulas = _db.Aula
                .Where(a =>
                    a.Deactivated == null &&
                    a.Data.Date >= request.IntervaloDe.Value.Date &&
                    a.Data.Date <= request.IntervaloAte.Value.Date)
                .Include(a => a.Aula_Aluno)
                .Include(a => a.Turma)
                .Include(a => a.Professor)
                .Include(a => a.Professor.Account)
                .Include(a => a.Sala)
                .ToList();

            List<Turma> turmas = _db.Turma
                .Where(t => t.Deactivated == null)
                .Include(t => t.Professor)
                .ToList();

            List<Professor> professores = _db.Professor
                .Include(p => p.Account)
                .Where(p => p.Account.Deactivated == null)
                .ToList();

            // Aplicar filtros

            // Filtro de Turma
            if (request.Turma_Id.HasValue) {
                turmas = turmas.Where(x => x.Id == request.Turma_Id.Value).ToList();
                aulas = aulas.Where(x => x.Turma_Id == request.Turma_Id.Value).ToList();
                //aulasIndependentes.Clear();
            }

            // Filtro de Perfil Cognitivo
            if (request.Perfil_Cognitivo_Id.HasValue) {
                int perfilId = request.Perfil_Cognitivo_Id.Value;

                // Filtrar turmas que possuem esse perfil cognitivo
                turmas = turmas
                    .Where(t => _db.Turma_PerfilCognitivo_Rel.Any(tp => tp.Turma_Id == t.Id && tp.PerfilCognitivo_Id == perfilId))
                    .ToList();

                // Obter IDs das turmas filtradas
                List<int> turmaIds = turmas.Select(t => t.Id).ToList();

                // Filtrar aulas apenas para as turmas que passaram no filtro
                aulas = aulas.Where(a =>
                    a.Turma_Id.HasValue &&
                    turmaIds.Contains(a.Turma_Id.Value))
                .ToList();

                // Como aulas independentes não pertencem a turmas, todas devem ser removidas
                //aulasIndependentes.Clear();
            }

            // Filtro de Professor
            if (request.Professor_Id.HasValue) {
                turmas = turmas.Where(x => x.Professor_Id == request.Professor_Id.Value).ToList();
                aulas = aulas.Where(x => x.Professor_Id == request.Professor_Id.Value).ToList();
                //aulasIndependentes = aulasIndependentes.Where(x => x.Professor_Id == request.Professor_Id.Value).ToList();
            }

            // Filtro de Aluno
            if (request.Aluno_Id.HasValue) {
                int alunoId = request.Aluno_Id.Value;

                // Buscar a turma do aluno (o aluno sempre pertence a uma turma)
                var aluno = _db.Aluno.FirstOrDefault(x => x.Id == alunoId);

                if (aluno != null) {
                    // Filtrar turmas pela turma do aluno
                    turmas = turmas.Where(x => x.Id == aluno.Turma_Id).ToList();

                    // Filtrar aulas da turma do aluno

                    List<int> aulasIds = aulas
                        .SelectMany(a => a.Aula_Aluno)
                        .Where(x => x.Aluno_Id == aluno.Id)
                        .Select(x => x.Aula_Id)
                        .ToList();

                    aulas = aulas.Where(x => aulasIds.Contains(x.Id)).ToList();
                }
            }

            List<CalendarioResponse> calendario = new();

            // Adicionar todas as aulas instanciadas - Aulas instanciadas de turmas + Aulas independentes
            foreach (Aula aula in aulas) {
                CalendarioList calendarioList = new() {
                    Aula_Id = aula.Id,
                    Data = aula.Data,

                    Turma_Id = aula.Turma_Id,
                    Turma = aula.Turma is null ? "" : aula.Turma.Nome,

                    Professor_Id = aula.Professor_Id,
                    Professor = aula.Professor.Account.Name ?? "Professor indefinido",
                    CorLegenda = aula.Professor.CorLegenda ?? "#000",
                    Finalizada = aula.Finalizada,

                    CapacidadeMaximaAlunos = aula.Turma?.CapacidadeMaximaAlunos,

                    Sala_Id = aula.Sala_Id,
                    NumeroSala = aula.Sala.NumeroSala,
                    Andar = aula.Sala.Andar,
                    Observacao = "",
                };

                CalendarioResponse agendamento = _mapper.Map<CalendarioResponse>(calendarioList);

                agendamento.Alunos = _db.CalendarioAlunoList
                    .Where(a =>
                        a.Aula_Id == aula.Id &&
                        a.Deactivated == null)
                    .ToList();

                calendario.Add(agendamento);
            };

            DateTime data = request.IntervaloDe.Value;

            // Adicionar todas as aulas não instanciadas - Aulas de turmas que tem horário marcado
            while (data < request.IntervaloAte) {
                List<Turma> turmasDoDia = turmas.Where(t => t.DiaSemana == ( int )data.DayOfWeek).ToList();

                foreach (Turma turma in turmasDoDia) {
                    Aula? aula = aulas.FirstOrDefault(a =>
                        ( int )a.Data.DayOfWeek == turma.DiaSemana &&
                        a.Data.TimeOfDay == turma.Horario &&
                        a.Turma_Id == turma.Id);

                    // Se a aula foi encontrada, ignora e passa pro proximo
                    if (aula is not null) {
                        continue;
                    }

                    CalendarioList calendarioList = new() {
                        Aula_Id = -1,
                        Data = new DateTime(data.Year, data.Month, data.Day, turma.Horario!.Value.Hours, turma.Horario!.Value.Minutes, turma.Horario!.Value.Seconds),

                        Turma_Id = turma.Id,
                        Turma = turma.Nome,

                        Professor_Id = turma.Professor_Id ?? -1,
                        Professor = turma.Professor is not null ? turma.Professor.Account.Name : "Professor indefinido",

                        CorLegenda = turma.Professor is not null ? turma.Professor.CorLegenda : "#000",

                        CapacidadeMaximaAlunos = turma.CapacidadeMaximaAlunos,

                        Finalizada = false,

                        Sala_Id = turma.Sala_Id,
                        NumeroSala = turma.Sala?.NumeroSala,
                        Andar = turma.Sala?.Andar,
                        Observacao = "",
                    };

                    CalendarioResponse agendamento = _mapper.Map<CalendarioResponse>(calendarioList);

                    // Na pseudo-aula, adicionar só os alunos da turma original
                    List<AlunoList> alunos = _db.AlunoList
                        .Where(a => a.Turma_Id == turma.Id)
                        .ToList();

                    agendamento.Alunos = _mapper.Map<List<CalendarioAlunoList>>(alunos);

                    calendario.Add(agendamento);
                }

                data = data.AddDays(1);
            }

            return calendario;
        }

        public ResponseModel RegisterChamada(RegisterChamadaRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                Aula? aula = _db.Aula.Find(model.Aula_Id);

                // Não devo poder realizar a chamada em uma aula que não existe
                if (aula == null) {
                    return new ResponseModel { Message = "Aula não encontrada" };
                }

                Professor? professor = _db.Professor.Find(model.Professor_Id);

                if (professor is null) {
                    return new ResponseModel { Message = "Professor não encontrado" };
                }

                // Se indicar uma mudança de professor - o professor sendo alocado não pode ter aula no mesmo horário
                if (model.Professor_Id != aula.Professor_Id) {
                    bool ProfessorIsAlreadyOccupied = _db.Aula.Any(a =>
                        a.Professor_Id == model.Professor_Id &&
                        a.Data == aula.Data
                    );

                    if (ProfessorIsAlreadyOccupied) {
                        return new ResponseModel { Message = "O professor sendo alocado já tem uma aula nesse horário." };
                    }
                }

                // Validations passed

                // Buscar registros / alunos / apostilas previamente para reduzir o número de requisições ao banco
                Dictionary<int, Aula_Aluno> registros = _db.Aula_Aluno
                    .Where(x => model.Registros.Select(r => r.Turma_Aula_Aluno_Id).Contains(x.Id))
                    .ToDictionary(x => x.Id);

                Dictionary<int, Aluno> alunos = _db.Aluno
                    .Where(x => registros.Values.Select(r => r.Aluno_Id).Contains(x.Id))
                    .ToDictionary(x => x.Id);

                // Agrupar todos ids de apostilas passados nos registros
                List<int> apostilasIds = model.Registros.SelectMany(r => new[] { r.Apostila_Abaco_Id, r.Apostila_Ah_Id }).Distinct().ToList();

                // Coletar previamente todas as apostilas que contenham qualquer dos ids
                List<Apostila_Kit_Rel> apostilasRel = _db.Apostila_Kit_Rel
                    .Include(x => x.Apostila)
                    .Where(x => apostilasIds.Contains(x.Apostila_Id))
                    .ToList();

                // Processar os registros / alunos / apostilas
                foreach (UpdateRegistroRequest item in model.Registros) {
                    // Pegar o registro do aluno na aula - Se existir, coloca na variável registro
                    registros.TryGetValue(item.Turma_Aula_Aluno_Id, out var registro);

                    if (registro is null) {
                        continue;
                    }

                    // Se existir, coloca na variável aluno
                    alunos.TryGetValue(item.Turma_Aula_Aluno_Id, out var aluno);

                    if (aluno is null) {
                        continue;
                    }

                    // Iterar sobre a lista de apostilas pré-coletadas
                    Apostila_Kit_Rel? apostilaAbacoRel = apostilasRel.FirstOrDefault(x =>
                        x.Apostila_Id == item.Apostila_Abaco_Id &&
                        x.Apostila_Kit_Id == aluno.Apostila_Kit_Id);

                    Apostila_Kit_Rel? apostilaAhRel = apostilasRel.FirstOrDefault(x =>
                        x.Apostila_Id == item.Apostila_Ah_Id &&
                        x.Apostila_Kit_Id == aluno.Apostila_Kit_Id);

                    if (apostilaAbacoRel is null) {
                        return new ResponseModel { Message = @$"Registro '{item.Turma_Aula_Aluno_Id}' está tentando atualizar a apostila do(a) aluno(a) com um kit Ábaco que ele(a) não possui" };
                    }

                    if (apostilaAhRel is null) {
                        return new ResponseModel { Message = @$"Registro '{item.Turma_Aula_Aluno_Id}' está tentando atualizar a apostila do(a) aluno(a) com um kit AH que ele(a) não possui" };
                    }

                    // Se o número da página passado no request for maior que o número de páginas da apostila
                    if (item.Numero_Pagina_Abaco > apostilaAbacoRel.Apostila.NumeroTotalPaginas) {
                        return new ResponseModel { Message = "Número da página passado na requisição é maior que o número de páginas da apostila" };
                    }

                    if (item.Numero_Pagina_Ah > apostilaAhRel.Apostila.NumeroTotalPaginas) {
                        return new ResponseModel { Message = "Número da página passado na requisição é maior que o número de páginas da apostila" };
                    }

                    registro.Apostila_Abaco_Id = apostilaAbacoRel.Apostila_Id;
                    registro.Apostila_AH_Id = apostilaAhRel.Apostila_Id;

                    registro.NumeroPaginaAbaco = item.Numero_Pagina_Abaco;
                    registro.NumeroPaginaAH = item.Numero_Pagina_Ah;

                    registro.Presente = item.Presente;

                    _db.Aula_Aluno.Update(registro);
                }

                aula.Professor_Id = model.Professor_Id;
                aula.Observacao = model.Observacao;
                aula.Finalizada = true;
                _db.Aula.Update(aula);

                _db.SaveChanges();

                CalendarioList? aulaToReturn = _db.CalendarioList.FirstOrDefault(x => x.Aula_Id == aula.Id);
                List<CalendarioAlunoList> alunosToReturn = _db.CalendarioAlunoList.Where(x => x.Aula_Id == aula.Id).ToList();

                CalendarioResponse calendario = _mapper.Map<CalendarioResponse>(aulaToReturn);
                calendario.Alunos = alunosToReturn;

                response.Message = "Chamada realizada com sucesso";
                response.Object = calendario;
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao realizar a chamada: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel ReagendarAula(ReagendarAulaRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                Aula? aula = _db.Aula.Find(model.Id);

                if (aula is null) {
                    return new ResponseModel { Message = "Aula não encontrada" };
                }

                // Não deve ser possível reagendar uma aula que foi finalizada
                if (aula.Finalizada == true) {
                    return new ResponseModel { Message = "Não é possível reagendar uma aula finalizada" };
                }

                if (model.Data <= TimeFunctions.HoraAtualBR()) {
                    return new ResponseModel { Message = "Não é possível reagendar uma aula para um horário que já passou" };
                }

                bool professorHasTurmaConflict = _professorService.HasTurmaTimeConflict(
                    model.Professor_Id,
                    ( int )model.Data.DayOfWeek,
                    model.Data.TimeOfDay,
                    IgnoredTurmaId: aula.Turma_Id);

                if (professorHasTurmaConflict) {
                    return new ResponseModel { Message = "O professor já tem uma turma nesse horário" };
                }

                bool professorHasAulaConflict = _professorService.HasAulaTimeConflict(
                    model.Professor_Id,
                    model.Data,
                    IgnoredAulaId: aula.Id);

                if (professorHasAulaConflict) {
                    return new ResponseModel { Message = "O professor já tem uma aula nesse horário" };
                }

                // Validations passed

                // Desativar a aula que foi reposta e adicionar a nova aula
                aula.Deactivated = TimeFunctions.HoraAtualBR();
                _db.Aula.Update(aula);

                Aula aulaReagendada = new() {
                    Data = model.Data,
                    Professor_Id = model.Professor_Id,
                    Observacao = model.Observacao ?? aula.Observacao ?? "",
                    ReposicaoDe_Aula_Id = aula.Id,
                    Descricao = aula.Descricao ?? "",
                    Finalizada = false,

                    Sala_Id = aula.Sala_Id,
                    Created = aula.Created,
                    Turma_Id = aula.Turma_Id,
                    LastUpdated = TimeFunctions.HoraAtualBR(),
                };

                _db.Aula.Add(aulaReagendada);
                _db.SaveChanges();

                List<Aula_Aluno> registrosOriginais = _db.Aula_Aluno.Where(a => a.Aula_Id == aula.Id).ToList();

                // Mover os registros dos alunos para a nova aula e remover os antigos
                foreach (Aula_Aluno registro in registrosOriginais) {
                    Aula_Aluno registroReagendado = new() {
                        Aula_Id = aulaReagendada.Id,

                        Aluno_Id = registro.Aluno_Id,

                        Observacao = registro.Observacao,
                        Apostila_Abaco_Id = registro.Apostila_Abaco_Id,
                        NumeroPaginaAbaco = registro.NumeroPaginaAbaco,
                        Apostila_AH_Id = registro.Apostila_AH_Id,
                        NumeroPaginaAH = registro.NumeroPaginaAH,

                        ReposicaoDe_Aula_Id = registro.ReposicaoDe_Aula_Id,
                    };

                    _db.Aula_Aluno.Add(registroReagendado);
                }

                _db.SaveChanges();

                _db.Aula_Aluno.RemoveRange(registrosOriginais);
                _db.SaveChanges();

                response.Success = true;
                response.OldObject = _db.CalendarioList.FirstOrDefault(a => a.Aula_Id == model.Id);
                response.Object = _db.CalendarioList.FirstOrDefault(a => a.Aula_Id == aulaReagendada.Id);
                response.Message = "Reagendamento realizado com sucesso";
            } catch (Exception ex) {
                response.Message = "Falha ao reagendar aula: " + ex.ToString();
            }

            return response;
        }
    }
}
