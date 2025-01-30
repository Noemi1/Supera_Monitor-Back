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
                    Data = model.Data
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
    }
}
