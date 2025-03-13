using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Checklist;
using Supera_Monitor_Back.Services;
using System.Reflection;

namespace Supera_Monitor_Back.Controllers {
    [Authorize(Entities.Role.Admin, Entities.Role.Teacher, Entities.Role.Assistant)]
    [ApiController]
    [Route("back/[controller]")]
    public class ChecklistController : _BaseController {
        private readonly IChecklistService _checklistService;
        private readonly ILogService _logger;

        public ChecklistController(IChecklistService checklistService, ILogService logger)
        {
            _checklistService = checklistService;
            _logger = logger;
        }

        [HttpGet("all")]
        public ActionResult<List<ChecklistModel>> GetAll()
        {
            try {
                var response = _checklistService.GetAll();

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("all/{checklistId}")]
        public ActionResult<ResponseModel> GetAllByChecklistId(int checklistId)
        {
            try {
                var response = _checklistService.GetAllByChecklistId(checklistId);

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("all/aluno/{alunoId}")]
        public ActionResult<List<AlunoChecklistView>> GetAllByAlunoId(int alunoId)
        {
            try {
                var response = _checklistService.GetAllByAlunoId(alunoId);

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("all/aula/{aulaId}")]
        public ActionResult<List<ChecklistsFromAlunoModel>> GetAllAlunoChecklistsByAulaId(int aulaId)
        {
            try {
                var response = _checklistService.GetAllAlunoChecklistsByAulaId(aulaId);

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost()]
        public ActionResult<ResponseModel> Insert(CreateChecklistItemRequest model)
        {
            try {
                var response = _checklistService.Insert(model);

                if (response.Success) {
                    _logger.Log("Insert", "Checklist_Item", response.Object, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPut()]
        public ActionResult<ResponseModel> Update(UpdateChecklistItemRequest model)
        {
            try {
                var response = _checklistService.Update(model);

                if (response.Success) {
                    _logger.Log("Update", "Checklist_Item", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPatch("toggle-active/{checklistItemId}")]
        public ActionResult<ResponseModel> ToggleDeactivate(int checklistItemId)
        {
            try {
                var response = _checklistService.ToggleDeactivate(checklistItemId);

                if (response.Success) {
                    _logger.Log("ToggleDeactivate", "Checklist_Item", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("populate/{alunoId}")]
        public ActionResult<ResponseModel> PopulateAlunoChecklist(int alunoId)
        {
            try {
                var response = _checklistService.PopulateAlunoChecklist(alunoId);

                if (response.Success) {
                    _logger.Log("PopulateAlunoChecklist", "Aluno_Checklist_Item", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPatch("toggle-item/{alunoChecklistItemId}")]
        public ActionResult<ResponseModel> ToggleAlunoChecklistItem(int alunoChecklistItemId)
        {
            try {
                var response = _checklistService.ToggleAlunoChecklistItem(alunoChecklistItemId);

                if (response.Success) {
                    _logger.Log("PopulateAlunoChecklist", "Aluno_Checklist_Item", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }
    }
}
