using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Checklist;

namespace Supera_Monitor_Back.Services {
    public interface IChecklistService {
        List<ChecklistModel> GetAll();
        List<ChecklistItemModel> GetAllByChecklistId(int checklistId);
        List<AlunoChecklistView> GetAllByAlunoId(int alunoId);
        ResponseModel Insert(CreateChecklistItemRequest model);
        ResponseModel Update(UpdateChecklistItemRequest model);
        ResponseModel ToggleDeactivate(int checklistItemId);

        //List<ChecklistsFromAlunoModel> GetAllAlunoChecklistsByAulaId(int aulaId);
        ResponseModel PopulateAlunoChecklist(int alunoId);
        ResponseModel ToggleAlunoChecklistItem(int alunoId);
    }

    public class ChecklistService : IChecklistService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;
        private readonly Account? _account;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public ChecklistService(DataContext db, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _account = ( Account? )_httpContextAccessor?.HttpContext?.Items["Account"];
        }

        public List<ChecklistModel> GetAll()
        {
            List<Checklist> listChecklist = _db.Checklists
                .OrderBy(c => c.Ordem)
                .ToList();

            var response = _mapper.Map<List<ChecklistModel>>(listChecklist);

            foreach (ChecklistModel checklist in response) {
                checklist.Items = GetAllByChecklistId(checklist.Id);
            }

            return response;
        }

        public List<ChecklistItemModel> GetAllByChecklistId(int checklistId)
        {
            List<Checklist_Item> listChecklistItem = _db.Checklist_Items
                .Where(c =>
                    c.Checklist_Id == checklistId &&
                    c.Deactivated == null)
                .OrderBy(c => c.Ordem)
                .ToList();

            return _mapper.Map<List<ChecklistItemModel>>(listChecklistItem);
        }


        public List<AlunoChecklistView> GetAllByAlunoId(int alunoId)
        {
            List<AlunoChecklistView> listAlunoChecklistView = _db.AlunoChecklistViews
                .Where(c =>
                    c.Aluno_Id == alunoId)
                .OrderBy(c => c.Checklist_Id)
                .ThenBy(c => c.Ordem)
                .ToList();

            return listAlunoChecklistView;
        }

        public ResponseModel Insert(CreateChecklistItemRequest model)
        {
            ResponseModel response = new() { Success = false };

            try {
                bool checklistExists = _db.Checklists.Any(c => c.Id == model.Checklist_Id);

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

                _db.Checklist_Items.Add(newChecklistItem);
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
                Checklist_Item? checklistItem = _db.Checklist_Items.AsNoTracking().FirstOrDefault(c => c.Id == model.Id);

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
                Checklist_Item? checklistItem = _db.Checklist_Items.FirstOrDefault(ci => ci.Id == checklistItemId);

                if (checklistItem == null) {
                    return new ResponseModel { Message = "Item da checklist não encontrado." };
                }

                bool isItemActive = checklistItem.Deactivated == null;

                checklistItem.Deactivated = isItemActive ? TimeFunctions.HoraAtualBR() : null;

                _db.Checklist_Items.Update(checklistItem);
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
                Aluno? aluno = _db.Alunos.Find(alunoId);

                if (aluno is null) {
                    return new ResponseModel { Message = "Aluno não encontrado" };
                }

                // Se a checklist do aluno já está populada, não popular novamente
                if (_db.Aluno_Checklist_Items.Any(c => c.Id == aluno.Id)) {
                    return new ResponseModel { Message = "Aluno já possui itens na checklist, logo, não foi populado novamente" };
                }

                // Se o aluno não tiver uma data de início de vigencia, não será possível calcular os prazos
                if (aluno.DataInicioVigencia is null) {
                    return new ResponseModel { Message = "Aluno deve possuir uma data de início de vigência para calcular os prazos das atividades" };
                }

                // Buscar todos os itens da checklist não desativados
                List<Checklist_Item> checklistItems = _db.Checklist_Items.Where(c => c.Deactivated == null).ToList();

                List<Aluno_Checklist_Item> alunoChecklist = new();

                List<Checklist> checklists = _db.Checklists.ToList();

                // Para cada um, adicionar na checklist do aluno
                foreach (Checklist_Item item in checklistItems) {
                    // Obtém o número da semana da tarefa, padrão 0 se não encontrado
                    int semana = checklists.FirstOrDefault(c => c.Id == item.Checklist_Id)?.NumeroSemana ?? 0;

                    // Obtém a data de início do aluno
                    DateTime dataInicio = ( DateTime )aluno.DataInicioVigencia;

                    // Calcula quantos dias faltam para o próximo domingo após a data de início
                    int diasAteDomingo = (( int )DayOfWeek.Sunday - ( int )dataInicio.DayOfWeek + 7) % 7;

                    // Determina o primeiro domingo a partir da data de início
                    DateTime primeiroDomingo = dataInicio.AddDays(diasAteDomingo);

                    // Calcula o prazo final da tarefa (domingo da semana correspondente)
                    DateTime prazo = primeiroDomingo.AddDays(7 * (semana + 1));

                    // Adiciona o item à checklist do aluno
                    alunoChecklist.Add(new() {
                        Aluno_Id = aluno.Id,
                        Prazo = prazo,
                        Checklist_Item_Id = item.Id,
                        DataFinalizacao = null,
                        Account_Finalizacao_Id = null,
                    });
                }

                _db.Aluno_Checklist_Items.AddRange(alunoChecklist);
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
                Aluno_Checklist_Item? alunoChecklistItem = _db.Aluno_Checklist_Items.Find(alunoChecklistItemId);

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

                _db.Aluno_Checklist_Items.Update(alunoChecklistItem);
                _db.SaveChanges();

                response.Success = true;
                response.Message = "Item da checklist foi atualizado com sucesso";
                response.Object = _mapper.Map<AlunoChecklistItemModel>(alunoChecklistItem);
            } catch (Exception ex) {
                response.Message = "Falha ao popular checklist do aluno: " + ex.ToString();
            }

            return response;
        }

        public List<ChecklistsFromAlunoModel> GetAllAlunoChecklistsByAulaId(int aulaId)
        {
            // Coletar lista de registros e os alunos que tem esses registros previamente p/ reduzir o número de chamadas ao banco
            List<Evento_Participacao_Aluno> registros = _db.Evento_Participacao_Alunos.Where(p => p.Evento_Id == aulaId && p.Deactivated == null).ToList();

            List<int> alunoIds = registros.Select(r => r.Aluno_Id).ToList();
            List<AlunoList> alunos = _db.AlunoLists.Where(a => alunoIds.Contains(a.Id)).ToList();

            // Montar o objeto de retorno
            List<ChecklistsFromAlunoModel> response = new();

            foreach (Evento_Participacao_Aluno registro in registros) {
                var aluno = alunos.FirstOrDefault(a => a.Id == registro.Aluno_Id);

                List<AlunoChecklistView> alunoChecklists = GetAllByAlunoId(registro.Aluno_Id);

                response.Add(new() {
                    Aluno_Id = registro.Aluno_Id,
                    Checklist = alunoChecklists
                });
            }

            return response;
        }
    }
}
