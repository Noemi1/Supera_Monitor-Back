using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aula;
using Supera_Monitor_Back.Models.Turma;
using Supera_Monitor_Back.Services.Email;
using Supera_Monitor_Back.Services.Email.Models;

namespace Supera_Monitor_Back.Services {
    public interface IAulaService {
        CalendarioResponse Get(int aulaId);
        ResponseModel Insert(CreateAulaRequest model);
        ResponseModel Update(UpdateAulaRequest model);
        ResponseModel Delete(int aulaId);

        List<CalendarioResponse> GetAll();
        List<CalendarioResponse> GetAllByTurmaId(int turmaId);
        List<CalendarioResponse> GetAllByProfessorId(int professorId);

        List<CalendarioResponse> Calendario(CalendarioRequest request);

        ResponseModel RegisterChamada(RegisterChamadaRequest model);
        ResponseModel ReagendarAula(ReagendarAulaRequest model);
    }

    public class AulaService : IAulaService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;
        private readonly IProfessorService _professorService;
        private readonly IEmailService _emailService;

        public AulaService(DataContext db, IMapper mapper, IProfessorService professorService, IEmailService emailService)
        {
            _db = db;
            _mapper = mapper;
            _professorService = professorService;
            _emailService = emailService;
        }

        public CalendarioResponse Get(int aulaId)
        {
            CalendarioList? aula = _db.CalendarioList.FirstOrDefault(a => a.Aula_Id == aulaId);

            if (aula == null) {
                throw new Exception("Aula não encontrada");
            }

            CalendarioResponse agendamento = _mapper.Map<CalendarioResponse>(aula);

            List<Aula_PerfilCognitivo_Rel> aulaPerfisCognitivos = _db.Aula_PerfilCognitivo_Rel
                .Where(p => p.Aula_Id == agendamento.Aula_Id)
                .Include(p => p.PerfilCognitivo)
                .ToList();

            List<PerfilCognitivo> perfisCognitivos = aulaPerfisCognitivos.Select(p => p.PerfilCognitivo).ToList();

            agendamento.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfisCognitivos);

