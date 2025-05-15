using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Restricao;

namespace Supera_Monitor_Back.Services;

public interface IRestricaoService {
    RestricaoModel Get(int restricaoId);
    List<RestricaoModel> GetAll();
    List<RestricaoModel> GetAllByAluno(int alunoId);
    ResponseModel Insert(CreateRestricaoRequest model);
    ResponseModel Update(UpdateRestricaoRequest model);
    ResponseModel ToggleActive(int restricaoId);
}

public class RestricaoService : IRestricaoService {
    private readonly DataContext _db;
    private readonly IMapper _mapper;
    private readonly Account? _account;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public RestricaoService(DataContext db, IMapper mapper, IHttpContextAccessor httpContextAccessor) {
        _db = db;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _account = (Account?)_httpContextAccessor?.HttpContext?.Items["Account"];
    }

    public RestricaoModel Get(int restricaoId) {
        Aluno_Restricao? restricao = _db.Aluno_Restricaos.Find(restricaoId);

        if (restricao is null) {
            throw new Exception("Restrição não encontrada");
        }

        return _mapper.Map<RestricaoModel>(restricao);
    }

    public List<RestricaoModel> GetAll() {
        List<Aluno_Restricao> listRestricoes = _db.Aluno_Restricaos.ToList();

        return _mapper.Map<List<RestricaoModel>>(listRestricoes);
    }

    public ResponseModel Insert(CreateRestricaoRequest model) {
        ResponseModel response = new() { Success = false };

        try {
            Aluno? aluno = _db.Alunos
                .Include(a => a.Pessoa)
                .FirstOrDefault(a => a.Id == model.Aluno_Id);

            if (aluno is null) {
                return new ResponseModel { Message = "Aluno não encontrado" };
            }

            if (string.IsNullOrEmpty(model.Descricao)) {
                return new ResponseModel { Message = "Descrição da restrição não pode ser nula/vazia" };
            }

            Aluno_Restricao newRestricao = new()
            {
                Descricao = model.Descricao,
                Aluno_Id = model.Aluno_Id,

                Created = TimeFunctions.HoraAtualBR(),
                Deactivated = null,
                Account_Created_Id = _account.Id,
            };

            _db.Aluno_Restricaos.Add(newRestricao);
            _db.SaveChanges();

            response.Success = true;
            response.Message = $"Restrição criada com sucesso";
            response.Object = _mapper.Map<RestricaoModel>(newRestricao);
        }
        catch (Exception ex) {
            response.Message = $"Não foi possível criar a restrição: {ex}";
        }

        return response;
    }

    public ResponseModel Update(UpdateRestricaoRequest model) {
        ResponseModel response = new() { Success = false };

        try {
            Aluno_Restricao? restricao = _db.Aluno_Restricaos.Find(model.Id);

            if (restricao is null) {
                return new ResponseModel { Message = "Restrição não encontrada" };
            }

            if (string.IsNullOrEmpty(model.Descricao)) {
                return new ResponseModel { Message = "Descrição da restrição não pode ser nula/vazia" };
            }

            Aluno_Restricao? oldRestricao = _db.Aluno_Restricaos.AsNoTracking().FirstOrDefault(r => r.Id == model.Id);

            restricao.Descricao = model.Descricao;

            _db.Aluno_Restricaos.Update(restricao);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Restrição atualizada com sucesso";
            response.Object = _mapper.Map<RestricaoModel>(restricao);
            response.OldObject = _mapper.Map<RestricaoModel>(oldRestricao);
        }
        catch (Exception ex) {
            response.Message = $"Não foi possível atualizar a restrição: {ex}";
        }

        return response;
    }

    public ResponseModel ToggleActive(int restricaoId) {
        ResponseModel response = new() { Success = false };

        try {
            Aluno_Restricao? restricao = _db.Aluno_Restricaos.Find(restricaoId);

            if (restricao is null) {
                return new ResponseModel { Message = "Restrição não encontrada" };
            }

            restricao.Deactivated = restricao.Deactivated.HasValue ? null : TimeFunctions.HoraAtualBR();

            _db.Aluno_Restricaos.Update(restricao);
            _db.SaveChanges();

            response.Success = true;
            response.Object = _mapper.Map<RestricaoModel>(restricao);
            response.Message = "Restrição desativada com sucesso";
        }
        catch (Exception ex) {
            response.Message = "Não foi possível desativar a restrição: " + ex.ToString();
        }

        return response;
    }

    public List<RestricaoModel> GetAllByAluno(int alunoId) {
        List<Aluno_Restricao> restricoes = _db.Aluno_Restricaos
            .Include(r => r.Aluno)
            .ThenInclude(r => r.Pessoa)
            .Where(r => r.Aluno_Id == alunoId)
            .ToList();

        return _mapper.Map<List<RestricaoModel>>(restricoes);
    }
}
