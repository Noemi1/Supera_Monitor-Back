using AutoMapper;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Professor;

namespace Supera_Monitor_Back.Services {
    public interface IProfessorService {
        ResponseModel Get(int professorId);
        List<ProfessorList> GetAll();
        ResponseModel Insert(CreateProfessorRequest model);
        ResponseModel Update(UpdateProfessorRequest model);
        ResponseModel Delete(int professorId);
    }

    public class ProfessorService : IProfessorService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;

        public ProfessorService(DataContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public ResponseModel Get(int professorId)
        {

            throw new NotImplementedException();
        }

        public List<ProfessorList> GetAll()
        {
            throw new NotImplementedException();
            //List<ProfessorList> professores = _db.TurmaList.ToList();

            //return professores;
        }

        public ResponseModel Insert(CreateProfessorRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {

            } catch (Exception ex) {
                response.Message = "Falha ao cadastrar professor: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel Update(UpdateProfessorRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {

            } catch (Exception ex) {
                response.Message = "Falha ao atualizar professor: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel Delete(int professorId)
        {
            ResponseModel response = new() { Success = false };

            try {

            } catch (Exception ex) {
                response.Message = "Falha ao deletar professor: " + ex.ToString();
            }

            return response;
        }
    }
}
