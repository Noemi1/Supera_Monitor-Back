using AutoMapper;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Jornada;

namespace Supera_Monitor_Back.Services;

public interface IJornadaService {
    List<JornadaModel> GetAll();
    ResponseModel Insert(CreateJornadaRequest model);
    ResponseModel Update(UpdateJornadaRequest model);
    ResponseModel ToggleDeactivate(int jornadaId);

    List<MaterialModel> GetAllMaterialByJornada(int jornadaId);
    ResponseModel InsertMaterial(CreateJornadaMaterialRequest model);
    ResponseModel ToggleDeactivateMaterial(int jornadaMaterialId);
}

public class JornadaService : IJornadaService {
    private readonly DataContext _db;
    private readonly IMapper _mapper;
    private readonly Account? _account;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public JornadaService(DataContext db, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _account = ( Account? )_httpContextAccessor?.HttpContext?.Items["Account"];
    }

    public List<JornadaModel> GetAll()
    {
        List<Jornada> listJornada = _db.Jornada
            .OrderBy(j => j.Semana)
            .ToList();

        return _mapper.Map<List<JornadaModel>>(listJornada);
    }

    public ResponseModel Insert(CreateJornadaRequest model)
    {
        ResponseModel response = new() { Success = false };

        try {
            if (model.DataFim <= model.DataInicio) {
                return new ResponseModel { Message = "Data de fim não pode ser anterior à data de início" };
            }

            // Não deve ser possível criar uma jornada em uma semana que já está associada a outra jornada (nas jornadas ativas)
            bool hasSemanaConflict = _db.Jornada.Any(j =>
                j.Deactivated == null &&
                j.Semana == model.Semana);

            if (hasSemanaConflict) {
                return new ResponseModel { Message = "A semana passada na requisição já possui uma jornada associada." };
            }

            // Verifica se a nova jornada se sobrepõe a uma jornada existente ativa
            // As condições cobrem os seguintes casos:
            // 1. A DataInicio da nova jornada está dentro do intervalo de uma jornada existente.
            // 2. A DataFim da nova jornada está dentro do intervalo de uma jornada existente.
            // 3. A nova jornada engloba completamente outra jornada já cadastrada.
            bool isDuringAnotherJornada = _db.Jornada.Any(j =>
                j.Deactivated == null &&
                ((model.DataInicio >= j.DataInicio && model.DataInicio <= j.DataFim) || // DataInicio está dentro de outra jornada
                 (model.DataFim >= j.DataInicio && model.DataFim <= j.DataFim) ||       // DataFim está dentro de outra jornada
                 (model.DataInicio <= j.DataInicio && model.DataFim >= j.DataFim))      // Nova jornada engloba outra jornada
            );

            if (isDuringAnotherJornada) {
                return new ResponseModel { Message = "As datas passadas na requisição entram em conflito com outra jornada existente." };
            }

            // Validations passed

            Jornada newJornada = new() {
                Tema = model.Tema,
                Semana = model.Semana,
                DataInicio = model.DataInicio,
                DataFim = model.DataFim,

                Deactivated = null,
                Created = TimeFunctions.HoraAtualBR(),
                Account_Created_Id = _account.Id,
            };

            _db.Jornada.Add(newJornada);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Jornada inserida com sucesso";
            response.Object = _mapper.Map<JornadaModel>(newJornada);
        } catch (Exception ex) {
            response.Message = $"Falha ao inserir jornada: {ex}";
        }

        return response;
    }

    public ResponseModel Update(UpdateJornadaRequest model)
    {
        ResponseModel response = new() { Success = false };

        try {
            Jornada? jornada = _db.Jornada.FirstOrDefault(j => j.Id == model.Id);

            if (jornada == null) {
                return new ResponseModel { Message = "Jornada não encontrada." };
            }

            if (model.DataFim <= model.DataInicio) {
                return new ResponseModel { Message = "Data de fim não pode ser anterior à data de início" };
            }

            // Não deve ser possível criar uma jornada em uma semana que já está associada a outra jornada (nas jornadas ativas)
            bool hasSemanaConflict = _db.Jornada.Any(j =>
                j.Id != model.Id &&
                j.Deactivated == null &&
                j.Semana == model.Semana);

            if (hasSemanaConflict) {
                return new ResponseModel { Message = "A semana passada na requisição já possui uma jornada associada." };
            }

            // Validations passed

            jornada.Tema = model.Tema;
            jornada.Semana = model.Semana;
            jornada.DataInicio = model.DataInicio;
            jornada.DataFim = model.DataFim;

            jornada.LastUpdated = TimeFunctions.HoraAtualBR();

            _db.Jornada.Update(jornada);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Jornada atualizada com sucesso";
            response.Object = _mapper.Map<JornadaModel>(jornada);
        } catch (Exception ex) {
            response.Message = $"Falha ao atualizar jornada: {ex}";
        }

        return response;
    }

    public ResponseModel ToggleDeactivate(int jornadaId)
    {
        ResponseModel response = new() { Success = false };

        try {
            Jornada? jornada = _db.Jornada.FirstOrDefault(j => j.Id == jornadaId);

            if (jornada == null) {
                return new ResponseModel { Message = "Jornada não encontrada." };
            }

            // Validations passed

            bool isJornadaActive = jornada.Deactivated == null;

            jornada.Deactivated = isJornadaActive ? TimeFunctions.HoraAtualBR() : null;

            _db.Jornada.Update(jornada);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Jornada desativada com sucesso";
        } catch (Exception ex) {
            response.Message = $"Falha ao desativar jornada: {ex}";
        }

        return response;
    }

    public List<MaterialModel> GetAllMaterialByJornada(int jornadaId)
    {
        List<Jornada_Material> listMaterial = _db.Jornada_Material
            .Where(m => m.Jornada_Id == jornadaId)
            .ToList();

        return _mapper.Map<List<MaterialModel>>(listMaterial);
    }

    public ResponseModel InsertMaterial(CreateJornadaMaterialRequest model)
    {
        ResponseModel response = new() { Success = false };

        try {
            Jornada? jornada = _db.Jornada.FirstOrDefault(j => j.Id == model.Jornada_Id);

            if (jornada == null) {
                return new ResponseModel { Message = "Jornada não encontrada" };
            }

            if (jornada.Deactivated.HasValue) {
                return new ResponseModel { Message = "Não é possível adicionar material em uma jornada desativada" };
            }

            // Validations passed

            Jornada_Material newMaterial = new() {
                FileName = model.FileName,
                FileBase64 = model.FileBase64,
                Jornada_Id = model.Jornada_Id,

                Deactivated = null,
                Account_Created_Id = _account.Id,
                Created = TimeFunctions.HoraAtualBR(),
            };

            _db.Jornada_Material.Add(newMaterial);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Material criado com sucesso";
            response.Object = _mapper.Map<MaterialModel>(newMaterial);
        } catch (Exception ex) {
            response.Message = $"Falha ao criar material: {ex}";
        }

        return response;
    }

    public ResponseModel ToggleDeactivateMaterial(int jornadaMaterialId)
    {
        ResponseModel response = new() { Success = false };

        try {
            Jornada_Material? material = _db.Jornada_Material.FirstOrDefault(jm => jm.Id == jornadaMaterialId);

            if (material == null) {
                return new ResponseModel { Message = "Material não encontrado." };
            }

            // Validations passed

            bool isMaterialActive = material.Deactivated == null;

            material.Deactivated = isMaterialActive ? TimeFunctions.HoraAtualBR() : null;

            _db.Jornada_Material.Update(material);
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
