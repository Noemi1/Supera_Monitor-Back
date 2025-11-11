using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Checklist;

namespace Supera_Monitor_Back.Services;

public interface IChecklistService
{
	List<ChecklistModel> GetAll();
	List<AlunoChecklistView> GetAllByAlunoId(int alunoId);
	List<ChecklistItemModel> GetAllByChecklistId(int checklistId);
	List<ChecklistsFromAlunoModel> GetAllAlunoChecklistsByEventoId(int eventoId);
	ResponseModel Insert(CreateChecklistItemRequest model);
	ResponseModel PopulateAlunoChecklist(int alunoId);
	ResponseModel Update(UpdateChecklistItemRequest model);
	ResponseModel ToggleDeactivate(int checklistItemId);
	ResponseModel ToggleAlunoChecklistItem(ToggleAlunoChecklistRequest model);
}

public class ChecklistService : IChecklistService
{
	private readonly DataContext _db;
	private readonly IMapper _mapper;
	private readonly Account? _account;

	public ChecklistService(DataContext db, IMapper mapper, IHttpContextAccessor httpContextAccessor)
	{
		_db = db;
		_mapper = mapper;
		_account = (Account?)httpContextAccessor?.HttpContext?.Items["Account"];
	}

	public List<ChecklistModel> GetAll()
	{
		var listChecklist = _db.Checklists
			.OrderBy(c => c.Ordem)
			.ToList();

		var response = _mapper.Map<List<ChecklistModel>>(listChecklist);

		foreach (ChecklistModel checklist in response)
		{
			checklist.Items = GetAllByChecklistId(checklist.Id);
		}

		return response;
	}

	public List<ChecklistItemModel> GetAllByChecklistId(int checklistId)
	{
		var listChecklistItem = _db.Checklist_Items
			.Where(c =>
				c.Checklist_Id == checklistId &&
				c.Deactivated == null)
			.OrderBy(c => c.Ordem)
			.ToList();

		return _mapper.Map<List<ChecklistItemModel>>(listChecklistItem);
	}

	public List<AlunoChecklistView> GetAllByAlunoId(int alunoId)
	{
		var listAlunoChecklistView = _db.AlunoChecklistViews
			.Where(c => c.Aluno_Id == alunoId)
			.OrderBy(c => c.Checklist_Id)
			.ThenBy(c => c.Ordem)
			.ToList();

		return listAlunoChecklistView;
	}

	public ResponseModel Insert(CreateChecklistItemRequest model)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			bool checklistExists = _db.Checklists.Any(c => c.Id == model.Checklist_Id);

			if (checklistExists == false)
			{
				return new ResponseModel { Message = "Checklist não encontrada" };
			}

			if (string.IsNullOrEmpty(model.Nome))
			{
				return new ResponseModel { Message = "Item da checklist deve possuir um nome" };
			}

			Checklist_Item newChecklistItem = new()
			{
				Nome = model.Nome,
				Ordem = model.Ordem,
				Checklist_Id = model.Checklist_Id,
				Deactivated = null
			};

			_db.Checklist_Items.Add(newChecklistItem);
			_db.SaveChanges();

