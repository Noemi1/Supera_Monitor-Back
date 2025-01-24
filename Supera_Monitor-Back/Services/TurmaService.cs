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

        List<TurmaAula> GetAllAulasByTurma(int turmaId);
        List<AlunoList> GetAllAlunosByTurma(int turmaId);

        ResponseModel InsertAula(InsertAulaRequest model);

        ResponseModel RegisterPresenca(RegisterPresencaRequest model);
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

            // Validations passed

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

            // Não devo poder atualizar uma turma que não existe
            if (turma == null) {
                return new ResponseModel { Message = "Turma não encontrada" };
            }
            // Não devo poder atualizar turma colocando um tipo que não existe
            bool TurmaTipoExists = _db.TurmaTipos.Any(t => t.Id == model.Turma_Tipo_Id);

            if (!TurmaTipoExists) {
                return new ResponseModel { Message = "Este tipo de turma não existe." };
            }

            // Não devo poder atualizar turma colocando um professor que não existe

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

            // Não devo poder deletar uma turma que não existe
            if (turma == null) {
                return new ResponseModel { Message = "Turma não encontrada" };
            }

            // Validations passed

            TurmaList? logObject = _db.TurmaList.Find(turmaId);

            _db.Turmas.Remove(turma);
            _db.SaveChanges();

            return new ResponseModel {
                Message = "Turma deletada com sucesso",
                Success = true,
                Object = logObject,
            };
        }

        public ResponseModel InsertAula(InsertAulaRequest model)
        {
            // Não devo poder registrar uma aula colocando uma turma que não existe
            bool TurmaExists = _db.Turmas.Any(t => t.Id == model.Turma_Id);

            if (!TurmaExists) {
                return new ResponseModel { Message = "Turma não encontrada" };
            }

            // Não devo poder registrar uma aula colocando um professor que não existe

            // Validations passed

            TurmaAula aula = new() {
                Turma_Id = model.Turma_Id,
                Professor_Id = model.Professor_Id,
                Data = model.Data
            };

            _db.TurmaAulas.Add(aula);
            _db.SaveChanges();

            return new ResponseModel {
                Message = "Aula registrada com sucesso",
                Object = aula,
                Success = true
            };
        }

        public List<TurmaAula> GetAllAulasByTurma(int turmaId)
        {
            List<TurmaAula> aulas = _db.TurmaAulas
                .Where(t => t.Turma_Id == turmaId)
                .Include(t => t.Turma_Aula_Alunos)
                .ToList();

            return aulas;
        }

        public List<AlunoList> GetAllAlunosByTurma(int turmaId)
        {
            List<AlunoList> alunos = _db.AlunoList.Where(a => a.Turma_Id == turmaId).ToList();

            return alunos;
        }

        public ResponseModel RegisterPresenca(RegisterPresencaRequest model)
        {
            TurmaAula? aula = _db.TurmaAulas.Find(model.Turma_Aula_Id);

            // Não devo poder registrar presença em uma aula que não existe
            if (aula == null) {
                return new ResponseModel { Message = "Aula não encontrada" };
            }

            // Não devo poder registrar presença de um aluno que não pertence a essa turma
            bool AlunoBelongsToTurma = _db.AlunoList
                .AsNoTracking()
                .Any(a => a.Id == model.Aluno_Id && a.Turma_Id == aula.Turma_Id);

            if (!AlunoBelongsToTurma) {
                return new ResponseModel { Message = "Aluno não pertence à turma" };
            }

            // Validations passed

            TurmaAulaAluno presenca = new() {
                Presente = model.Presente,
                Ah = model.Ah,
                ApostilaAbaco = model.ApostilaAbaco,
                NumeroPaginaAbaco = model.NumeroPaginaAbaco,
                NumeroPaginaAh = model.NumeroPaginaAH,

                Turma_Aula_Id = model.Turma_Aula_Id,
                Aluno_Id = model.Aluno_Id,
            };

            _db.TurmaAulaAlunos.Add(presenca);
            _db.SaveChanges();

            return new ResponseModel {
                Message = "Presença registrada com sucesso",
                Object = presenca,
                Success = true
            };
        }
    }
}
