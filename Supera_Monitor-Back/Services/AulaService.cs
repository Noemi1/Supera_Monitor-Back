using AutoMapper;
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
            // Se não passar data inicio, considera a segunda-feira da semana atual
            if (!request.IntervaloDe.HasValue) {
                request.IntervaloDe = DateTime.Now.AddDays(1 - ( int )DateTime.Now.DayOfWeek);
            }

            // Se não passar data fim, considera o sábado da semana da data inicio
            if (!request.IntervaloAte.HasValue) {
                request.IntervaloAte = request.IntervaloDe.Value.AddDays(6 - ( int )request.IntervaloDe.Value.DayOfWeek);
            }

            // Listar turmas com horários já definidos
            List<TurmaList> turmas = _db.TurmaList.Where(x => x.Horario != null).ToList();

            // Filtro de Turma
            if (request.Turma_Id.HasValue) {
                turmas = turmas.Where(x => x.Id == request.Turma_Id.Value).ToList();
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
                // Coleta as turmas tem aula no mesmo dia da semana que a data de referência
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
                        aula = new CalendarioList {
                            Aula_Id = null,
                            Data = data + turma.Horario!.Value,
                            Turma_Id = turma.Id,
                            Turma = turma.Nome,
                            CapacidadeMaximaAlunos = turma.CapacidadeMaximaAlunos,
                            Professor_Id = turma.Professor_Id,
                            Professor = turma.Professor ?? "Professor Indefinido",
                            CorLegenda = turma.CorLegenda ?? "#000",
                            Observacao = ""

                        };

                        alunos = _db.AlunoList.Where(
                            x => x.Turma_Id == turma.Id &&
                            (request.Aluno_Id.HasValue ? request.Aluno_Id.Value == x.Id : true))
                            .ToList()
                            .Select(x => {
                                return new CalendarioAlunoList() {
                                    Id = null,
                                    Aluno_Id = x.Id,
                                    Nome = x.Nome,
                                    Aluno_Foto = x.Aluno_Foto,
                                    Turma_Id = x.Turma_Id,
                                    Turma = x.Turma,
                                };
                            })
                            .ToList();
                    } else {
                        alunos = _db.CalendarioAlunoList.Where(x => x.Turma_Id == turma.Id).ToList();
                    }

                    CalendarioResponse calendario = _mapper.Map<CalendarioResponse>(aula);
                    calendario.Alunos = alunos;

                    list.Add(calendario);
                }

                // Incrementa data para próxima
                data = data.AddDays(1);
            } while (data < request.IntervaloAte);


            return list;
        }
    }
}
