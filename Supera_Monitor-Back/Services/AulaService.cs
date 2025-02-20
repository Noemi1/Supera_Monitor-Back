using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aula;

namespace Supera_Monitor_Back.Services {
    public interface IAulaService {
        AulaList Get(int aulaId);
        ResponseModel Insert(CreateAulaRequest model);
        ResponseModel Update(UpdateAulaRequest model);
        ResponseModel Delete(int aulaId);

        List<AulaList> GetAll();
        List<AulaList> GetAllByTurmaId(int turmaId);
        List<AulaList> GetAllByProfessorId(int professorId);

        List<CalendarioResponse> Calendario(CalendarioRequest request);

        ResponseModel RegisterChamada(RegisterChamadaRequest model);
    }

    public class AulaService : IAulaService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;

        public AulaService(DataContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public AulaList Get(int aulaId)
        {
            AulaList? aula = _db.AulaList.FirstOrDefault(a => a.Id == aulaId);

            if (aula == null) {
                throw new Exception("Aula não encontrada");
            }

            return aula;
        }

        public List<AulaList> GetAll()
        {
            List<AulaList> aulas = _db.AulaList.ToList();

            return aulas;
        }

        public List<AulaList> GetAllByTurmaId(int turmaId)
        {
            List<AulaList> aulas = _db.AulaList.Where(a => a.Turma_Id == turmaId).ToList();

            return aulas;
        }

        public List<AulaList> GetAllByProfessorId(int professorId)
        {
            List<AulaList> aulas = _db.AulaList.Where(a => a.Professor_Id == professorId).ToList();

            return aulas;
        }

        public ResponseModel Insert(CreateAulaRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                // Não devo poder registrar uma aula em uma turma que não existe
                bool TurmaExists = _db.Turmas.Any(t => t.Id == model.Turma_Id);

                if (!TurmaExists) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                // Não devo poder registrar uma aula com um professor que não existe
                bool ProfessorExists = _db.Professors.Any(p => p.Id == model.Professor_Id);

                if (!ProfessorExists) {
                    return new ResponseModel { Message = "Professor não encontrado" };
                }

                // Validations passed

                TurmaAula aula = new() {
                    Turma_Id = model.Turma_Id,
                    Professor_Id = model.Professor_Id,
                    Data = model.Data,
                    Observacao = model.Observacao,
                };

                _db.TurmaAulas.Add(aula);
                _db.SaveChanges();

                // Inserir os registros dos alunos originais na aula recém criada
                List<AlunoList> alunos = _db.AlunoList.Where(a => a.Turma_Id == aula.Turma_Id).ToList();

                foreach (AlunoList aluno in alunos) {
                    TurmaAulaAluno registro = new() {
                        Turma_Aula_Id = aula.Id,
                        Aluno_Id = aluno.Id,
                        Presente = null,
                    };

                    _db.TurmaAulaAlunos.Add(registro);
                }

                _db.SaveChanges();

                response.Message = "Aula registrada com sucesso";
                response.Object = _db.AulaList.FirstOrDefault(a => a.Id == aula.Id);
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
                TurmaAula? aula = _db.TurmaAulas.Find(model.Id);

                // Não devo poder atualizar uma aula que não existe
                if (aula == null) {
                    return new ResponseModel { Message = "Aula não encontrada" };
                }

                // Não devo poder atualizar turma com um professor que não existe
                bool ProfessorExists = _db.Professors.Any(p => p.Id == model.Professor_Id);

                if (!ProfessorExists) {
                    return new ResponseModel { Message = "Este tipo de turma não existe." };
                }

                // Validations passed

                response.OldObject = _db.AulaList.FirstOrDefault(a => a.Id == model.Id);

                aula.Professor_Id = model.Professor_Id;
                aula.Data = model.Data;
                aula.Observacao = model.Observacao;

                _db.TurmaAulas.Update(aula);
                _db.SaveChanges();

                response.Message = "Aula atualizada com sucesso";
                response.Object = aula;
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

            // Listar turmas com horários já definidos
            List<TurmaList> turmas = _db.TurmaList
                .Where(
                    x => x.Horario != null &&
                    x.Deactivated == null) // Não exibir aulas de turmas inativas
                .ToList();

            // Filtro de Turma
            if (request.Turma_Id.HasValue) {
                turmas = turmas.Where(x => x.Id == request.Turma_Id.Value).ToList();
            }

            // Filtro de Turma Tipo
            if (request.Turma_Tipo_Id.HasValue) {
                turmas = turmas.Where(x => x.Turma_Tipo_Id == request.Turma_Tipo_Id.Value).ToList();
            }

            // Filtro de Professor
            if (request.Professor_Id.HasValue) {
                turmas = turmas.Where(x => x.Professor_Id == request.Professor_Id.Value).ToList();
            }

            // Filtro de Aluno
            if (request.Aluno_Id.HasValue) {
                AlunoList aluno = _db.AlunoList.FirstOrDefault(x => x.Id == request.Aluno_Id.Value)!;
                turmas = turmas.Where(x => x.Id == aluno.Turma_Id).ToList();
            }

            DateTime data = request.IntervaloDe.Value;
            List<CalendarioResponse> list = new();

            // Adiciona no calendario cada item do dia do intervalo
            do {
                // Coleta as turmas que tem aula no mesmo dia da semana que a data de referência
                List<TurmaList> turmasDoDia = turmas.Where(x => x.DiaSemana == ( int )data.DayOfWeek).ToList();

                foreach (TurmaList turma in turmasDoDia) {

                    // Seleciona a aula daquela turma com o mesmo dia e o mesmo horário
                    CalendarioList? aula = _db.CalendarioList.FirstOrDefault(x => x.Turma_Id == turma.Id
                                                    && x.Data.TimeOfDay == turma.Horario!.Value
                                                    && x.Data.Date == data.Date);

                    List<CalendarioAlunoList> alunos = new List<CalendarioAlunoList>() { };

                    // Se a aula não estiver cadastrada ainda, retorna uma lista de alunos originalmente cadastrados na turma
                    // Senão, a aula já existe, a lista de alunos será composta pelos alunos da turma + alunos de reposição  
                    if (aula == null) {
                        ProfessorList? professor = _db.ProfessorList.FirstOrDefault(p => p.Id == turma.Professor_Id);

                        // Não exibir aulas de professores inativos (porém, > exibir professores nulos <)
                        if (professor?.Active == false) {
                            continue;
                        }

                        var horario = turma.Horario!.Value;
                        aula = new CalendarioList {
                            Aula_Id = null,
                            Data = new DateTime(data.Year, data.Month, data.Day, horario.Hours, horario.Minutes, horario.Seconds),
                            Turma_Id = turma.Id,
                            Turma = turma.Nome,
                            CapacidadeMaximaAlunos = turma.CapacidadeMaximaAlunos,
                            Professor_Id = ( int )turma.Professor_Id,
                            Professor = turma.Professor ?? "Professor Indefinido",
                            CorLegenda = turma.CorLegenda ?? "#000",
                            Observacao = "",
                            Turma_Tipo_Id = ( int )turma.Turma_Tipo_Id,
                            Turma_Tipo = turma.Turma_Tipo
                        };

                        alunos = _db.AlunoList.Where(
                            x => x.Turma_Id == turma.Id &&
                            (request.Aluno_Id.HasValue ? request.Aluno_Id.Value == x.Id : true) &&
                            x.Deactivated == null) // Não exibir alunos inativos na pseudo-aula
                            .ToList()
                            .Select(a => _mapper.Map<CalendarioAlunoList>(a))
                            .OrderBy(a => a.Aluno)
                            .ToList();
                    } else {
                        alunos = _db.CalendarioAlunoList
                            .Where(x =>
                                x.Aula_Id == aula.Aula_Id &&
                                _db.AlunoList.Any(a => a.Id == x.Aluno_Id && a.Deactivated == null)) // Não exibir alunos inativos na lista de CalendarioAlunoList
                            .OrderBy(a => a.Aluno)
                            .ToList();
                    }

                    CalendarioResponse calendario = _mapper.Map<CalendarioResponse>(aula);
                    calendario.Alunos = alunos;

                    list.Add(calendario);
                }

                data = data.AddDays(1);
            } while (data < request.IntervaloAte);

            return list;
        }

        public ResponseModel RegisterChamada(RegisterChamadaRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                TurmaAula? aula = _db.TurmaAulas.Find(model.Aula_Id);

                // Não devo poder realizar a chamada em uma aula que não existe
                if (aula == null) {
                    return new ResponseModel { Message = "Aula não encontrada" };
                }

                Professor? professor = _db.Professors.Find(model.Professor_Id);

                if (professor is null) {
                    return new ResponseModel { Message = "Professor não encontrado" };
                }

                // Se indicar uma mudança de professor - o professor sendo alocado não pode ter aula no mesmo horário
                if (model.Professor_Id != aula.Professor_Id) {
                    bool ProfessorIsAlreadyOccupied = _db.TurmaAulas.Any(a =>
                        a.Professor_Id == model.Professor_Id &&
                        a.Data == aula.Data
                    );

                    if (ProfessorIsAlreadyOccupied) {
                        return new ResponseModel { Message = "O professor sendo alocado já tem uma aula nesse horário." };
                    }
                }

                // Validations passed

                // Buscar registros / alunos / apostilas previamente para reduzir o número de requisições ao banco
                Dictionary<int, TurmaAulaAluno> registros = _db.TurmaAulaAlunos
                    .Where(x => model.Registros.Select(r => r.Turma_Aula_Aluno_Id).Contains(x.Id))
                    .ToDictionary(x => x.Id);

                Dictionary<int, Aluno> alunos = _db.Alunos
                    .Where(x => registros.Values.Select(r => r.Aluno_Id).Contains(x.Id))
                    .ToDictionary(x => x.Id);

                // Agrupar todos ids de apostilas passados nos registros
                List<int> apostilasIds = model.Registros.SelectMany(r => new[] { r.Apostila_Abaco_Id, r.Apostila_Ah_Id }).Distinct().ToList();

                // Coletar previamente todas as apostilas que contenham qualquer dos ids
                List<Apostila_Kit_Rel> apostilasRel = _db.Apostila_Kit_Rels
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

                    _db.TurmaAulaAlunos.Update(registro);
                }

                aula.Professor_Id = model.Professor_Id;
                aula.Observacao = model.Observacao;
                aula.Finalizada = true;
                _db.TurmaAulas.Update(aula);

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
    }
}
