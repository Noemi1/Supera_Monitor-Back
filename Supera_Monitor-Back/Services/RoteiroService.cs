using AutoMapper;
using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Roteiro;
using System.Globalization;

namespace Supera_Monitor_Back.Services;

public interface IRoteiroService {
    public RoteiroModel? Get(int roteiroId);
    List<RoteiroModel> GetAll(int? ano);
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

    public List<RoteiroModel> GetAll(int? ano) {
		if (!ano.HasValue)
			ano = DateTime.Now.Year;

        List<Roteiro> list = _db.Roteiros
			.Where(x => x.DataInicio.Year == ano.Value || x.DataFim.Year == ano.Value)
			.OrderBy(x => x.Semana)
			.ToList();

		DateTime dataDe = new DateTime(ano.Value, 1, 1);
		DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
		int weeksInYear = dfi.Calendar.GetWeekOfYear(dataDe, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
		Roteiro[] roteirosArray = new Roteiro[weeksInYear];
		list.ForEach(roteiro =>
		{
			int week = dfi.Calendar.GetWeekOfYear(roteiro.DataInicio, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);

			if (roteiro.DataInicio.Year == ano.Value - 1)
				week = 1;
			else if (roteiro.DataFim.Year == ano.Value + 1)
				week = weeksInYear;

			roteirosArray[week - 1] = roteiro;
		});
		for (int index = 0; index < roteirosArray.Length; index++)
		{
			Roteiro roteiro = roteirosArray[index];

			// Se não tiver roteiro salva pseudo roteirosy
			if (roteiro == null)
			{
				int semana;
				DateTime dataInicio;
				DateTime dataFim;

				if (index == 0) // Se for a semana 0
				{
					semana = 0;
					dataInicio = dataDe.AddDays((int)dataDe.DayOfWeek - 5); // Segunda-feira
				}
				else
				{
					Roteiro lastRoteiro = roteirosArray[index - 1];
					DateTime domingo = lastRoteiro.DataInicio.AddDays(7 - (int)lastRoteiro.DataInicio.DayOfWeek);
					dataInicio = domingo.AddDays(1); // Segunda-feira
					semana = lastRoteiro.Semana + 1;
				}

				dataFim = dataInicio.AddDays(6 - (int)dataInicio.DayOfWeek);

				roteiro = new Roteiro
				{
					Id = -1,
					DataInicio = dataInicio,
					DataFim = dataFim,
					Semana = semana,
					Tema = "Tema Indefinido",
					CorLegenda = "#3f3f3f"
				};
				roteirosArray[index] = roteiro;

			}
		}


		return _mapper.Map<List<RoteiroModel>>(roteirosArray);
    }

    public ResponseModel Insert(CreateRoteiroRequest model) {
        ResponseModel response = new() { Success = false };

        try {
            if (model.DataFim <= model.DataInicio) {
                return new ResponseModel { Message = "Data de fim não pode ser anterior à data de início" };
            }

            // Verifica se o novo roteiro se sobrepõe a outro roteiro existente ativo
            // As condições cobrem os seguintes casos:
            // 1. A DataInicio do novo roteiro está dentro do intervalo de um roteiro existente.
            // 2. A DataFim do novo roteiro está dentro do intervalo de um roteiro existente.
            // 3. O novo roteiro engloba completamente outro roteiro já cadastrado.
            Roteiro? isDuringAnotherRoteiro = _db.Roteiros.FirstOrDefault(r =>
                r.Deactivated == null &&
                ((model.DataInicio >= r.DataInicio && model.DataInicio <= r.DataFim) || // DataInicio está dentro de outro roteiro
                 (model.DataFim >= r.DataInicio && model.DataFim <= r.DataFim) ||       // DataFim está dentro de outro roteiro
                 (model.DataInicio <= r.DataInicio && model.DataFim >= r.DataFim))      // Novo roteiro engloba outro roteiro
            );

            if (isDuringAnotherRoteiro != null) {
                return new ResponseModel { Message = $"A data do intervalo desse roteiro conflita com outro roteiro: Semana: ${isDuringAnotherRoteiro.Semana}, Tema: ${isDuringAnotherRoteiro.Tema}." };
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

            // Verifica se o novo roteiro se sobrepõe a outro roteiro existente ativo
            // As condições cobrem os seguintes casos:
            // 1. A DataInicio do novo roteiro está dentro do intervalo de um roteiro existente.
            // 2. A DataFim do novo roteiro está dentro do intervalo de um roteiro existente.
            // 3. O novo roteiro engloba completamente outro roteiro já cadastrado.
            Roteiro? isDuringAnotherRoteiro = _db.Roteiros.FirstOrDefault(r =>
                r.Deactivated == null &&
                r.Id != model.Id &&
                ((model.DataInicio >= r.DataInicio && model.DataInicio <= r.DataFim) || // DataInicio está dentro de outro roteiro
                 (model.DataFim >= r.DataInicio && model.DataFim <= r.DataFim) ||       // DataFim está dentro de outro roteiro
                 (model.DataInicio <= r.DataInicio && model.DataFim >= r.DataFim))      // Novo roteiro engloba outro roteiro
            );

            if (isDuringAnotherRoteiro != null) {
                return new ResponseModel { Message = $"A data do intervalo desse roteiro conflita com outro roteiro: Semana: ${isDuringAnotherRoteiro.Semana}, Tema: ${isDuringAnotherRoteiro.Tema}." };
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
