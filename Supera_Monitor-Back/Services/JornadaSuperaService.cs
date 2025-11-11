using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models.JornadaSupera;
using Supera_Monitor_Back.Models.JornadaSupera.Card;
using Supera_Monitor_Back.Models.JornadaSupera.List;

namespace Supera_Monitor_Back.Services;

public interface IJornadaSuperaService
{

	List<JornadaSupera_Card_Checklist> GetCards(JornadaSupera_Request request);

	IEnumerable<JornadaSupera_List_Aluno> GetList(JornadaSupera_Request request);

}

public class JornadaSuperaService : IJornadaSuperaService
{
	private readonly DataContext _db;
	private readonly IMapper _mapper;
	private readonly Account? _account;

	public JornadaSuperaService(
		DataContext db,
		IMapper mapper,
		IHttpContextAccessor httpContextAccessor
	)
	{
		_db = db;
		_mapper = mapper;
		_account = (Account?)httpContextAccessor?.HttpContext?.Items["Account"];
	}


	public List<JornadaSupera_Card_Checklist> GetCards(JornadaSupera_Request request)
	{
		var response = new List<JornadaSupera_Card_Checklist>() { };

		//
		// Queryable
		//
		var alunosQueryable = _db.AlunoList
			.Where(x => x.Active == true)
			.AsNoTracking();

		var alunosChecklistItemsQueryable = _db.Aluno_Checklist_Item
			.Where(x => x.DataFinalizacao == null)
			.AsQueryable()
			.AsNoTracking();

		var itemsQueryable = _db.Checklist_Items
			.Where(x => x.Deactivated == null)
			.AsNoTracking();

		var checklistQueryable = _db.Checklists
			.AsNoTracking();

		if (request.Aluno_Id.HasValue)
		{
			alunosQueryable = alunosQueryable.Where(x => x.Id == request.Aluno_Id);
			alunosChecklistItemsQueryable = alunosChecklistItemsQueryable.Where(x => x.Aluno_Id == request.Aluno_Id);
		}

		if (request.Turma_Id.HasValue)
		{
			alunosQueryable = alunosQueryable.Where(x => x.Turma_Id == request.Turma_Id);
		}

		if (request.Professor_Id.HasValue)
		{
			alunosQueryable = alunosQueryable.Where(x => x.Professor_Id == request.Professor_Id);
		}
		//
		// Materialização
		//
		var alunos = alunosQueryable
			.ToList();

		var alunosIds = alunos
			.Select(x => x.Id)
			.ToHashSet();

		var alunosChecklistItems = alunosChecklistItemsQueryable
				//.Where(x => alunosIds.Contains(x.Aluno_Id))
			.Join(alunosQueryable, x => x.Aluno_Id, a => a.Id, (x, a) => x)
			.ToList();

		var items = itemsQueryable
			.ToList();

		var checklists = checklistQueryable
			.ToList();

		var accountIds = alunosChecklistItems
			.Where(x => x.Account_Finalizacao_Id.HasValue)
			.Select(x => x.Account_Finalizacao_Id)
			.ToHashSet();

		var accounts = _db.Accounts
			.Where(x => accountIds.Contains(x.Id))
			.AsNoTracking()
			.ToList();

		//
		// Dicionarios
		//

		var itemsPorChecklistId = items
			.ToLookup(x => x.Checklist_Id);

		var alunosPorChecklistItemId = alunosChecklistItems
			.ToLookup(x => x.Checklist_Item_Id);

		var alunosPorId = alunos
			.ToDictionary(x => x.Id, x => x);

		var accountsPorId = accounts
			.ToDictionary(x => x.Id, x => x);


		// 
		// Mapeia os checklists
		//
		foreach(var checklist in checklists)
		{
			var jornadaChecklist = new JornadaSupera_Card_Checklist()
			{
				Id = checklist.Id,
				Ordem = checklist.Ordem,
				NumeroSemana = checklist.NumeroSemana,
				Nome = checklist.Nome,
			};

			var itemsPorChecklist = itemsPorChecklistId[checklist.Id]
									.OrderBy(x => x.Ordem)
									.ToList();

			foreach (Checklist_Item item in itemsPorChecklist)
			{

				var jornadaItem = new JornadaSupera_Card_Checklist_Item()
				{
					Id = item.Id,
					Checklist_Id = item.Checklist_Id,
					Ordem = item.Ordem,
					Nome = item.Nome,
				};

				var alunosItems = alunosPorChecklistItemId[item.Id];

				foreach (Aluno_Checklist_Item alunoItem in alunosItems)
				{
					if (alunosPorId.TryGetValue(alunoItem.Aluno_Id, out var aluno))
					{
						Account? account = null;
						if (alunoItem.Account_Finalizacao_Id.HasValue)
							accountsPorId.TryGetValue(alunoItem.Account_Finalizacao_Id.Value, out account);


						var jornadaAluno = new JornadaSupera_Card_Checklist_Item_Aluno()
						{
							Id = alunoItem.Id,
							Aluno_Id = alunoItem.Aluno_Id,
							Aluno = aluno.Nome,
							Turma_Id = aluno.Turma_Id,
							Turma = aluno.Turma,
							CorLegenda = aluno.CorLegenda,
							Celular = aluno.Celular,
							NumeroSemana = checklist.NumeroSemana,
							Prazo = alunoItem.Prazo,
							DataFinalizacao = alunoItem.DataFinalizacao,
							Account = account?.Name,
							Account_Id = account?.Id,
							Observacoes = alunoItem.Observacoes
						};

						if (request.PendentesSemana == true && jornadaAluno.Status == StatusChecklistItem.EmAndamento)
						{
							jornadaItem.Alunos.Add(jornadaAluno);
						}
						else if (request.PendentesSemana == false)
						{
							jornadaItem.Alunos.Add(jornadaAluno);
						}

					}
				}
				jornadaItem.Alunos = jornadaItem.Alunos
					.OrderBy(x => x.Status)
					.ThenBy(x => x.Aluno)
					.ToList();
				jornadaChecklist.Items.Add(jornadaItem);
			}

				response.Add(jornadaChecklist);
		}

		response = response
			.OrderBy(x => x.Ordem)
			.ToList();

		return response;
	}

