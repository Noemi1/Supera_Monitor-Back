using AutoMapper;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Roteiro;

namespace Supera_Monitor_Back.Services;

public interface IRoteiroService {
    List<RoteiroModel> GetAll();
    ResponseModel Insert(CreateRoteiroRequest model);
    ResponseModel Update(UpdateRoteiroRequest model);
    ResponseModel ToggleDeactivate(int roteiroId);

    List<MaterialModel> GetAllMaterialByRoteiro(int roteiroId);
    ResponseModel InsertMaterial(CreateRoteiroMaterialRequest model);
    ResponseModel ToggleDeactivateMaterial(int roteiroMaterialId);
}

public class RoteiroService : IRoteiroService {
    private readonly DataContext _db;
    private readonly IMapper _mapper;
    private readonly Account? _account;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public RoteiroService(DataContext db, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _account = ( Account? )_httpContextAccessor?.HttpContext?.Items["Account"];
    }

    public List<RoteiroModel> GetAll()
    {
        List<Roteiro> listRoteiros = _db.Roteiros
            .OrderBy(j => j.Semana)
            .ToList();

        return _mapper.Map<List<RoteiroModel>>(listRoteiros);
    }

    public ResponseModel Insert(CreateRoteiroRequest model)
    {
        ResponseModel response = new() { Success = false };

        try {
            if (model.DataFim <= model.DataInicio) {
                return new ResponseModel { Message = "Data de fim não pode ser anterior à data de início" };
            }

            // Não deve ser possível criar um roteiro em uma semana que já está associada a outro roteiro (nos roteiros ativos)
            bool hasSemanaConflict = _db.Roteiros.Any(r =>
                r.Deactivated == null &&
                r.Semana == model.Semana);

            if (hasSemanaConflict) {
                return new ResponseModel { Message = "A semana passada na requisição já possui um roteiro associado." };
            }

            // Verifica se o novo roteiro se sobrepõe a outro roteiro existente ativo
            // As condições cobrem os seguintes casos:
            // 1. A DataInicio do novo roteiro está dentro do intervalo de um roteiro existente.
            // 2. A DataFim do novo roteiro está dentro do intervalo de um roteiro existente.
            // 3. O novo roteiro engloba completamente outro roteiro já cadastrado.
            bool isDuringAnotherRoteiro = _db.Roteiros.Any(r =>
                r.Deactivated == null &&
                ((model.DataInicio >= r.DataInicio && model.DataInicio <= r.DataFim) || // DataInicio está dentro de outro roteiro
                 (model.DataFim >= r.DataInicio && model.DataFim <= r.DataFim) ||       // DataFim está dentro de outro roteiro
                 (model.DataInicio <= r.DataInicio && model.DataFim >= r.DataFim))      // Novo roteiro engloba outro roteiro
            );

            if (isDuringAnotherRoteiro) {
                return new ResponseModel { Message = "As datas passadas na requisição entram em conflito com outro roteiro existente." };
            }

            // Validations passed

            Roteiro newRoteiro = new() {
                Tema = model.Tema,
                Semana = model.Semana,
                DataInicio = model.DataInicio,
                DataFim = model.DataFim,
                CorLegenda = model.CorLegenda,

                Deactivated = null,
                Created = TimeFunctions.HoraAtualBR(),
                Account_Created_Id = _account!.Id,
            };

            _db.Roteiros.Add(newRoteiro);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Roteiro inserido com sucesso";
            response.Object = _mapper.Map<RoteiroModel>(newRoteiro);
        } catch (Exception ex) {
            response.Message = $"Falha ao inserir roteiro: {ex}";
        }

        return response;
    }

