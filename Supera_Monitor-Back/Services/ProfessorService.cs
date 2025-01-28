using AutoMapper;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Professor;

namespace Supera_Monitor_Back.Services {
    public interface IProfessorService {
        ProfessorResponse Get(int professorId);
        ResponseModel Insert(CreateProfessorRequest model);
        ResponseModel Update(UpdateProfessorRequest model);
        ResponseModel Delete(int professorId);

        List<ProfessorList> GetAll();
    }

    public class ProfessorService : IProfessorService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;

        public ProfessorService(DataContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public ProfessorResponse Get(int professorId)
        {
            var professor = _db.Professors.Find(professorId);

            return _mapper.Map<ProfessorResponse>(professor);
        }

        public List<ProfessorList> GetAll()
        {
            List<ProfessorList> professores = _db.ProfessorList.ToList();

            return professores;
        }

        public ResponseModel Insert(CreateProfessorRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                var account = _db.AccountList.Find(model.Account_Id);

                if (account == null) {
                    return new ResponseModel { Message = "Conta não foi encontrada." };
                }

                // Validations passed

                Professor professor = new() {
                    DataInicio = model.DataInicio,
                    Account_Id = model.Account_Id,
                    NivelAbaco = model.NivelAbaco,
                    NivelAh = model.NivelAH
                };

                _db.Professors.Add(professor);
                _db.SaveChanges();

                response.Message = "Professor cadastrado com sucesso";
                response.Object = professor;
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao cadastrar professor: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel Update(UpdateProfessorRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                Professor? professor = _db.Professors.Find(model.Id);

                // Não devo poder atualizar um professor que não existe
                if (professor == null) {
                    return new ResponseModel { Message = "Professor não encontrado" };
                }

                // Validations passed

                response.OldObject = _db.ProfessorList.Find(model.Id);

                professor.DataInicio = model.DataInicio;
                professor.NivelAh = model.NivelAH;
                professor.NivelAbaco = model.NivelAbaco;

                _db.Professors.Update(professor);
                _db.SaveChanges();

                response.Message = "Professor atualizado com sucesso";
                response.Object = professor;
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao atualizar professor: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel Delete(int professorId)
        {
            ResponseModel response = new() { Success = false };

            try {
                Professor? professor = _db.Professors.Find(professorId);

                if (professor == null) {
                    return new ResponseModel { Message = "Professor não encontrado" };
                }

                // Validations passed

                response.Object = _db.ProfessorList.Find(professorId);

                _db.Professors.Remove(professor);
                _db.SaveChanges();

                response.Message = "Turma excluída com sucesso";
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao deletar professor: " + ex.ToString();
            }

            return response;
        }
    }
}