	public IEnumerable<JornadaSupera_List_Aluno> GetList(JornadaSupera_Request request)
	{
		var response = new List<JornadaSupera_List_Aluno>() { };

		//
		// Queryable
		//
		var alunosQueryable = _db.AlunoList
			.Where(x => x.Active == true)
			.AsNoTracking();

		var alunosChecklistItemsQueryable = _db.Aluno_Checklist_Item
			.OrderByDescending(x => x.Prazo)
			.AsNoTracking();

		var checklistQueryable = _db.Checklists
			.OrderBy(x => x.Ordem)
			.AsNoTracking();

		if (request.Aluno_Id.HasValue)
		{
			alunosQueryable = alunosQueryable.Where(x => x.Id == request.Aluno_Id);
			alunosChecklistItemsQueryable = alunosChecklistItemsQueryable.Where(x => x.Aluno_Id == request.Aluno_Id);
		}

		if (request.Turma_Id.HasValue)
		{
			alunosQueryable = alunosQueryable.Where(x => x.Turma_Id == request.Turma_Id);
		}

		if (request.Professor_Id.HasValue)
		{
			alunosQueryable = alunosQueryable.Where(x => x.Professor_Id == request.Professor_Id);
		}

		//
		// Materialização
		//
		var alunos = alunosQueryable
			.ToList();

		var alunosIds = alunos
			.Select(x => x.Id)
			.ToHashSet();

		var alunosChecklistItems = alunosChecklistItemsQueryable
			.Where(x => alunosIds.Contains(x.Aluno_Id))
			.Include(x => x.Checklist_Item)
			.Include(x => x.Account_Finalizacao)
			.ToList();

		var checklists = checklistQueryable
			.ToList();


		//
		// Dicionarios
		//

		var alunosPorChecklist = alunosChecklistItems
			.GroupBy(x => (x.Aluno_Id, x.Checklist_Item.Checklist_Id))
			.ToDictionary(x => (x.Key.Aluno_Id, x.Key.Checklist_Id), x => x.ToList());

		// 
		// Mapeia os alunos
		//
		foreach(var aluno in alunos)
		{
			var jornadaAluno = new JornadaSupera_List_Aluno()
			{
				Id = aluno.Id,
				Nome = aluno.Nome,
				Celular = aluno.Celular,
				Turma_Id = aluno.Turma_Id,
				Turma = aluno.Turma,
				CorLegenda = aluno.CorLegenda,
			};

			foreach (Checklist checklist in checklists)
			{
				var jornadaChecklist = new JornadaSupera_List_Checklist()
				{
					Id = checklist.Id,
					Nome = checklist.Nome,
					Ordem = checklist.Ordem,
					NumeroSemana = checklist.NumeroSemana,
				};


				if (alunosPorChecklist.TryGetValue((aluno.Id, checklist.Id), out var itemsDoChecklist))
				{
					// lista é List<Aluno_Checklist_Item>
					var jornadaChecklistItemAlunos = itemsDoChecklist.Select(item => new JornadaSupera_List_Checklist_Item_Aluno()
					{
						Id = item.Id,
						Checklist_Item_Id = item.Checklist_Item_Id,
						Checklist_Item = item.Checklist_Item.Nome,
						Aluno_Id = item.Aluno_Id,
						NumeroSemana = checklist.NumeroSemana,
						Prazo = item.Prazo,
						DataFinalizacao = item.DataFinalizacao,
						Account = item.Account_Finalizacao?.Name,
						Account_Id = item.Account_Finalizacao_Id,
						Observacoes = item.Observacoes,
					}).ToList();
					jornadaChecklist.Items = jornadaChecklistItemAlunos;
				}

				jornadaAluno.Checklists.Add(jornadaChecklist);

				var a = jornadaChecklist.Status;
			}

			response.Add(jornadaAluno);
		}

		return response;
	}
}
