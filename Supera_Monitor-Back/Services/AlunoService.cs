using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aluno;

namespace Supera_Monitor_Back.Services {
    public interface IAlunoService {
        AlunoResponse Get(int alunoId);
        List<AlunoList> GetAll();
        ResponseModel Insert(CreateAlunoRequest model);
        ResponseModel Update(UpdateAlunoRequest model);
        ResponseModel Delete(int alunoId);

        List<Pessoa> GetAllPessoas();
    }

    public class AlunoService : IAlunoService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;

        public AlunoService(DataContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public AlunoResponse Get(int alunoId)
        {
            Aluno? aluno = _db.Alunos
                .Include(t => t.Turma)
                .Include(p => p.Pessoa)
                .FirstOrDefault(a => a.Id == alunoId);

            if (aluno == null) {
                throw new Exception("Aluno não encontrado.");
            }

            // Validations passed

            AlunoResponse response = _mapper.Map<AlunoResponse>(aluno);

            return response;
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
                Pessoa? pessoa;

                // Se foi passado um Pessoa_Id no request, busca a pessoa no banco, senão cria uma e salva no banco
                if (model.Pessoa_Id == null) {
                    pessoa = new() {
                        DataNascimento = model.DataNascimento,
                        Nome = model.Nome,
                    };

                    _db.Pessoas.Add(pessoa);
                    _db.SaveChanges();
                } else {
                    pessoa = _db.Pessoas.AsNoTracking().FirstOrDefault(p => p.Id == model.Pessoa_Id);
                }

                if (pessoa == null) {
                    return new ResponseModel { Message = "Ocorreu algum erro ao criar a pessoa." };
                }

                Aluno aluno = new() {
                    Pessoa_Id = pessoa.Id,
                    Turma_Id = model.Turma_Id,
                };

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
                Aluno? aluno = _db.Alunos.Include(a => a.Pessoa).FirstOrDefault(a => a.Id == model.Id);

                if (aluno == null) {
                    return new ResponseModel { Message = "Aluno não encontrado" };
                }

                Pessoa? pessoa = _db.Pessoas.FirstOrDefault(pessoa => pessoa.Id == aluno.Pessoa_Id);

                if (pessoa == null) {
                    return new ResponseModel { Message = "Pessoa não encontrada" };
                }

                if (string.IsNullOrEmpty(model.Nome)) {
                    return new ResponseModel { Message = "Nome Inválido" };
                }

                // Validations passed

                AlunoList? old = _db.AlunoList.AsNoTracking().FirstOrDefault(a => a.Id == model.Id);

                pessoa.Nome = model.Nome;
                pessoa.DataNascimento = model.DataNascimento;

                _db.Pessoas.Update(pessoa);
                _db.SaveChanges();

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
            // Como lidar com o delete?
            throw new NotImplementedException();
        }

        public List<Pessoa> GetAllPessoas()
        {
            List<Pessoa> pessoas = _db.Pessoas.ToList();

            return pessoas;
        }
    }
}
