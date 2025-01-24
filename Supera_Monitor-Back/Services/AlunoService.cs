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
        ResponseModel Update(UpdateAlunoRequest model/*, string ip*/);
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
                throw new Exception("Aluno not found.");
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
            // Validations passed

            // Se não foi passada uma pessoa especifica, criar uma e salvá-la no banco
            Pessoa? pessoa;

            if (model.Pessoa_Id == null) {
                pessoa = new() {
                    DataNascimento = model.DataNascimento,
                    Nome = model.Nome,
                };

                _db.Pessoas.Add(pessoa);
                _db.SaveChanges();

                pessoa = _db.Pessoas.AsNoTracking().FirstOrDefault(p => p.Nome == model.Nome && p.DataNascimento == model.DataNascimento);
            } else {
                // Se foi passada uma pessoa especifica, buscá-la no banco
                pessoa = _db.Pessoas.AsNoTracking().FirstOrDefault(p => p.Id == model.Pessoa_Id);
            }

            if (pessoa == null) {
                return new ResponseModel {
                    Message = "Ocorreu algum erro ao criar a pessoa."
                };
            }

            // Criar um aluno com o ID da pessoa
            Aluno aluno = new() {
                Pessoa_Id = pessoa.Id,
                Turma_Id = model.Turma_Id
            };

            _db.Alunos.Add(aluno);
            _db.SaveChanges();

            return new ResponseModel {
                Message = "Aluno cadastrado com sucesso",
                Object = _db.AlunoList.AsNoTracking().FirstOrDefault(a => a.Id == aluno.Id),
                Success = true,
            };
        }

        public ResponseModel Update(UpdateAlunoRequest model)
        {
            Aluno? aluno = _db.Alunos.Include(a => a.Pessoa).FirstOrDefault(aluno => aluno.Id == model.Id);

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

            AlunoList? old = _db.AlunoList.AsNoTracking().FirstOrDefault(t => t.Id == model.Id);

            pessoa.Nome = model.Nome;
            pessoa.DataNascimento = model.DataNascimento;

            _db.Pessoas.Update(pessoa);
            _db.SaveChanges();

            return new ResponseModel {
                Message = "Turma atualizada com sucesso",
                Object = _db.AlunoList.AsNoTracking().FirstOrDefault(x => x.Id == model.Id),
                Success = true,
                OldObject = old
            };
        }

        public ResponseModel Delete(int alunoId)
        {
            throw new NotImplementedException();
        }

        public List<Pessoa> GetAllPessoas()
        {
            List<Pessoa> pessoas = _db.Pessoas.ToList();

            return pessoas;
        }
    }
}
