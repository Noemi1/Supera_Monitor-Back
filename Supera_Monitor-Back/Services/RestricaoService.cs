using AutoMapper;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Restricao;

namespace Supera_Monitor_Back.Services;

public interface IRestricaoService {
    AlunoRestricaoModel Get(int restricaoId);
    List<AlunoRestricaoModel> GetAll();
    ResponseModel Insert(CreateRestricaoRequest model);
    ResponseModel Update(UpdateRestricaoRequest model);
    ResponseModel Delete(int restricaoId);
}

public class RestricaoService : IRestricaoService {
    private readonly DataContext _db;
    private readonly IMapper _mapper;

    public RestricaoService(DataContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public AlunoRestricaoModel Get(int restricaoId)
    {
        Aluno_Restricao? restricao = _db.Aluno_Restricao.Find(restricaoId);

        if (restricao is null) {
            throw new Exception("Restrição não encontrada");
        }

        return _mapper.Map<AlunoRestricaoModel>(restricao);
    }

    public List<AlunoRestricaoModel> GetAll()
    {
        List<Aluno_Restricao> listChecklist = _db.Aluno_Restricao.ToList();

        return _mapper.Map<List<AlunoRestricaoModel>>(listChecklist);
    }

    public ResponseModel Insert(CreateRestricaoRequest model)
    {
        ResponseModel response = new() { Success = false };

        try {
            if (string.IsNullOrEmpty(model.Restricao)) {
                return new ResponseModel { Message = "Restrição não pode ser nula/vazia" };
            }

            bool restricaoAlreadyExists = _db.Aluno_Restricao.Any(r => r.Restricao == model.Restricao);

            if (restricaoAlreadyExists) {
                return new ResponseModel { Message = "Restrição já está registrada" };
            }

            Aluno_Restricao newRestricao = new() { Restricao = model.Restricao };

            _db.Aluno_Restricao.Add(newRestricao);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Restrição criada com sucesso";
            response.Object = _mapper.Map<AlunoRestricaoModel>(newRestricao);
        } catch (Exception ex) {
            response.Message = "Não foi possível criar a restrição: " + ex.ToString();
        }

        return response;
    }

    public ResponseModel Update(UpdateRestricaoRequest model)
    {
        ResponseModel response = new() { Success = false };

        try {
            Aluno_Restricao? restricao = _db.Aluno_Restricao.Find(model.Id);

            if (restricao is null) {
                return new ResponseModel { Message = "Restrição não encontrada" };
            }

            if (string.IsNullOrEmpty(model.Restricao)) {
                return new ResponseModel { Message = "Restrição não pode ser nula/vazia" };
            }

            bool restricaoAlreadyExists = _db.Aluno_Restricao
                .Any(r => r.Restricao == model.Restricao && r.Id != model.Id);

            if (restricaoAlreadyExists) {
                return new ResponseModel { Message = "Restrição já está registrada" };
            }

            restricao.Restricao = model.Restricao;

            _db.Aluno_Restricao.Update(restricao);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Restrição atualizada com sucesso";
            response.Object = _mapper.Map<AlunoRestricaoModel>(restricao);
        } catch (Exception ex) {
            response.Message = "Não foi possível atualizar a restrição: " + ex.ToString();
        }

        return response;
    }

    public ResponseModel Delete(int restricaoId)
    {
        ResponseModel response = new() { Success = false };

        try {
            Aluno_Restricao? restricao = _db.Aluno_Restricao.Find(restricaoId);

            if (restricao is null) {
                return new ResponseModel { Message = "Restrição não encontrada" };
            }

            _db.Aluno_Restricao.Remove(restricao);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Restrição removida com sucesso";
        } catch (Exception ex) {
            response.Message = "Não foi possível remover a restrição: " + ex.ToString();
        }

        return response;
    }
}
