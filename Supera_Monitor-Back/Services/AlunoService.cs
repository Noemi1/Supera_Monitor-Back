using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.CRM;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aluno;

namespace Supera_Monitor_Back.Services {
    public interface IAlunoService {
        AlunoList Get(int alunoId);
        List<AlunoList> GetAll();
        ResponseModel Insert(CreateAlunoRequest model);
        ResponseModel Update(UpdateAlunoRequest model);
        ResponseModel Delete(int alunoId);
    }

    public class AlunoService : IAlunoService {
        private readonly DataContext _db;
        private readonly CrmContext _crm;
        private readonly IMapper _mapper;

        public AlunoService(DataContext db, CrmContext crm, IMapper mapper)
        {
            _db = db;
            _crm = crm;
            _mapper = mapper;
        }

        public AlunoList Get(int alunoId)
        {
            AlunoList? aluno = _db.AlunoList.AsNoTracking().SingleOrDefault(a => a.Id == alunoId);

            if (aluno is null) {
                throw new Exception("Aluno não encontrado");
            }

            return aluno;
        }

        public List<AlunoList> GetAll()
        {
            List<AlunoList> alunos = _db.AlunoList.ToList();

            return alunos;
        }

        public ResponseModel Insert(CreateAlunoRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                Pessoa? pessoa = _crm.Pessoas.Find(model.Pessoa_Id);

                // Aluno só pode ser cadastrado se a pessoa existir no CRM
                if (pessoa == null) {
                    return new ResponseModel { Message = "Pessoa não encontrada" };
                }

                // Aluno só pode ser cadastrado se tiver status matriculado
                if (pessoa.Pessoa_Status_Id != ( int )PessoaStatus.Matriculado) {
                    return new ResponseModel { Message = "Pessoa não está matriculada" };
                }

                // Aluno só pode ser cadastrado se tiver Unidade_Id = 1 (dev)
                if (pessoa.Unidade_Id != 1) {
                    return new ResponseModel { Message = "Pessoa não tem Unidade_Id = 1" };
                }

                // Só pode ser cadastrado um aluno por pessoa
                bool AlunoAlreadyRegistered = _db.Alunos.Any(a => a.Pessoa_Id == model.Pessoa_Id);

                if (AlunoAlreadyRegistered) {
                    return new ResponseModel { Message = "Pessoa já tem um aluno cadastrado em seu nome" };
                }

                // Aluno só pode ser cadastrado em uma turma válida
                bool TurmaExists = _db.Turmas.Any(t => t.Id == model.Turma_Id);

                if (!TurmaExists) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                // Validations passed
                Aluno aluno = _mapper.Map<Aluno>(model);

                _db.Alunos.Add(aluno);
                _db.SaveChanges();

                response.Message = "Aluno cadastrado com sucesso";
                response.Object = aluno;
                response.Success = true;
            } catch (Exception ex) {
                response.Message = "Falha ao registrar aluno: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel Update(UpdateAlunoRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                Aluno? aluno = _db.Alunos.Find(model.Id);

                // Aluno só pode ser atualizado se existir
                if (aluno == null) {
                    return new ResponseModel { Message = "Aluno não encontrado" };
                }

                Pessoa? pessoa = _crm.Pessoas.Find(aluno.Pessoa_Id);

                // Pessoa só pode ser atualizada se existir no CRM
                if (pessoa == null) {
                    return new ResponseModel { Message = "Pessoa não encontrada" };
                }

                // Pessoa não pode ter um nome vazio
                if (string.IsNullOrEmpty(model.Nome)) {
                    return new ResponseModel { Message = "Nome não pode ser nulo/vazio" };
                }

                // Aluno só pode ser trocado de turma se for uma turma válida
                bool TurmaExists = _db.Turmas.Any(t => t.Id == model.Turma_Id);

                if (!TurmaExists) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                // Validations passed

                AlunoList? old = _db.AlunoList.AsNoTracking().FirstOrDefault(a => a.Id == model.Id);

                aluno.Turma_Id = model.Turma_Id;
                _db.Alunos.Update(aluno);
                _db.SaveChanges();

                // WARNING: Sending null values in the request will always override existing fields in Pessoa
                // If you'd like null values to be ignored do:
                // pessoa.CPF = model.CPF ?? pessoa.CPF;
                // However, this approach doesn't allow null, so you'd have to send an empty string
                // Else be careful with your requests
                _mapper.Map<Pessoa>(model);

                _crm.Pessoas.Update(pessoa);
                _crm.SaveChanges();

                response.Message = "Aluno atualizado com sucesso";
                response.Object = _db.AlunoList.AsNoTracking().FirstOrDefault(aluno => aluno.Id == model.Id);
                response.Success = true;
                response.OldObject = old;
            } catch (Exception ex) {
                response.Message = "Falha ao atualizar aluno: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel Delete(int alunoId)
        {
            throw new NotImplementedException();
        }
    }
}
