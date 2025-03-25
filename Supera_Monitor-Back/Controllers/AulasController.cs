using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aula;
using Supera_Monitor_Back.Services;
using System.Reflection;

namespace Supera_Monitor_Back.Controllers {
    [Authorize(Entities.Role.Admin, Entities.Role.Teacher, Entities.Role.Assistant)]
    [ApiController]
    [Route("back/[controller]")]
    public class AulasController : _BaseController {
        private readonly IAulaService _aulaService;
        private readonly ILogService _logger;

        public AulasController(IAulaService aulaService, ILogService logger)
        {
            _aulaService = aulaService;
            _logger = logger;
        }

        [HttpGet("{aulaId}")]
        public ActionResult<CalendarioList> Get(int aulaId)
        {
            try {
                var response = _aulaService.Get(aulaId);

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("all")]
        public ActionResult<List<CalendarioList>> GetAll()
        {
            try {
                var response = _aulaService.GetAll();

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("all/professor/{professorId}")]
        public ActionResult<List<CalendarioList>> GetAllByProfessorId(int professorId)
        {
            try {
                var response = _aulaService.GetAllByProfessorId(professorId);

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("all/turma/{turmaId}")]
        public ActionResult<List<CalendarioList>> GetAllByTurmaId(int turmaId)
        {
            try {
                var response = _aulaService.GetAllByTurmaId(turmaId);

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost()]
        public ActionResult<ResponseModel> InsertAulaTurma(CreateAulaTurmaRequest model)
        {
            try {
                var response = _aulaService.InsertAulaTurma(model);

                if (response.Success) {
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("extra")]
        public ActionResult<ResponseModel> InsertAulaExtra(CreateAulaExtraRequest model)
        {
            try {
                var response = _aulaService.InsertAulaExtra(model);

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
        public ActionResult<ResponseModel> Update(UpdateAulaRequest model)
        {
            try {
                var response = _aulaService.Update(model);

                if (response.Success) {
                    _logger.Log("Update", "Aula", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpDelete("{aulaId}")]
        public ActionResult<ResponseModel> Delete(int aulaId)
        {
            try {
                var response = _aulaService.Delete(aulaId);

                if (response.Success) {
                    _logger.Log("Delete", "Aula", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("calendario")]
        public ActionResult<List<CalendarioResponse>> Calendario(CalendarioRequest request)
        {
            try {
                List<CalendarioResponse> response = _aulaService.Calendario(request);

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("chamada")]
        public ActionResult<ResponseModel> Chamada(RegisterChamadaRequest model)
        {
            try {
                ResponseModel response = _aulaService.RegisterChamada(model);

                if (response.Success) {
                    _logger.Log("Chamada", "Aula", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPatch("reagendar")]
        public ActionResult<ResponseModel> ReagendarAula(ReagendarAulaRequest model)
        {
            try {
                ResponseModel response = _aulaService.ReagendarAula(model);

                if (response.Success) {
                    _logger.Log("Reagendamento", "Aula", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("feriados/all")]
        public ActionResult<ResponseModel> GetAllFeriados()
        {
            try {
                var response = _aulaService.GetAllFeriados();

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }
    }
}
