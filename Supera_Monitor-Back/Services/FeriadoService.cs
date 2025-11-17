using AutoMapper;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Feriado;

namespace Supera_Monitor_Back.Services;

public interface IFeriadoService
{

	List<FeriadoList> GetList();
	FeriadoList Get(int id);
	ResponseModel Insert(InsertFeriadoRequest request);
	ResponseModel Update(UpdateFeriadoRequest request);
	ResponseModel Delete(int id);
	ResponseModel ToggleDeactivate(int id);
}

public class FeriadoService : IFeriadoService
{
	private readonly DataContext _db;
	private readonly IMapper _mapper;

	private readonly Account? _account;

	public FeriadoService(
		DataContext db,
		IMapper mapper,
		IHttpContextAccessor httpContextAccessor
	)
	{
		_db = db;
		_mapper = mapper;
		_account = (Account?)httpContextAccessor.HttpContext?.Items["Account"];
	}


	public List<FeriadoList> GetList()
	{
		return _db.FeriadoList.ToList();
	}

	public FeriadoList Get(int id)
	{
		var model = _db.FeriadoList.FirstOrDefault(x => x.Id == id);
		if (model == null)
			throw new Exception("Feriado não existe");

		return model;
	}
	public ResponseModel Insert(InsertFeriadoRequest request)
	{

		var response = new ResponseModel();

		var existe = _db.Feriado.FirstOrDefault(x => x.Data == request.Data);
		if (existe != null)
		{
			response.Message = "Um outro feriado já foi registrado para o mesmo dia";
			return response;
		}


		var feriado = new Feriado
		{
			Descricao = request.Descricao,
			Data = request.Data,
			Account_Created_Id = _account?.Id ?? 1,
			Created = TimeFunctions.HoraAtualBR(),
			Deactivated = null,
		};

		_db.Feriado.Add(feriado);
		_db.SaveChanges();


		response.Success = true;
		response.Object = Get(feriado.Id);
		response.Message = "Feriado inserido com sucesso.";

		return response;
	}
	public ResponseModel Update(UpdateFeriadoRequest request)
	{

		var response = new ResponseModel();

		var existe = _db.Feriado.FirstOrDefault(x => x.Data == request.Data && x.Id != request.Id);
		if (existe != null)
		{
			response.Message = "Um outro feriado já foi registrado para o mesmo dia";
			return response;
		}

		var feriado = _db.Feriado.FirstOrDefault(x => x.Id == request.Id);
		if (feriado == null)
		{
			response.Message = "Feriado não encontrado";
			return response;
		}
		feriado.Descricao = request.Descricao;
		feriado.Data = request.Data;


		_db.Feriado.Update(feriado);
		_db.SaveChanges();


		response.Success = true;
		response.Object = Get(feriado.Id);
		response.Message = "Feriado atualizado com sucesso.";

		return response;
	}

	public ResponseModel Delete(int id)
	{


		ResponseModel response = new() { Success = false };

		try
		{
			Feriado? feriado = _db.Feriado.FirstOrDefault(r => r.Id == id);

			if (feriado == null)
				return new ResponseModel { Message = "Feriado não encontrado." };

			var oldObject = this.Get(id);

			_db.Feriado.Remove(feriado);
			_db.SaveChanges();

			response.Success = true;
			response.Message = "Feriado excluido com sucesso";
			response.Object = oldObject;
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao desativar Feriado: {ex}";
		}

		return response;
	}


	public ResponseModel ToggleDeactivate(int id)
	{

		ResponseModel response = new() { Success = false };

		try
		{
			Feriado? feriado = _db.Feriado.FirstOrDefault(r => r.Id == id);

			if (feriado == null)
				return new ResponseModel { Message = "Feriado não encontrado." };

			// Validations passed

			bool isActive = feriado.Deactivated == null;

			feriado.Deactivated = isActive ? TimeFunctions.HoraAtualBR() : null;

			_db.Feriado.Update(feriado);
			_db.SaveChanges();

			response.Success = true;
			response.Message = "Feriado desativado com sucesso";
			response.Object = this.Get(feriado.Id);
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao desativar Feriado: {ex}";
		}

		return response;
	}
}
