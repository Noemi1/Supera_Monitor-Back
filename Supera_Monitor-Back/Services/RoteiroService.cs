using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Roteiro;

namespace Supera_Monitor_Back.Services;

public interface IRoteiroService {


    public RoteiroModel? Get(int roteiroId);
    List<RoteiroModel> GetAll();
    ResponseModel Insert(CreateRoteiroRequest model);
    ResponseModel Update(UpdateRoteiroRequest model);
    ResponseModel ToggleDeactivate(int roteiroId);
}

public class RoteiroService : IRoteiroService {
    private readonly DataContext _db;
    private readonly IMapper _mapper;
    private readonly Account? _account;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public RoteiroService(DataContext db, IMapper mapper, IHttpContextAccessor httpContextAccessor) {
        _db = db;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _account = (Account?)_httpContextAccessor?.HttpContext?.Items["Account"];
    }

    public RoteiroModel? Get(int roteiroId) {
        Roteiro? roteiro = _db.Roteiros.Find(roteiroId);

        if (roteiro is null) {
            return null;
        }

        return _mapper.Map<RoteiroModel>(roteiro);
    }

    public List<RoteiroModel> GetAll() {
        List<Roteiro> listRoteiros = _db.Roteiros
            .OrderBy(j => j.Semana)
            .ToList();

        return _mapper.Map<List<RoteiroModel>>(listRoteiros);
    }

    public ResponseModel Insert(CreateRoteiroRequest model) {
        ResponseModel response = new() { Success = false };

        try {
            if (model.DataFim <= model.DataInicio) {
                return new ResponseModel { Message = "Data de fim não pode ser anterior à data de início" };
            }

            // Não deve ser possível criar um roteiro em uma semana que já está associada a outro roteiro (nos roteiros ativos)
            var roteiroConflict = _db.Roteiros.FirstOrDefault(r =>
                r.Deactivated == null &&
                r.Semana == model.Semana);

            if (roteiroConflict is not null) {
                return new ResponseModel { Message = $"A semana {model.Semana} já possui um roteiro associado: '{roteiroConflict.Tema}'." };
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

			Roteiro roteiro = _mapper.Map<Roteiro>(model);
			roteiro.Account_Created_Id = _account!.Id;
			roteiro.Created = TimeFunctions.HoraAtualBR();

            _db.Roteiros.Add(roteiro);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Roteiro inserido com sucesso";
            response.Object = _mapper.Map<RoteiroModel>(roteiro);
        }
        catch (Exception ex) {
            response.Message = $"Falha ao inserir roteiro: {ex}";
        }

        return response;
    }

    public ResponseModel Update(UpdateRoteiroRequest model) {
        ResponseModel response = new() { Success = false };

        try {
            Roteiro? roteiro = _db.Roteiros.AsNoTracking().FirstOrDefault(r => r.Id == model.Id);
            if (roteiro == null) {
                return new ResponseModel { Message = "Roteiro não encontrado." };
            }

            if (model.DataFim <= model.DataInicio) {
                return new ResponseModel { Message = "Intervalo de roteiro inválido" };
            }

            // Não deve ser possível atualizar um roteiro em uma semana que já está associada a outro roteiro (nos roteiros ativos)
            var roteiroConflict = _db.Roteiros.FirstOrDefault(r =>
                r.Id != model.Id &&
                r.Deactivated == null &&
                r.Semana == model.Semana);

            if (roteiroConflict is not null) {
                return new ResponseModel { Message = $"A semana {model.Semana} já possui um roteiro associado: '{roteiroConflict.Tema}'." };
            }

			// Validations passed

			DateTime created = roteiro.Created;
			DateTime? deactivated = roteiro.Deactivated;
			int account_Created_Id = roteiro.Account_Created_Id;

			roteiro = _mapper.Map<Roteiro>(model);

			roteiro.Created = created;
			roteiro.Deactivated = deactivated;
			roteiro.Account_Created_Id = account_Created_Id;
			roteiro.LastUpdated = TimeFunctions.HoraAtualBR();



			_db.Roteiros.Update(roteiro);
            _db.SaveChanges();

            response.Success = true;
            response.Message = "Roteiro atualizado com sucesso";
            response.Object = _mapper.Map<RoteiroModel>(roteiro);
        }
        catch (Exception ex) {
            response.Message = $"Falha ao atualizar roteiro: {ex}";
        }

        return response;
    }

    public ResponseModel ToggleDeactivate(int roteiroId) {
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
        }
        catch (Exception ex) {
            response.Message = $"Falha ao desativar roteiro: {ex}";
        }

        return response;
    }

}