    public ResponseModel Update(UpdateRoteiroRequest model)
    {
        ResponseModel response = new() { Success = false };

        try {
            Roteiro? roteiro = _db.Roteiros.FirstOrDefault(r => r.Id == model.Id);

            if (roteiro == null) {
                return new ResponseModel { Message = "Roteiro não encontrado." };
            }

            if (model.DataFim <= model.DataInicio) {
                return new ResponseModel { Message = "Data de fim não pode ser anterior à data de início" };
            }

            // Não deve ser possível criar um roteiro em uma semana que já está associada a outro roteiro (nos roteiros ativos)
            bool hasSemanaConflict = _db.Roteiros.Any(r =>
                r.Id != model.Id &&
                r.Deactivated == null &&
                r.Semana == model.Semana);

            if (hasSemanaConflict) {
                return new ResponseModel { Message = "A semana passada na requisição já possui um roteiro associado." };
            }

            // Validations passed

            roteiro.Tema = model.Tema;
            roteiro.Semana = model.Semana;
            roteiro.DataInicio = model.DataInicio;
            roteiro.DataFim = model.DataFim;
            roteiro.CorLegenda = model.CorLegenda;

            roteiro.LastUpdated = TimeFunctions.HoraAtualBR();

            _db.Roteiros.Update(roteiro);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Roteiro atualizado com sucesso";
            response.Object = _mapper.Map<RoteiroModel>(roteiro);
        } catch (Exception ex) {
            response.Message = $"Falha ao atualizar roteiro: {ex}";
        }

        return response;
    }

    public ResponseModel ToggleDeactivate(int roteiroId)
    {
        ResponseModel response = new() { Success = false };

        try {
            Roteiro? roteiro = _db.Roteiros.FirstOrDefault(r => r.Id == roteiroId);

            if (roteiro == null) {
                return new ResponseModel { Message = "Roteiro não encontrado." };
            }

            // Validations passed

            bool isRoteiroActive = roteiro.Deactivated == null;

            roteiro.Deactivated = isRoteiroActive ? TimeFunctions.HoraAtualBR() : null;

            _db.Roteiros.Update(roteiro);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Roteiro desativado com sucesso";
        } catch (Exception ex) {
            response.Message = $"Falha ao desativar roteiro: {ex}";
        }

        return response;
    }

    public List<MaterialModel> GetAllMaterialByRoteiro(int roteiroId)
    {
        List<Roteiro_Material> listMaterial = _db.Roteiro_Materials
            .Where(m => m.Roteiro_Id == roteiroId)
            .ToList();

        return _mapper.Map<List<MaterialModel>>(listMaterial);
    }

    public ResponseModel InsertMaterial(CreateRoteiroMaterialRequest model)
    {
        ResponseModel response = new() { Success = false };

        try {
            Roteiro? roteiro = _db.Roteiros.FirstOrDefault(r => r.Id == model.Roteiro_Id);

            if (roteiro == null) {
                return new ResponseModel { Message = "Roteiro não encontrado." };
            }

            if (roteiro.Deactivated.HasValue) {
                return new ResponseModel { Message = "Não é possível adicionar material em um roteiro desativado" };
            }

            // Validations passed

            Roteiro_Material newMaterial = new() {
                FileName = model.FileName,
                FileBase64 = model.FileBase64,
                Roteiro_Id = model.Roteiro_Id,

                Deactivated = null,
                Account_Created_Id = _account!.Id,
                Created = TimeFunctions.HoraAtualBR(),
            };

            _db.Roteiro_Materials.Add(newMaterial);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Material criado com sucesso";
            response.Object = _mapper.Map<MaterialModel>(newMaterial);
        } catch (Exception ex) {
            response.Message = $"Falha ao criar material: {ex}";
        }

        return response;
    }

    public ResponseModel ToggleDeactivateMaterial(int roteiroMaterialId)
    {
        ResponseModel response = new() { Success = false };

        try {
            Roteiro_Material? material = _db.Roteiro_Materials.FirstOrDefault(rm => rm.Id == roteiroMaterialId);

            if (material == null) {
                return new ResponseModel { Message = "Material não encontrado." };
            }

            // Validations passed

            bool isMaterialActive = material.Deactivated == null;

            material.Deactivated = isMaterialActive ? TimeFunctions.HoraAtualBR() : null;

            _db.Roteiro_Materials.Update(material);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Material desativado com sucesso";
            response.Object = _mapper.Map<MaterialModel>(material);
        } catch (Exception ex) {
            response.Message = $"Falha ao desativar material: {ex}";
        }

        return response;
    }


}
