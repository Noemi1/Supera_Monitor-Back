using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Checklist;

namespace Supera_Monitor_Back.Services {
    public interface IChecklistService {
        List<ChecklistItemModel> GetAllByChecklistId(int checklistId);
        ResponseModel Insert(CreateChecklistItemRequest model);
        ResponseModel Update(UpdateChecklistItemRequest model);
        ResponseModel ToggleDeactivate(int checklistItemId);

        ResponseModel PopulateAlunoChecklist(int alunoId);
        ResponseModel ToggleAlunoChecklistItem(int alunoId);
    }

    public class ChecklistService : IChecklistService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;
        private readonly Account? _account;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChecklistService(DataContext db, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _account = ( Account? )httpContextAccessor?.HttpContext?.Items["Account"];
        }

        public List<ChecklistItemModel> GetAllByChecklistId(int checklistId)
        {
            List<Checklist_Item> listChecklistItem = _db.Checklist_Item
                .Where(c =>
                    c.Checklist_Id == checklistId &&
                    c.Deactivated == null)
                .OrderBy(c => c.Ordem)
                .ToList();

            return _mapper.Map<List<ChecklistItemModel>>(listChecklistItem);
        }

        public ResponseModel Insert(CreateChecklistItemRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                bool checklistExists = _db.Checklist.Any(c => c.Id == model.Checklist_Id);

                if (checklistExists == false) {
                    return new ResponseModel { Message = "Checklist não encontrada" };
                }

                if (string.IsNullOrEmpty(model.Nome)) {
                    return new ResponseModel { Message = "Item da checklist deve possuir um nome" };
                }

                Checklist_Item newChecklistItem = new() {
                    Nome = model.Nome,
                    Ordem = model.Ordem,
                    Checklist_Id = model.Checklist_Id,
                    Deactivated = null
                };

                _db.Checklist_Item.Add(newChecklistItem);
                _db.SaveChanges();

                response.Success = true;
                response.Message = "Item da checklist criado com sucesso";
                response.Object = _mapper.Map<ChecklistItemModel>(newChecklistItem);
            } catch (Exception ex) {
                response.Message = "Falha ao inserir item da checklist: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel Update(UpdateChecklistItemRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                Checklist_Item? checklistItem = _db.Checklist_Item.AsNoTracking().FirstOrDefault(c => c.Id == model.Id);

                if (checklistItem is null) {
                    return new ResponseModel { Success = false, Message = "Item da checklist não encontrado" };
                }

                if (checklistItem.Deactivated.HasValue) {
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
            } catch (Exception ex) {
                response.Message = "Falha ao atualizar item da checklist: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel ToggleDeactivate(int checklistItemId)
        {
            ResponseModel response = new() { Success = false };

            try {
                Checklist_Item? checklistItem = _db.Checklist_Item.FirstOrDefault(ci => ci.Id == checklistItemId);

                if (checklistItem == null) {
                    return new ResponseModel { Message = "Item da checklist não encontrado." };
                }

                bool isItemActive = checklistItem.Deactivated == null;

                checklistItem.Deactivated = isItemActive ? TimeFunctions.HoraAtualBR() : null;

                _db.Checklist_Item.Update(checklistItem);
                _db.SaveChanges();

                response.Success = true;
                response.Message = "Item da checklist foi ativado/desativado com sucesso";
                response.Object = _mapper.Map<ChecklistItemModel>(checklistItem);
            } catch (Exception ex) {
                response.Message = "Falha ao ativar/desativar item da checklist: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel PopulateAlunoChecklist(int alunoId)
        {
            ResponseModel response = new() { Success = false };

            try {
                Aluno? aluno = _db.Aluno.Find(alunoId);

                if (aluno is null) {
                    return new ResponseModel { Message = "Aluno não encontrado" };
                }

                // Se a checklist do aluno já está populada, não popular novamente
                if (_db.Aluno_Checklist_Item.Any(c => c.Id == aluno.Id)) {
                    return new ResponseModel { Message = "Aluno já possui itens na checklist, logo, não foi populado novamente" };
                }

                // Buscar todos os itens da checklist não desativados
                List<Checklist_Item> checklistItems = _db.Checklist_Item.Where(c => c.Deactivated == null).ToList();

                List<Aluno_Checklist_Item> alunoChecklist = new();

                // Para cada um, adicionar na checklist do aluno
                foreach (Checklist_Item item in checklistItems) {
                    alunoChecklist.Add(new() {
                        Aluno_Id = aluno.Id,
                        Prazo = aluno.DataInicioVigencia.HasValue ? aluno.DataInicioVigencia.Value.AddDays(90) : null,
                        Checklist_Item_Id = item.Id,
                        DataFinalizacao = null,
                        Account_Finalizacao_Id = null,
                    });
                }

                _db.Aluno_Checklist_Item.AddRange(alunoChecklist);
                _db.SaveChanges();

                response.Success = true;
                response.Message = "Itens da checklist do aluno foram populados com sucesso";
            } catch (Exception ex) {
                response.Message = "Falha ao popular checklist do aluno: " + ex.ToString();
            }

            return response;
        }

        public ResponseModel ToggleAlunoChecklistItem(int alunoChecklistItemId)
        {
            ResponseModel response = new() { Success = false };

            try {
                Aluno_Checklist_Item? alunoChecklistItem = _db.Aluno_Checklist_Item.Find(alunoChecklistItemId);

                if (alunoChecklistItem is null) {
                    return new ResponseModel { Message = "Item da checklist do aluno não encontrado" };
                }

                // É um toggle, então:
                //  Se está finalizado, deve tirar os dados de finalização
                //  Se não está finalizado, deve adicionar dados de finalização
                if (alunoChecklistItem.Account_Finalizacao_Id.HasValue) {
                    alunoChecklistItem.DataFinalizacao = null;
                    alunoChecklistItem.Account_Finalizacao_Id = null;
                } else {
                    alunoChecklistItem.DataFinalizacao = TimeFunctions.HoraAtualBR();
                    alunoChecklistItem.Account_Finalizacao_Id = _account.Id;
                }

                _db.Aluno_Checklist_Item.Update(alunoChecklistItem);
                _db.SaveChanges();

                response.Success = true;
                response.Message = "Item da checklist foi atualizado com sucesso";
                response.Object = _mapper.Map<AlunoChecklistItemModel>(alunoChecklistItem);
            } catch (Exception ex) {
                response.Message = "Falha ao popular checklist do aluno: " + ex.ToString();
            }

            return response;
        }
    }
}
