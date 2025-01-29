using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Professor;
using Supera_Monitor_Back.Services;
using System.Reflection;

namespace Supera_Monitor_Back.Controllers {
    [Authorize(Entities.Role.Admin, Entities.Role.Teacher, Entities.Role.Assistant)]
    [ApiController]
    [Route("back/[controller]")]
    public class ProfessorController : _BaseController {
        private readonly IProfessorService _professorService;
        private readonly ILogService _logger;

        public ProfessorController(IProfessorService professorService, ILogService logger)
        {
            _professorService = professorService;
            _logger = logger;
        }

        [HttpGet("{professorId}")]
        public ActionResult<ProfessorResponse> Get(int professorId)
        {
            try {
                var response = _professorService.Get(professorId);

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }

        [HttpGet("all")]
        public ActionResult<List<ProfessorList>> GetAll()
        {
            try {
                var response = _professorService.GetAll();

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }

        [HttpPost()]
        public ActionResult<ResponseModel> Insert(CreateProfessorRequest model)
        {
            try {
                var response = _professorService.Insert(model);

                if (response.Success) {
                    var professorId = response.Object!.Id;
                    return Created($"/professor/{professorId}", professorId);
                }

                return BadRequest(response.Message);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }

        [HttpPut()]
        public ActionResult<ResponseModel> Update(UpdateProfessorRequest model)
        {
            try {
                var response = _professorService.Update(model);

                if (response.Success) {
                    return Ok(response);
                }

                return BadRequest(response.Message);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }

        [HttpDelete("{professorId}")]
        public ActionResult<ResponseModel> Delete(int professorId)
        {
            try {
                var response = _professorService.Delete(professorId);

                if (response.Success) {
                    return Ok(response);
                }

                return BadRequest(response.Message);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }
    }
}