			response.Success = true;
			response.Message = "Item da checklist criado com sucesso";
			response.Object = _mapper.Map<ChecklistItemModel>(newChecklistItem);
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao inserir item da checklist: {ex}";
		}

		return response;
	}

	public ResponseModel Update(UpdateChecklistItemRequest model)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Checklist_Item? checklistItem = _db.Checklist_Items.AsNoTracking().FirstOrDefault(c => c.Id == model.Id);

			if (checklistItem is null)
			{
				return new ResponseModel { Message = "Item da checklist não encontrado" };
			}

			if (checklistItem.Deactivated.HasValue)
			{
				return new ResponseModel { Message = "Este item está inativo" };
			}

			response.OldObject = _mapper.Map<ChecklistItemModel>(checklistItem);

			checklistItem.Nome = model.Nome;
			checklistItem.Ordem = model.Ordem;

			_db.Update(checklistItem);
			_db.SaveChanges();

			response.Success = true;
			response.Message = "Item da checklist atualizado com sucesso";
			response.Object = _mapper.Map<ChecklistItemModel>(checklistItem);
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao atualizar item da checklist: {ex}";
		}

		return response;
	}

	public ResponseModel ToggleDeactivate(int checklistItemId)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Checklist_Item? checklistItem = _db.Checklist_Items.FirstOrDefault(ci => ci.Id == checklistItemId);

			if (checklistItem == null)
			{
				return new ResponseModel { Message = "Item da checklist não encontrado." };
			}

			bool isItemActive = checklistItem.Deactivated == null;

			checklistItem.Deactivated = isItemActive ? TimeFunctions.HoraAtualBR() : null;

			_db.Checklist_Items.Update(checklistItem);
			_db.SaveChanges();

			response.Success = true;
			response.Message = "Item da checklist foi ativado/desativado com sucesso";
			response.Object = _mapper.Map<ChecklistItemModel>(checklistItem);
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao ativar/desativar item da checklist: {ex}";
		}

		return response;
	}

	public ResponseModel PopulateAlunoChecklist(int alunoId)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Aluno? aluno = _db.Alunos
				.Include(x => x.Aluno_Checklist_Items)
				.FirstOrDefault(x => x.Id == alunoId);

			if (aluno is null)
			{
				return new ResponseModel { Message = "Aluno não encontrado" };
			}

			// Buscar todos os itens da checklist não desativados
			var checklistItems = _db.Checklist_Items
				.Where(c => c.Deactivated == null)
				.ToList();

			var checklists = _db.Checklists.ToList();

			List<Aluno_Checklist_Item> alunoChecklist = [];


			var alunoChecklistItemPorId = aluno.Aluno_Checklist_Items
				.ToDictionary(x => x.Checklist_Item_Id, x => x);

			var checklistPorId = checklists
				.ToDictionary(x => x.Id, x => x);


			// Para cada um, adicionar na checklist do aluno
			foreach (Checklist_Item item in checklistItems)
			{
				//Aluno_Checklist_Item? existe = aluno.Aluno_Checklist_Items
				//	.FirstOrDefault(x => x.Checklist_Item_Id == item.Id);

				if (!alunoChecklistItemPorId.TryGetValue(item.Id, out var existe))
				{
					var checklist = checklistPorId.GetValueOrDefault(item.Checklist_Id, new Checklist());

					int semana = checklist.NumeroSemana;

					// Obtém a data de início do aluno
					DateTime dataInicio = aluno.Created;

					//// Calcula quantos dias faltam para o próximo domingo após a data de início
					//int diasAteDomingo = ((int)DayOfWeek.Sunday - (int)dataInicio.DayOfWeek + 7) % 7;

					//// Determina a primeira segunda a partir da data de início
					//DateTime primeiraSegunda = dataInicio.AddDays(diasAteDomingo + 1);

					// Calcula o prazo final da tarefa (segunda da semana correspondente)
					DateTime prazo = dataInicio.AddDays(7 * (semana + 1));

					// Adiciona o item à checklist do aluno
					alunoChecklist.Add(new Aluno_Checklist_Item()
					{
						Aluno_Id = aluno.Id,
						Prazo = prazo,
						Checklist_Item_Id = item.Id,
						DataFinalizacao = null,
						Account_Finalizacao_Id = null,
					});
				}
			}

			_db.Aluno_Checklist_Items.AddRange(alunoChecklist);
			_db.SaveChanges();

			response.Success = true;
			response.Message = "Itens da checklist do aluno foram populados com sucesso";
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao popular checklist do aluno: {ex}";
		}

		return response;
	}

	public ResponseModel ToggleAlunoChecklistItem(ToggleAlunoChecklistRequest model)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			Aluno_Checklist_Item? item = _db.Aluno_Checklist_Items.Find(model.Aluno_Checklist_Item_Id);

			if (item is null)
			{
				return new ResponseModel { Message = "Item da checklist do aluno não encontrado" };
			}

			if (item.DataFinalizacao is null)
			{
				item.Account_Finalizacao_Id = _account?.Id;
				item.DataFinalizacao = TimeFunctions.HoraAtualBR();
			}

			item.Observacoes = model.Observacoes ?? item.Observacoes;

			_db.Aluno_Checklist_Items.Update(item);
			_db.SaveChanges();

			response.Success = true;
			response.Message = "Item da checklist do aluno foi atualizado com sucesso";
			response.Object = _mapper.Map<AlunoChecklistItemModel>(item);
		}
		catch (Exception ex)
		{
			response.Message = $"Falha ao atualizar checklist item do aluno: {ex}";
		}

		return response;
	}

	public List<ChecklistsFromAlunoModel> GetAllAlunoChecklistsByEventoId(int eventoId)
	{
		var registros = _db.Evento_Participacao_Aluno
			.Where(p => p.Evento_Id == eventoId && p.Deactivated == null)
			.ToList();

		// Montar o objeto de retorno
		List<ChecklistsFromAlunoModel> response = [];

		foreach (Evento_Participacao_Aluno registro in registros)
		{
			List<AlunoChecklistView> alunoChecklists = GetAllByAlunoId(registro.Aluno_Id);

			response.Add(new ChecklistsFromAlunoModel { Aluno_Id = registro.Aluno_Id, Checklist = alunoChecklists });
		}

		return response;
	}
}
