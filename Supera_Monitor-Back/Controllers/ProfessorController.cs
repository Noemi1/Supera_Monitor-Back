using Microsoft.AspNetCore.Mvc;
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
        public ActionResult Get(int professorId)
        {
            try {
                var response = _professorService.Get(professorId);
                throw new NotImplementedException();
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }

        [HttpGet("all")]
        public ActionResult GetAll()
        {
            try {
                var response = _professorService.GetAll();
                throw new NotImplementedException();
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }

        [HttpPost()]
        public ActionResult Insert(CreateProfessorRequest model)
        {
            try {
                var response = _professorService.Insert(model);
                throw new NotImplementedException();
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }

        [HttpPut()]
        public ActionResult Update(UpdateProfessorRequest model)
        {
            try {
                var response = _professorService.Update(model);
                throw new NotImplementedException();
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }

        [HttpDelete("{professorId}")]
        public ActionResult Delete(int professorId)
        {
            try {
                var response = _professorService.Delete(professorId);
                throw new NotImplementedException();
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }
    }
}
