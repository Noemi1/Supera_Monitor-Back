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
        ResponseModel Insert(CreateTurmaRequest model);
        ResponseModel Update(UpdateTurmaRequest model);
        ResponseModel Delete(int turmaId);
        List<TurmaList> GetAll();
        List<TurmaTipoModel> GetTypes();

        List<AlunoList> GetAllAlunosByTurma(int turmaId);
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
            Turma? turma = _db.Turmas.Include(t => t.Turma_Tipo).FirstOrDefault(t => t.Id == turmaId);

            if (turma == null) {
                throw new Exception("Turma não encontrada.");
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
            ResponseModel response = new() { Success = false };

            try {
                // Não devo poder criar turma com um tipo que não existe
                bool TurmaTipoExists = _db.TurmaTipos.Any(t => t.Id == model.Turma_Tipo_Id);

                if (!TurmaTipoExists) {
                    return new ResponseModel { Message = "Este tipo de turma não existe." };
                }

                // Futuro: Não devo poder criar turma com um professor que não existe

                // Validations passed

                Turma turma = _mapper.Map<Turma>(model);

                _db.Turmas.Add(turma);
                _db.SaveChanges();

                response.Message = "Turma cadastrada com sucesso";
                response.Object = turma;
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao inserir nova turma: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel Update(UpdateTurmaRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                Turma? turma = _db.Turmas.Find(model.Id);

                // Não devo poder atualizar uma turma que não existe
                if (turma == null) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                // Não devo poder atualizar turma com um tipo que não existe
                bool TurmaTipoExists = _db.TurmaTipos.Any(t => t.Id == model.Turma_Tipo_Id);

                if (!TurmaTipoExists) {
                    return new ResponseModel { Message = "Este tipo de turma não existe." };
                }

                // Futuro: Não devo poder atualizar turma com um professor que não existe

                // Validations passed

                response.OldObject = _db.TurmaList.Find(model.Id);

                turma.DiaSemana = model.DiaSemana;
                turma.Horario = model.Horario;
                turma.Professor_Id = model.Professor_Id;
                turma.Turma_Tipo_Id = model.Turma_Tipo_Id;

                _db.Turmas.Update(turma);
                _db.SaveChanges();

                response.Message = "Turma atualizada com sucesso";
                response.Object = turma;
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao atualizar a turma: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel Delete(int turmaId)
        {
            ResponseModel response = new() { Success = false };

            try {
                Turma? turma = _db.Turmas.Find(turmaId);

                if (turma == null) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                // Validations passed

                response.Object = _db.TurmaList.Find(turmaId);

                _db.Turmas.Remove(turma);
                _db.SaveChanges();

                response.Message = "Turma excluída com sucesso";
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao excluir turma: " + ex.ToString();
            }

            return response;
        }

        public List<AlunoList> GetAllAlunosByTurma(int turmaId)
        {
            List<AlunoList> alunos = _db.AlunoList.Where(a => a.Turma_Id == turmaId).ToList();

            return alunos;
        }

        //public ResponseModel InsertPresenca(RegisterPresencaRequest model)
        //{
        //    ResponseModel response = new() { Success = false };

        //    try {
        //        // Não devo poder registrar presença em uma aula que não existe
        //        TurmaAula? aula = _db.TurmaAulas.Find(model.Turma_Aula_Id);

        //        if (aula == null) {
        //            return new ResponseModel { Message = "Aula não encontrada" };
        //        }

        //        // Não devo poder registrar presença de um aluno que não existe
        //        Aluno? aluno = _db.Alunos.Find(model.Aluno_Id);

        //        if (aluno == null) {
        //            return new ResponseModel { Message = "Aluno não encontrado" };
        //        }

        //        // Não devo poder registrar presença de um aluno que não pertence a essa turma
        //        bool AlunoBelongsToTurma = aluno.Turma_Id == aula.Turma_Id;

        //        if (!AlunoBelongsToTurma) {
        //            return new ResponseModel { Message = "Aluno não pertence à turma" };
        //        }

        //        // Não devo poder registrar mais de uma presença para o mesmo aluno na mesma aula
        //        bool AlunoAlreadyPresent = _db.TurmaAulaAlunos.Any(a =>
        //            a.Turma_Aula_Id == model.Turma_Aula_Id &&
        //            a.Aluno_Id == model.Aluno_Id);

        //        if (AlunoAlreadyPresent) {
        //            return new ResponseModel { Message = "Presença do aluno já foi registrada para nesta aula" };
        //        }

        //        // Validations passed

        //        TurmaAulaAluno presenca = new() {
        //            Presente = model.Presente,
        //            Ah = model.Ah,
        //            ApostilaAbaco = model.ApostilaAbaco,
        //            NumeroPaginaAbaco = model.NumeroPaginaAbaco,
        //            NumeroPaginaAh = model.NumeroPaginaAH,

        //            Turma_Aula_Id = model.Turma_Aula_Id,
        //            Aluno_Id = model.Aluno_Id,
        //        };

        //        _db.TurmaAulaAlunos.Add(presenca);
        //        _db.SaveChanges();

        //        response.Message = "Presença registrada com sucesso";
        //        response.Object = presenca;
        //        response.Success = true;
        //    } catch (Exception ex) {
        //        response.Message = "Não foi possível inserir a presença" + ex.ToString();
        //    }

        //    return response;
        //}
    }
}