            return agendamento;
        }

        public List<CalendarioResponse> GetAll()
        {
            List<CalendarioList> aulas = _db.CalendarioList.ToList();

            List<CalendarioResponse> agendamentos = new();

            foreach (CalendarioList aula in aulas) {
                CalendarioResponse agendamento = _mapper.Map<CalendarioResponse>(aula);

                List<Aula_PerfilCognitivo_Rel> aulaPerfisCognitivos = _db.Aula_PerfilCognitivo_Rel
                    .Where(p => p.Aula_Id == agendamento.Aula_Id)
                    .Include(p => p.PerfilCognitivo)
                    .ToList();

                List<PerfilCognitivo> perfisCognitivos = aulaPerfisCognitivos.Select(p => p.PerfilCognitivo).ToList();

                agendamento.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfisCognitivos);

                agendamentos.Add(agendamento);
            }

            return agendamentos;
        }

        public List<CalendarioResponse> GetAllByTurmaId(int turmaId)
        {
            List<CalendarioList> aulas = _db.CalendarioList.Where(a => a.Turma_Id == turmaId).ToList();

            List<CalendarioResponse> agendamentos = new();

            foreach (CalendarioList aula in aulas) {
                CalendarioResponse agendamento = _mapper.Map<CalendarioResponse>(aula);

                List<Aula_PerfilCognitivo_Rel> aulaPerfisCognitivos = _db.Aula_PerfilCognitivo_Rel
                    .Where(p => p.Aula_Id == agendamento.Aula_Id)
                    .Include(p => p.PerfilCognitivo)
                    .ToList();

                List<PerfilCognitivo> perfisCognitivos = aulaPerfisCognitivos.Select(p => p.PerfilCognitivo).ToList();

                agendamento.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfisCognitivos);

                agendamentos.Add(agendamento);
            }

            return agendamentos;
        }

        public List<CalendarioResponse> GetAllByProfessorId(int professorId)
        {
            List<CalendarioList> aulas = _db.CalendarioList.Where(a => a.Professor_Id == professorId).ToList();

            List<CalendarioResponse> agendamentos = new();

            foreach (CalendarioList aula in aulas) {
                CalendarioResponse agendamento = _mapper.Map<CalendarioResponse>(aula);

                List<Aula_PerfilCognitivo_Rel> aulaPerfisCognitivos = _db.Aula_PerfilCognitivo_Rel
                    .Where(p => p.Aula_Id == agendamento.Aula_Id)
                    .Include(p => p.PerfilCognitivo)
                    .ToList();

                List<PerfilCognitivo> perfisCognitivos = aulaPerfisCognitivos.Select(p => p.PerfilCognitivo).ToList();

                agendamento.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfisCognitivos);

                agendamentos.Add(agendamento);
            }

            return agendamentos;
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

                // Não devo poder registrar uma aula em uma sala que não existe
                bool salaExists = _db.Sala.Any(s => s.Id == model.Sala_Id);

                if (!salaExists) {
                    return new ResponseModel { Message = "Sala não encontrada" };
                }

                // Não devo poder criar turma com um roteiro que não existe
                bool roteiroExists = _db.Roteiro.Any(r => r.Id == model.Roteiro_Id);

                if (!roteiroExists) {
                    return new ResponseModel { Message = "Roteiro não encontrado" };
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

                // Validations passed

                Aula aula = new() {
                    Data = model.Data,
                    Observacao = model.Observacao,
                    Sala_Id = model.Sala_Id,
                    Professor_Id = model.Professor_Id,
                    Turma_Id = model.Turma_Id,
                    Created = TimeFunctions.HoraAtualBR(),
                    Descricao = model.Descricao ?? turma?.Nome ?? "Aula independente",
                    Roteiro_Id = model.Roteiro_Id,
                    Finalizada = false,
                };

                _db.Aula.Add(aula);
                _db.SaveChanges();

                // Inserir os registros dos alunos originais na aula recém criada
                // Se for uma aula sem Turma, então essa lista é vazia, e por padrão não será inserido nenhum registro de aluno
                List<Aluno> alunos = _db.Aluno.Where(a =>
                    a.Turma_Id == aula.Turma_Id &&
                    a.Deactivated == null)
                .ToList();

                foreach (Aluno aluno in alunos) {
                    Aula_Aluno registro = new() {
                        Aula_Id = aula.Id,
                        Aluno_Id = aluno.Id,
                        Presente = null,
                    };

                    _db.Aula_Aluno.Add(registro);
                }

                _db.SaveChanges();

                // Pegar os perfis cognitivos passados no request e criar as entidades de Aula_PerfilCognitivo
                foreach (var perfil in model.PerfilCognitivo) {
                    Aula_PerfilCognitivo_Rel newPerfilCognitivoRel = new() {
                        Aula_Id = aula.Id,
                        PerfilCognitivo_Id = perfil.Id,
                    };

                    _db.Aula_PerfilCognitivo_Rel.Add(newPerfilCognitivoRel);
                }

                _db.SaveChanges();

                var calendarioList = _db.CalendarioList.FirstOrDefault(a => a.Aula_Id == aula.Id);
                var calendarioAlunos = _db.CalendarioAlunoList.Where(a => a.Aula_Id == aula.Id).ToList();

                CalendarioResponse calendarioResponse = _mapper.Map<CalendarioResponse>(calendarioList);
                calendarioResponse.Alunos = calendarioAlunos;
                calendarioResponse.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(model.PerfilCognitivo);

                response.Message = "Aula registrada com sucesso";
                response.Object = calendarioResponse;
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

                // Não devo poder atualizar turma com um roteiro que não existe
                bool roteiroExists = _db.Roteiro.Any(r => r.Id == model.Roteiro_Id);

                if (!roteiroExists) {
                    return new ResponseModel { Message = "Roteiro não encontrado" };
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

                aula.Roteiro_Id = model.Roteiro_Id;
                aula.Sala_Id = model.Sala_Id;
                aula.Professor_Id = model.Professor_Id;
                aula.Observacao = model.Observacao;
                aula.Descricao = model.Descricao ?? aula.Descricao ?? "";
                aula.LastUpdated = TimeFunctions.HoraAtualBR();

                _db.Aula.Update(aula);
                _db.SaveChanges();

                // Por simplicidade, remover os perfis cognitivos anteriores
                List<Aula_PerfilCognitivo_Rel> perfisToRemove = _db.Aula_PerfilCognitivo_Rel
                    .Where(p => p.Aula_Id == aula.Id)
                    .ToList();

                _db.RemoveRange(perfisToRemove);
                _db.SaveChanges();

                // Pegar os perfis cognitivos passados no request e criar as entidades de Aula_PerfilCognitivo
                foreach (var perfil in model.PerfilCognitivo) {
                    Aula_PerfilCognitivo_Rel newPerfilCognitivoRel = new() {
                        Aula_Id = aula.Id,
                        PerfilCognitivo_Id = perfil.Id,
                    };

                    _db.Aula_PerfilCognitivo_Rel.Add(newPerfilCognitivoRel);
                }

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
                    a.Data.Date >= request.IntervaloDe.Value.Date &&
                    a.Data.Date <= request.IntervaloAte.Value.Date)
                .Include(a => a.Aula_Aluno)
                .Include(a => a.Turma)
                .Include(a => a.Professor)
                .Include(a => a.Professor.Account)
                .Include(a => a.Sala)
                .Include(a => a.Roteiro)
                .ToList();

            List<Turma> turmas = _db.Turma
                .Where(t => t.Deactivated == null)
                .Include(t => t.Professor)
                .Include(t => t.Sala)
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

                    Roteiro_Id = aula.Roteiro_Id,
                    Semana = aula.Roteiro.Semana,
                    Tema = aula.Roteiro.Tema,

                    Data = aula.Data,

                    Turma_Id = aula.Turma_Id,
                    Turma = aula.Turma is null ? "" : aula.Turma.Nome,

                    Professor_Id = aula.Professor_Id,
                    Professor = aula.Professor.Account.Name ?? "Professor indefinido",
                    CorLegenda = aula.Professor.CorLegenda ?? "#000",
                    Finalizada = aula.Finalizada,
                    ReposicaoDe_Aula_Id = aula.ReposicaoDe_Aula_Id,
                    Deactivated = aula.Deactivated,

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
                    .OrderBy(a => a.Aluno)
                    .ToList();

                List<Aula_PerfilCognitivo_Rel> aulaPerfisCognitivos = _db.Aula_PerfilCognitivo_Rel
                    .Where(p => p.Aula_Id == agendamento.Aula_Id)
                    .Include(p => p.PerfilCognitivo)
                    .ToList();

                List<PerfilCognitivo> perfisCognitivos = aulaPerfisCognitivos
                    .Select(p => p.PerfilCognitivo)
                    .ToList();

                agendamento.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfisCognitivos);

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

                        Roteiro_Id = -1,
                        Semana = null,
                        Tema = null,

                        Data = new DateTime(data.Year, data.Month, data.Day, turma.Horario!.Value.Hours, turma.Horario!.Value.Minutes, turma.Horario!.Value.Seconds),

                        Turma_Id = turma.Id,
                        Turma = turma.Nome,

                        Professor_Id = turma.Professor_Id ?? -1,
                        Professor = turma.Professor is not null ? turma.Professor.Account.Name : "Professor indefinido",

                        CorLegenda = turma.Professor is not null ? turma.Professor.CorLegenda : "#000",

                        CapacidadeMaximaAlunos = turma.CapacidadeMaximaAlunos,

                        Finalizada = false,

                        Sala_Id = turma.Sala?.Id ?? -1,
                        NumeroSala = turma.Sala?.NumeroSala,
                        Andar = turma.Sala?.Andar,
                        Observacao = "",

                    };

                    CalendarioResponse agendamento = _mapper.Map<CalendarioResponse>(calendarioList);

                    // Na pseudo-aula, adicionar só os alunos da turma original
                    List<AlunoList> alunos = _db.AlunoList
                        .Where(a => a.Turma_Id == turma.Id)
                        .OrderBy(a => a.Nome)
                        .ToList();

                    agendamento.Alunos = _mapper.Map<List<CalendarioAlunoList>>(alunos);

                    List<Turma_PerfilCognitivo_Rel> turmaPerfisCognitivos = _db.Turma_PerfilCognitivo_Rel
                        .Where(p => p.Turma_Id == agendamento.Turma_Id)
                        .Include(p => p.PerfilCognitivo)
                        .ToList();

                    List<PerfilCognitivo> perfisCognitivos = turmaPerfisCognitivos
                        .Select(p => p.PerfilCognitivo)
                        .ToList();

                    agendamento.PerfilCognitivo = _mapper.Map<List<PerfilCognitivoModel>>(perfisCognitivos);

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

                // Não devo poder realizar a chamada em uma aula que está finalizada
                //if (aula.Finalizada) {
                //    return new ResponseModel { Message = "Aula já está finalizada" };
                //}

                Professor? professor = _db.Professor.Find(model.Professor_Id);

                if (professor is null) {
                    return new ResponseModel { Message = "Professor não encontrado" };
                }

                // Validations passed

                // Buscar registros / alunos / apostilas previamente para reduzir o número de requisições ao banco
                Dictionary<int, Aula_Aluno> registros = _db.Aula_Aluno
                    .Where(x => model.Registros.Select(r => r.Aula_Aluno_Id).Contains(x.Id))
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
                    registros.TryGetValue(item.Aula_Aluno_Id, out var registro);

                    if (registro is null) {
                        continue;
                    }

                    // Se o registro existir, pega o aluno baseado no Aluno_Id do registro e coloca na variável aluno
                    alunos.TryGetValue(registro.Aluno_Id, out var aluno);

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
                        return new ResponseModel { Message = $"Registro '{item.Aula_Aluno_Id}' está tentando atualizar a apostila do(a) aluno(a) com um kit Ábaco que ele(a) não possui" };
                    }

                    if (apostilaAhRel is null) {
                        return new ResponseModel { Message = $"Registro '{item.Aula_Aluno_Id}' está tentando atualizar a apostila do(a) aluno(a) com um kit AH que ele(a) não possui" };
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
                    registro.Observacao = item.Observacao;

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

                // Se essa aula já foi reagendada, tem status Deactivated e não é possível reagendar novamente
                if (aula.Deactivated.HasValue) {
                    return new ResponseModel { Message = "Não foi possível reagendar esta aula, pois já possui um reagendamento marcado." };
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

                // Replicar os perfis cognitivos para a aula reagendada, sem remover os originais
                List<Aula_PerfilCognitivo_Rel> aulaPerfisCognitivos = _db.Aula_PerfilCognitivo_Rel
                    .Where(p => p.Aula_Id == aula.Id)
                    .ToList();

                List<Aula_PerfilCognitivo_Rel> aulaReagendadaPerfisCognitivos = aulaPerfisCognitivos
                    .Select(p => new Aula_PerfilCognitivo_Rel() {
                        Aula_Id = aulaReagendada.Id,
                        PerfilCognitivo_Id = p.PerfilCognitivo_Id,
                    }).ToList();

                _db.Aula_PerfilCognitivo_Rel.AddRange(aulaReagendadaPerfisCognitivos);
                _db.SaveChanges();

                List<Aula_Aluno> registrosOriginais = _db.Aula_Aluno
                    .Where(a => a.Aula_Id == aula.Id)
                    .ToList();

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
                        Deactivated = registro.Deactivated,
                    };

                    _db.Aula_Aluno.Add(registroReagendado);
                }

                _db.SaveChanges();

                _db.Aula_Aluno.RemoveRange(registrosOriginais);
                _db.SaveChanges();

                // Enviar, de forma assíncrona, e-mail aos interessados:
                // 1. O professor responsável pela aula
                // 2. Os alunos que estavam registrados na aula original e foram relocados juntamente com a aula, cujo registro não estava Deactivated
                Professor? professor = _db.Professor
                    .Include(p => p.Account)
                    .FirstOrDefault(p => p.Id == model.Professor_Id);

                Turma? turma = _db.Turma
                    .FirstOrDefault(t => t.Id == aula.Turma_Id);

                if (professor is not null) {
                    // TODO: Em produção, alterar o destinatário do e-mail

                    //_emailService.SendEmail(
                    //    templateType: "ReagendarAula",
                    //    model: new ReagendarAulaModel {
                    //        Name = professor.Account.Name ?? "Nome indefinido",
                    //        OldDate = aula.Data,
                    //        NewDate = aulaReagendada.Data,
                    //        IsProfessor = true,
                    //    },
                    //    to: professor.Account.Email
                    //);

                    _emailService.SendEmail(
                        templateType: "ReagendarAula",
                        model: new ReagendarAulaModel {
                            Name = professor.Account.Name ?? "Nome indefinido",
                            TurmaName = turma is not null ? turma.Nome : "Aula Independente",
                            OldDate = aula.Data,
                            NewDate = aulaReagendada.Data,
                        },
                        to: "lgalax1y@gmail.com"
                    );

                    _emailService.SendEmail(
                        templateType: "ReagendarAula",
                        model: new ReagendarAulaModel {
                            Name = professor.Account.Name ?? "Nome indefinido",
                            TurmaName = turma is not null ? turma.Nome : "Aula Independente",
                            OldDate = aula.Data,
                            NewDate = aulaReagendada.Data,
                        },
                        to: "noemi@bullest.com.br"
                    );
                }

                List<int> listAlunoIds = registrosOriginais
                    .Where(r => r.Deactivated == null)
                    .Select(r => r.Aluno_Id)
                    .ToList();

                // Por algum motivo Aluno não tá aparecendo relacionado com Pessoa pra dar include
                List<int> listPessoaIds = _db.Aluno
                    .Where(a => listAlunoIds.Contains(a.Id))
                    .Select(a => a.Pessoa_Id)
                    .ToList();

                List<Pessoa> listPessoas = _db.Pessoa
                    .Where(p => listPessoaIds.Contains(p.Id))
                    .ToList();

                foreach (Pessoa pessoa in listPessoas) {
                    if (pessoa?.Email is not null) {
                        // TODO: Em produção, alterar o destinatário do e-mail

                        //_emailService.SendEmail(
                        //    templateType: "ReagendarAula",
                        //    model: new ReagendarAulaModel { NewDate = aulaReagendada.Data },
                        //    to: pessoa.Email
                        //);

                        _emailService.SendEmail(
                            templateType: "ReagendarAula",
                            model: new ReagendarAulaModel {
                                Name = pessoa?.Nome ?? "Nome indefinido",
                                TurmaName = turma is not null ? turma.Nome : "Aula Independente",
                                OldDate = aula.Data,
                                NewDate = aulaReagendada.Data,
                            },
                            to: "noemi@bullest.com.br"
                        );

                        _emailService.SendEmail(
                            templateType: "ReagendarAula",
                            model: new ReagendarAulaModel {
                                Name = pessoa?.Nome ?? "Nome indefinido",
                                TurmaName = turma is not null ? turma.Nome : "Aula Independente",
                                OldDate = aula.Data,
                                NewDate = aulaReagendada.Data,
                            },
                            to: "lgalax1y@gmail.com"
                        );
                    }
                }

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
