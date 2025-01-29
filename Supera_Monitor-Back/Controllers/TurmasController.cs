using Microsoft.AspNetCore.Mvc;
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
        public ActionResult<TurmaResponse> Get(int turmaId)
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
        public ActionResult GetAll()
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
                var response = _turmaService.GetTypes();

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost()]
        public ActionResult Insert(CreateTurmaRequest model)
        {
            try {
                var response = _turmaService.Insert(model);

                if (response.Success) {
                    _logger.Log("Insert", "Turma", response, Account?.Id);
                    return Created($"/turmas/all", response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPut()]
        public ActionResult Update(UpdateTurmaRequest model)
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
        public ActionResult Delete(int turmaId)
        {
            try {
                var response = _turmaService.Delete(turmaId);

                if (response.Success) {
                    return Ok(response);
                }

                return BadRequest(response.Message);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        //[HttpGet("aulas/presenca")]
        //public ActionResult InsertPresenca(RegisterPresencaRequest model)
        //{
        //    try {
        //        var response = _turmaService.InsertPresenca(model);

        //        if (response.Success) {
        //            return Ok(response);
        //        }

        //        return BadRequest(response);
        //    } catch (Exception e) {
        //        _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
        //        return StatusCode(500, e);
        //    }
        //}
    }
}
