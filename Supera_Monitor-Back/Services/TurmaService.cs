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
            Turma? turma = _db.Turmas
                .Include(t => t.Turma_Tipo)
                .FirstOrDefault(t => t.Id == turmaId);

            if (turma == null) {
                throw new Exception("Turma not found.");
            }

            // Validations passed

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
            List<TurmaTipo> types = _db.TurmaTipos.ToList();

            return _mapper.Map<List<TurmaTipoModel>>(types);
        }

        public ResponseModel Insert(CreateTurmaRequest model)
        {
            // Validações
            // Não devo poder criar turma com um professor que não existe
            // Não devo poder criar turma com um tipo que não existe

            Turma turma = _mapper.Map<Turma>(model);

            _db.Turmas.Add(turma);
            _db.SaveChanges();

            return new ResponseModel {
                Message = "Turma cadastrada com sucesso",
                Object = _db.TurmaList.AsNoTracking().FirstOrDefault(t => t.Id == turma.Id),
                Success = true,
            };
        }

        public ResponseModel Update(UpdateTurmaRequest model)
        {
            Turma? turma = _db.Turmas.FirstOrDefault(t => t.Id == model.Id);

            if (turma == null) {
                return new ResponseModel { Message = "Turma não encontrada" };
            }

            // Não devo poder atualizar turma colocando um professor que não existe
            // Não devo poder atualizar turma colocando um tipo que não existe
            // Validations passed

            TurmaList? old = _db.TurmaList.AsNoTracking().FirstOrDefault(t => t.Id == model.Id);

            turma.DiaSemana = model.DiaSemana;
            turma.Horario = model.Horario;
            turma.Professor_Id = model.Professor_Id;
            turma.Turma_Tipo_Id = model.Turma_Tipo_Id;

            _db.Turmas.Update(turma);
            _db.SaveChanges();

            return new ResponseModel {
                Message = "Turma atualizada com sucesso",
                Object = _db.TurmaList.AsNoTracking().FirstOrDefault(x => x.Id == model.Id),
                Success = true,
                OldObject = old
            };
        }

        public ResponseModel Delete(int turmaId)
        {
            Turma? turma = _db.Turmas
                .Include(t => t.Turma_Tipo)
                .FirstOrDefault(t => t.Id == turmaId);

            if (turma == null) {
                return new ResponseModel { Message = "Turma não encontrada" };
            }

            // Validations passed

            TurmaList? logObject = _db.TurmaList.Find(turmaId);

            _db.Turmas.Remove(turma);
            _db.SaveChanges();

            return new ResponseModel {
                Message = "Turma deletada com sucesso.",
                Success = true,
                Object = logObject,
            };
        }
    }
}
