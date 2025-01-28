using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aula;

namespace Supera_Monitor_Back.Services {
    public interface IAulaService {
        AulaResponse Get(int aulaId);
        ResponseModel Insert(CreateAulaRequest model);
        ResponseModel Update(UpdateAulaRequest model);
        ResponseModel Delete(int aulaId);

        List<AulaList> GetAll();
        List<AulaList> GetAllByTurmaId(int turmaId);
        List<AulaList> GetAllByProfessorId(int professorId);
    }

    public class AulaService : IAulaService {
        public AulaResponse Get(int aulaId)
        {
            throw new NotImplementedException();
        }

        public List<AulaList> GetAll()
        {
            throw new NotImplementedException();
        }

        public List<AulaList> GetAllByTurmaId(int turmaId)
        {
            throw new NotImplementedException();
        }

        public List<AulaList> GetAllByProfessorId(int professorId)
        {
            throw new NotImplementedException();
        }

        public ResponseModel Insert(CreateAulaRequest model)
        {
            throw new NotImplementedException();
        }

        public ResponseModel Update(UpdateAulaRequest model)
        {
            throw new NotImplementedException();
        }

        public ResponseModel Delete(int aulaId)
        {
            throw new NotImplementedException();
        }
    }
}
