using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Sala;
using Supera_Monitor_Back.Services;
using System.Reflection;

namespace Supera_Monitor_Back.Controllers {
    [Authorize(Entities.Role.Admin, Entities.Role.Teacher, Entities.Role.Assistant)]
    [ApiController]
    [Route("back/[controller]")]
    public class SalasController : _BaseController {
        private readonly ISalaService _salaService;
        private readonly ILogService _logger;

        public SalasController(ISalaService salaService, ILogService logger)
        {
            _salaService = salaService;
            _logger = logger;
        }

        [HttpGet("all")]
        public ActionResult<ResponseModel> GetAllSalas()
        {
            try {
                var response = _salaService.GetAllSalas();

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost()]
        public ActionResult<ResponseModel> Insert(CreateSalaRequest model)
        {
            try {
                var response = _salaService.Insert(model);

                if (response.Success) {
                    _logger.Log("Insert", "Sala", response.Object, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPut()]
        public ActionResult<ResponseModel> Promote(UpdateSalaRequest model)
        {
            try {
                var response = _salaService.Update(model);

                if (response.Success) {
                    _logger.Log("Update", "Sala", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpDelete("{salaId}")]
        public ActionResult<ResponseModel> Delete(int salaId)
        {
            try {
                var response = _salaService.Delete(salaId);

                if (response.Success) {
                    _logger.Log("Delete", "Sala", response, Account?.Id);
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
