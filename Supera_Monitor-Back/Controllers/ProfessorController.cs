using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities;
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
        public ActionResult<ProfessorList> Get(int professorId)
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
                string origin = Request.Headers["origin"].ToString();

                var response = _professorService.Insert(model, origin);

                if (response.Success) {
                    string professorId = response.Object!.Id;
                    _logger.Log("Insert", "Professor", response, Account?.Id);
                    return Created($"/professor/{professorId}", response);
                }

                return BadRequest(response);
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
                    _logger.Log("Update", "Professor", response, Account?.Id);
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
                    _logger.Log("Delete", "Professor", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response.Message);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }

        [HttpGet("nivel/abaco/all")]
        public ActionResult<List<Professor_NivelAbaco>> GetAllNiveisAbaco()
        {
            try {
                var response = _professorService.GetAllNiveisAbaco();

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }

        [HttpGet("nivel/ah/all")]
        public ActionResult<List<Professor_NivelAH>> GetAllNiveisAh()
        {
            try {
                var response = _professorService.GetAllNiveisAh();

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }

        [HttpGet("apostilas/all")]
        public ActionResult<List<ApostilaList>> GetAllApostilas()
        {
            try {
                var response = _professorService.GetAllApostilas();

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }

        [HttpGet("kits/all")]
        public ActionResult<List<KitResponse>> GetAllKits()
        {
            try {
                var response = _professorService.GetAllKits();

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }
    }
}
