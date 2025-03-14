using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Jornada;
using Supera_Monitor_Back.Services;
using System.Reflection;

namespace Supera_Monitor_Back.Controllers {
    [Authorize(Entities.Role.Admin, Entities.Role.Teacher, Entities.Role.Assistant)]
    [ApiController]
    [Route("back/[controller]")]
    public class JornadasController : _BaseController {
        private readonly IJornadaService _jornadaService;
        private readonly ILogService _logger;

        public JornadasController(IJornadaService jornadaService, ILogService logger)
        {
            _jornadaService = jornadaService;
            _logger = logger;
        }

        [HttpGet("all")]
        public ActionResult<List<JornadaModel>> GetAll()
        {
            try {
                var response = _jornadaService.GetAll();

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost()]
        public ActionResult<ResponseModel> Insert(CreateJornadaRequest model)
        {
            try {
                var response = _jornadaService.Insert(model);

                if (response.Success) {
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPut()]
        public ActionResult<ResponseModel> Update(UpdateJornadaRequest model)
        {
            try {
                var response = _jornadaService.Update(model);

                if (response.Success) {
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPatch("{jornadaId}")]
        public ActionResult<ResponseModel> ToggleDeactivate(int jornadaId)
        {
            try {
                var response = _jornadaService.ToggleDeactivate(jornadaId);

                if (response.Success) {
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("material/{jornadaMaterialId}")]
        public ActionResult<ResponseModel> GetAllMaterialByJornada(int jornadaMaterialId)
        {
            try {
                var response = _jornadaService.GetAllMaterialByJornada(jornadaMaterialId);

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("material")]
        public ActionResult<ResponseModel> InsertMaterial(CreateJornadaMaterialRequest model)
        {
            try {
                var response = _jornadaService.InsertMaterial(model);

                if (response.Success) {
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }


        [HttpPatch("material/{jornadaMaterialId}")]
        public ActionResult<ResponseModel> ToggleDeactivateMaterial(int jornadaMaterialId)
        {
            try {
                var response = _jornadaService.ToggleDeactivateMaterial(jornadaMaterialId);

                if (response.Success) {
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
