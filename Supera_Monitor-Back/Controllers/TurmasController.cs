using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Turma;
using Supera_Monitor_Back.Services;
using System.Reflection;

namespace Supera_Monitor_Back.Controllers {
    [Authorize(Entities.Role.Admin, Entities.Role.Teacher, Entities.Role.Assistant)]
    [ApiController]
    [Route("back/[controller]")]
    public class TurmasController : _BaseController {
        private readonly ITurmaService _turmaService;
        private readonly ILogService _logger;

        public TurmasController(ITurmaService turmaService, ILogService logService)
        {
            _turmaService = turmaService;
            _logger = logService;
        }

        [HttpGet("{turmaId}")]
        public ActionResult<TurmaList> Get(int turmaId)
        {
            try {
                var response = _turmaService.Get(turmaId);

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("all")]
        public ActionResult<List<TurmaList>> GetAll()
        {
            try {
                var response = _turmaService.GetAll();

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("types")]
        public ActionResult<List<TurmaTipoModel>> GetTypes()
        {
            try {
                throw new NotImplementedException();

                //var response = _turmaService.GetTypes();

                //return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost()]
        public ActionResult<ResponseModel> Insert(CreateTurmaRequest model)
        {
            try {
                var response = _turmaService.Insert(model);

                if (response.Success) {
                    int turmaId = response.Object!.Id;
                    _logger.Log("Insert", "Turma", response, Account?.Id);
                    return Created($"/turmas/{turmaId}", response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPut()]
        public ActionResult<ResponseModel> Update(UpdateTurmaRequest model)
        {
            try {
                var response = _turmaService.Update(model);

                if (response.Success) {
                    _logger.Log("Update", "Turma", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [Authorize(Entities.Role.Admin)]
        [HttpDelete("{turmaId}")]
        public ActionResult<ResponseModel> Delete(int turmaId)
        {
            try {
                var response = _turmaService.Delete(turmaId);

                if (response.Success) {
                    _logger.Log("Delete", "Turma", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response.Message);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPatch("toggle-active/{Id}")]
        public ActionResult<ResponseModel> ToggleDeactivate(int Id)
        {
            try {
                ResponseModel response = _turmaService.ToggleDeactivate(Id, GetIpAddressFromHeaders());

                if (response.Success) {
                    string action = response.Object!.Active ? "Enable" : "Disable";
                    _logger.Log(action, "Turma", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("{turmaId}/aulas/all-possibilities")]
        public ActionResult<List<AulaVisualizationModel>> GetAllPossibleAulasByTurma(int turmaId, [FromBody] AulaPossibilitiesRequest model)
        {
            try {
                //var response = _turmaService.GetAllPossibleAulasByTurma(turmaId, model.DateReference);

                //return Ok(response);
                throw new NotImplementedException();
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }
    }
}
