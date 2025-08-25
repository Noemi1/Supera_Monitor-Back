using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Roteiro;
using Supera_Monitor_Back.Services;

namespace Supera_Monitor_Back.Controllers {
    [Authorize(Entities.Role.Admin, Entities.Role.Teacher, Entities.Role.Assistant)]
    [ApiController]
    [Route("back/[controller]")]
    public class RoteirosController : _BaseController {
        private readonly IRoteiroService _roteiroService;
        private readonly ILogService _logger;

        public RoteirosController(IRoteiroService roteiroService, ILogService logger) {
            _roteiroService = roteiroService;
            _logger = logger;
        }

        [HttpGet("{roteiroId}")]
        public ActionResult<RoteiroModel?> Get(int roteiroId) {
            try {
                var response = _roteiroService.Get(roteiroId);

                if (response is null) {
                    return NotFound();
                }

                return Ok(response);
            }
            catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("all")]
        public ActionResult<List<RoteiroModel>> GetAll() {
            try {
                var response = _roteiroService.GetAll();

                return Ok(response);
            }
            catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost()]
        public ActionResult<ResponseModel> Insert(CreateRoteiroRequest model) {
            try {
                var response = _roteiroService.Insert(model);

                if (response.Success) {
                    return Ok(response);
                }

                return BadRequest(response);
            }
            catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPut()]
        public ActionResult<ResponseModel> Update(UpdateRoteiroRequest model) {
            try {
                var response = _roteiroService.Update(model);

                if (response.Success) {
                    return Ok(response);
                }

                return BadRequest(response);
            }
            catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPatch("{roteiroId}")]
        public ActionResult<ResponseModel> ToggleDeactivate(int roteiroId) {
            try {
                var response = _roteiroService.ToggleDeactivate(roteiroId);

                if (response.Success) {
                    return Ok(response);
                }

                return BadRequest(response);
            }
            catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }
	}
}
