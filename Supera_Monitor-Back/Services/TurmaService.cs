using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Turma;

namespace Supera_Monitor_Back.Services {
    public interface ITurmaService {
        TurmaResponse Get(int turmaId);
        List<TurmaList> GetAll();
        List<TurmaTipoModel> GetTypes();
        ResponseModel Insert(CreateTurmaRequest model);
        ResponseModel Update(UpdateTurmaRequest model/*, string ip*/);
        ResponseModel Delete(int turmaId);
    }

    public class TurmaService : ITurmaService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;

        public TurmaService(DataContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public TurmaResponse Get(int turmaId)
        {
            Turma? turma = _db.Turma
                .Include(t => t.Turma_Tipo)
                .FirstOrDefault(t => t.Id == turmaId);

            if (turma == null) {
                throw new Exception("Turma not found.");
            }

            TurmaResponse response = _mapper.Map<TurmaResponse>(turma);

            return response;
        }

        public List<TurmaList> GetAll()
        {
            List<TurmaList> turmas = _db.TurmaList.ToList();

            return turmas;
        }

        public List<TurmaTipoModel> GetTypes()
        {
            List<Turma_Tipo> types = _db.Turma_Tipo.ToList();

            return _mapper.Map<List<TurmaTipoModel>>(types);
        }

        public ResponseModel Insert(CreateTurmaRequest model)
        {
            // Validações
            // Não devo poder criar turma com um professor que não existe
            // Não devo poder criar turma com um tipo que não existe

            Turma turma = _mapper.Map<Turma>(model);

            _db.Turma.Add(turma);
            _db.SaveChanges();

            return new ResponseModel {
                Message = "Turma cadastrada com sucesso",
                Object = _db.TurmaList.Find(turma.Id),
                Success = true,
            };
        }

        public ResponseModel Update(UpdateTurmaRequest model)
        {
            throw new NotImplementedException();
        }

        public ResponseModel Delete(int turmaId)
        {
            throw new NotImplementedException();
        }
    }
}
