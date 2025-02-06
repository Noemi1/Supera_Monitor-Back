using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aluno;
using Supera_Monitor_Back.Models.Pessoa;
using static Supera_Monitor_Back.Entities.Pessoa_Status;

namespace Supera_Monitor_Back.Services {
    public interface IAlunoService {
        AlunoList Get(int alunoId);
        List<AlunoList> GetAll();
        ResponseModel Insert(CreateAlunoRequest model);
        ResponseModel Update(UpdateAlunoRequest model);
        ResponseModel ToggleDeactivate(int alunoId);
    }

    public class AlunoService : IAlunoService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;
        private readonly Account? _account;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPessoaService _pessoaService;

        public AlunoService(DataContext db, IMapper mapper, IPessoaService pessoaService, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _mapper = mapper;
            _pessoaService = pessoaService;
            _httpContextAccessor = httpContextAccessor;
            _account = ( Account? )httpContextAccessor?.HttpContext?.Items["Account"];
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
                Pessoa? pessoa = _db.Pessoas.Find(model.Pessoa_Id);

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

                aluno.Created = TimeFunctions.HoraAtualBR();
                aluno.Deactivated = null;

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

                // Aluno só pode ser trocado de turma se for uma turma válida
                bool TurmaExists = _db.Turmas.Any(t => t.Id == model.Turma_Id);

                if (!TurmaExists) {
                    return new ResponseModel { Message = "Turma não encontrada" };
                }

                AlunoList? old = _db.AlunoList.AsNoTracking().FirstOrDefault(a => a.Id == model.Id);

                UpdatePessoaRequest pessoaModel = _mapper.Map<UpdatePessoaRequest>(model);
                pessoaModel.Pessoa_Id = aluno.Pessoa_Id;

                ResponseModel pessoaResponse = _pessoaService.Update(pessoaModel);

                // Caso não tenha passado nas validações de Pessoa
                if (pessoaResponse.Success == false) {
                    return pessoaResponse;
                }

                aluno.Turma_Id = model.Turma_Id;
                aluno.LastUpdated = TimeFunctions.HoraAtualBR();

                _db.Alunos.Update(aluno);
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

        public ResponseModel ToggleDeactivate(int alunoId)
        {
            ResponseModel response = new() { Success = false };

            try {
                Aluno? aluno = _db.Alunos.Find(alunoId);

                if (aluno == null) {
                    return new ResponseModel { Message = "Aluno não encontrado." };
                }

                if (_account == null) {
                    return new ResponseModel { Message = "Não foi possível completar a ação. Autenticação do autor não encontrada." };
                }

                // Validations passed

                bool IsAlunoActive = aluno.Deactivated == null;

                aluno.Deactivated = IsAlunoActive ? TimeFunctions.HoraAtualBR() : null;

                _db.Alunos.Update(aluno);
                _db.SaveChanges();

                response.Success = true;
                response.Object = _db.AlunoList.AsNoTracking().FirstOrDefault(a => a.Id == aluno.Id);
            } catch (Exception ex) {
                response.Message = "Falha ao ativar/desativar aluno: " + ex.ToString();
            }

            return response;
        }
    }
}
