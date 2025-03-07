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

                bool professorHasAulaConflict = _professorService.HasAulaTimeConflict(
                    model.Professor_Id,
                    model.Data,
                    IgnoredAulaId: null);

                if (professorHasAulaConflict) {
                    return new ResponseModel { Message = "O professor já tem uma aula nesse horário" };
                }

                if (professorHasTurmaConflict) {
                    return new ResponseModel { Message = "O professor já tem uma turma nesse horário" };
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

                    bool professorHasAulaConflict = _professorService.HasAulaTimeConflict(
                        model.Professor_Id,
                        aula.Data,
                        IgnoredAulaId: aula.Id);

                    if (professorHasAulaConflict) {
                        return new ResponseModel { Message = "O professor já tem uma aula nesse horário" };
                    }

                    if (professorHasTurmaConflict) {
                        return new ResponseModel { Message = "O professor já tem uma turma nesse horário" };
                    }
                }

                // Validations passed

                response.OldObject = _db.CalendarioList.FirstOrDefault(a => a.Aula_Id == model.Id);

                aula.Sala_Id = model.Sala_Id;
                aula.Professor_Id = model.Professor_Id;
                aula.Observacao = model.Observacao;
                aula.Descricao = string.IsNullOrEmpty(model.Descricao) ? aula.Descricao : model.Descricao;
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
                    a.Data >= request.IntervaloDe &&
                    a.Data <= request.IntervaloAte)
                .ToList();

            List<Aula> aulasIndependentes = _db.Aula
                .Where(a =>
                    a.Deactivated == null &&
                    a.Data >= request.IntervaloDe &&
                    a.Data <= request.IntervaloAte &&
                    a.Turma_Id == null)
                .ToList();

            List<Turma> turmas = _db.Turma
                .Where(t => t.Deactivated == null)
                .ToList();

            List<Professor> professores = _db.Professor
                .Include(p => p.Account)
                .Where(p => p.Account.Deactivated == null)
                .ToList();

            //Aplicar filtros
            // Filtro de Turma
            if (request.Turma_Id.HasValue) {
                turmas = turmas.Where(x => x.Id == request.Turma_Id.Value).ToList();
                aulas = aulas.Where(x => x.Turma_Id == request.Turma_Id.Value).ToList();
                aulasIndependentes = aulasIndependentes.Where(x => x.Turma_Id == request.Turma_Id.Value).ToList();
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
                aulas = aulas.Where(a => a.Turma_Id.HasValue && turmaIds.Contains(a.Turma_Id.Value)).ToList();

                // Como aulas independentes não pertencem a turmas, todas devem ser removidas
                aulasIndependentes.Clear();
            }

            // Filtro de Professor
            if (request.Professor_Id.HasValue) {
                turmas = turmas.Where(x => x.Professor_Id == request.Professor_Id.Value).ToList();
                aulas = aulas.Where(x => x.Professor_Id == request.Professor_Id.Value).ToList();
                aulasIndependentes = aulasIndependentes.Where(x => x.Professor_Id == request.Professor_Id.Value).ToList();
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
                    aulas = aulas.Where(x => x.Turma_Id == aluno.Turma_Id).ToList();

                    // Buscar IDs das aulas independentes em que o aluno está cadastrado
                    List<int> aulasIndependentesIds = _db.Aula_Aluno
                        .Where(x => x.Aluno_Id == alunoId)
                        .Select(x => x.Aula_Id)
                        .ToList();

                    // Filtrar apenas as aulas independentes que o aluno se cadastrou
                    aulasIndependentes = aulasIndependentes.Where(x => aulasIndependentesIds.Contains(x.Id)).ToList();
                }
            }

            List<CalendarioList> agendamentos = new();

            DateTime data = request.IntervaloDe.Value;
            while (data < request.IntervaloAte) {
                List<Turma> turmasDoDia = turmas.Where(t => t.DiaSemana == ( int )data.DayOfWeek).ToList();

                // Agrupar tanto aulas instanciadas quanto pseudo-aulas das turmas que existem
                foreach (Turma turma in turmasDoDia) {
                    Aula? aula = aulas.FirstOrDefault(a =>
                        ( int )a.Data.DayOfWeek == turma.DiaSemana &&
                        a.Data.TimeOfDay == turma.Horario);

                    Sala? sala = _db.Sala.FirstOrDefault(s => s.Id == turma.Sala_Id);

                    CalendarioList agendamento = new();

                    Professor? associatedProfessor = professores.FirstOrDefault(p => p.Id == turma.Professor_Id);

                    // Se a aula não existir, é uma pseudo-aula
                    if (aula is null) {
                        agendamento.Aula_Id = -1;
                        agendamento.Data = new DateTime(data.Year, data.Month, data.Day, turma.Horario!.Value.Hours, turma.Horario!.Value.Minutes, turma.Horario!.Value.Seconds);
                        agendamento.Turma_Id = turma.Id;
                        agendamento.Turma = turma.Nome;
                        agendamento.CapacidadeMaximaAlunos = turma.CapacidadeMaximaAlunos;

                        agendamento.Professor_Id = turma.Professor_Id ?? -1;
                        agendamento.Professor = associatedProfessor?.Account.Name ?? "Professor indefinido";
                        agendamento.CorLegenda = associatedProfessor?.CorLegenda ?? "#000";

                        agendamento.Sala_Id = turma.Sala_Id;
                        agendamento.NumeroSala = sala?.NumeroSala;
                        agendamento.Andar = sala?.Andar;

                        agendamento.Observacao = "";
                    }

                    // Se a aula existir, é uma aula instanciada
                    if (aula is not null) {
                        agendamento.Aula_Id = aula.Id;
                        agendamento.Data = aula.Data;
                        agendamento.Turma_Id = turma.Id;
                        agendamento.Turma = turma.Nome;
                        agendamento.CapacidadeMaximaAlunos = turma.CapacidadeMaximaAlunos;

                        agendamento.Professor_Id = aula.Professor_Id;
                        agendamento.Professor = associatedProfessor?.Account.Name ?? "Professor Indefinido";
                        agendamento.CorLegenda = associatedProfessor?.CorLegenda ?? "#000";
                        agendamento.Sala_Id = sala?.Id;
                        agendamento.NumeroSala = sala?.NumeroSala;
                        agendamento.Andar = sala?.Andar;

                        agendamento.Observacao = aula.Observacao;
                    }

                    agendamentos.Add(agendamento);
                }

                data = data.AddDays(1);
            }

            // Por fim, adicionar agendamentos de aulas que são independentes
            foreach (Aula aula in aulasIndependentes) {
                Professor? associatedProfessor = professores.FirstOrDefault(p => p.Id == aula.Professor_Id);

                CalendarioList agendamento = new() {
                    Aula_Id = aula.Id,
                    Data = aula.Data,
                    Turma_Id = -1,
                    Turma = "Aula Independente",
                    CapacidadeMaximaAlunos = 99,

                    Professor_Id = aula.Professor_Id,
                    Professor = associatedProfessor?.Account.Name ?? "Professor Indefinido",
                    CorLegenda = associatedProfessor?.CorLegenda ?? "#000",

                    Sala_Id = aula.Sala_Id,
                    Observacao = aula.Observacao,
                    Finalizada = aula.Finalizada
                };

                agendamentos.Add(agendamento);
            }

            List<CalendarioResponse> calendario = new();

            // Iterar sobre todos os agendamentos, adicionando os alunos a eles e adaptando à CalendarioResponse para retornar
            foreach (CalendarioList agendamento in agendamentos) {
                CalendarioResponse aula = _mapper.Map<CalendarioResponse>(agendamento);

                // Se Aula_Id === -1, é uma pseudo-aula que contém uma turma, então buscar os alunos com base no Id da turma
                if (aula.Aula_Id == -1) {
                    aula.Alunos = _db.CalendarioAlunoList.Where(a => a.Turma_Id == aula.Turma_Id).ToList();
                }
                // Senão, não é uma pseudo-aula, então buscar os alunos com base no Id da aula
                else {
                    aula.Alunos = _db.CalendarioAlunoList.Where(a => a.Aula_Id == aula.Aula_Id).ToList();
                }

                calendario.Add(aula);
            }

            return calendario;
        }

        //public List<CalendarioResponse> Calendario(CalendarioRequest request)
        //{
        //    DateTime now = TimeFunctions.HoraAtualBR();

        //    // Se não passar data inicio, considera a segunda-feira da semana atual
        //    if (!request.IntervaloDe.HasValue) {
        //        // Retorna para o início da semana (domingo) e adiciona um dia para obter segunda-feira
        //        request.IntervaloDe = now.AddDays(-( int )now.DayOfWeek);
        //        request.IntervaloDe = request.IntervaloDe.Value.AddDays(1);
        //    }

        //    // Se não passar data fim, considera o sábado da semana da data inicio
        //    if (!request.IntervaloAte.HasValue) {
        //        // Retorna para o início da semana (domingo) e adiciona seis dias para obter sábado
        //        request.IntervaloAte = request.IntervaloDe.Value.AddDays(-( int )request.IntervaloDe.Value.DayOfWeek);
        //        request.IntervaloAte = request.IntervaloDe.Value.AddDays(6);
        //    }

        //    // Listar turmas com horários já definidos
        //    List<Turma> turmas = _db.Turma
        //        .Where(
        //            x => x.Horario != null &&
        //            x.Deactivated == null) // Não exibir aulas de turmas inativas
        //        .ToList();

        //    // Filtro de Turma
        //    if (request.Turma_Id.HasValue) {
        //        turmas = turmas.Where(x => x.Id == request.Turma_Id.Value).ToList();
        //    }

        //    // Filtro de Turma Tipo
        //    //if (request.Turma_Tipo_Id.HasValue) {
        //    //    turmas = turmas.Where(x => x.Turma_Tipo_Id == request.Turma_Tipo_Id.Value).ToList();
        //    //}

        //    // Filtro de Professor
        //    if (request.Professor_Id.HasValue) {
        //        turmas = turmas.Where(x => x.Professor_Id == request.Professor_Id.Value).ToList();
        //    }

        //    // Filtro de Aluno
        //    if (request.Aluno_Id.HasValue) {
        //        AlunoList aluno = _db.AlunoList.FirstOrDefault(x => x.Id == request.Aluno_Id.Value)!;
        //        turmas = turmas.Where(x => x.Id == aluno.Turma_Id).ToList();
        //    }

        //    DateTime data = request.IntervaloDe.Value;
        //    List<CalendarioResponse> list = new();

        //    // Adiciona no calendario cada item do dia do intervalo
        //    do {
        //        // Coleta as turmas que tem aula no mesmo dia da semana que a data de referência
        //        List<Turma> turmasDoDia = turmas.Where(x => x.DiaSemana == ( int )data.DayOfWeek).ToList();

        //        foreach (TurmaList turma in turmasDoDia) {

        //            // Seleciona a aula daquela turma com o mesmo dia e o mesmo horário
        //            CalendarioList? aula = _db.CalendarioList.FirstOrDefault(x => x.Turma_Id == turma.Id
        //                                            && x.Data.TimeOfDay == turma.Horario!.Value
        //                                            && x.Data.Date == data.Date);

        //            List<CalendarioAlunoList> alunos = new() { };

        //            // Se a aula não estiver cadastrada ainda, retorna uma lista de alunos originalmente cadastrados na turma
        //            // Senão, a aula já existe, a lista de alunos será composta pelos alunos da turma + alunos de reposição  
        //            if (aula == null) {
        //                ProfessorList? professor = _db.ProfessorList.FirstOrDefault(p => p.Id == turma.Professor_Id);

        //                // Não exibir aulas de professores inativos (porém, > exibir professores nulos <)
        //                if (professor?.Active == false) {
        //                    continue;
        //                }

        //                var horario = turma.Horario!.Value;
        //                aula = new CalendarioList {
        //                    Aula_Id = -1,
        //                    Data = new DateTime(data.Year, data.Month, data.Day, horario.Hours, horario.Minutes, horario.Seconds),
        //                    Turma_Id = turma.Id,
        //                    Turma = turma.Nome,
        //                    CapacidadeMaximaAlunos = turma.CapacidadeMaximaAlunos,
        //                    Professor_Id = turma.Professor_Id.Value,
        //                    Professor = turma.Professor ?? "Professor Indefinido",
        //                    CorLegenda = turma.CorLegenda ?? "#000",
        //                    Observacao = "",
        //                    //Turma_Tipo_Id = ( int )turma.Turma_Tipo_Id,
        //                    //Turma_Tipo = turma.Turma_Tipo
        //                };

        //                alunos = _db.AlunoList.Where(
        //                    x => x.Turma_Id == turma.Id &&
        //                    (request.Aluno_Id.HasValue ? request.Aluno_Id.Value == x.Id : true) &&
        //                    x.Deactivated == null) // Não exibir alunos inativos na pseudo-aula
        //                    .ToList()
        //                    .Select(a => _mapper.Map<CalendarioAlunoList>(a))
        //                    .OrderBy(a => a.Aluno)
        //                    .ToList();
        //            } else {
        //                alunos = _db.CalendarioAlunoList
        //                    .Where(x =>
        //                        x.Aula_Id == aula.Aula_Id &&
        //                        _db.AlunoList.Any(a => a.Id == x.Aluno_Id && a.Deactivated == null)) // Não exibir alunos inativos na lista de CalendarioAlunoList
        //                    .OrderBy(a => a.Aluno)
        //                    .ToList();
        //            }

        //            CalendarioResponse calendario = _mapper.Map<CalendarioResponse>(aula);
        //            calendario.Alunos = alunos;

        //            list.Add(calendario);
        //        }

        //        data = data.AddDays(1);
        //    } while (data < request.IntervaloAte);

        //    return list;
        //}

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

                bool professorHasAulaConflict = _professorService.HasAulaTimeConflict(
                    model.Professor_Id,
                    model.Data,
                    IgnoredAulaId: aula.Id);

                if (professorHasAulaConflict) {
                    return new ResponseModel { Message = "O professor já tem uma aula nesse horário" };
                }

                if (professorHasTurmaConflict) {
                    return new ResponseModel { Message = "O professor já tem uma turma nesse horário" };
                }

                // Validations passed

                // Desativar a aula que foi reposta e adicionar a nova aula
                aula.Deactivated = TimeFunctions.HoraAtualBR();
                _db.Aula.Update(aula);

                Aula aulaReagendada = new() {
                    Data = model.Data,
                    Professor_Id = model.Professor_Id,
                    Observacao = string.IsNullOrEmpty(model.Observacao) ? aula.Observacao : model.Observacao,
                    ReposicaoDe_Aula_Id = aula.Id,

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
