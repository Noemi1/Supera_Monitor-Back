using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Models.Aluno;
using Supera_Monitor_Back.Services;
using System.Reflection;

namespace Supera_Monitor_Back.Controllers {
    [Authorize(Entities.Role.Admin, Entities.Role.Teacher, Entities.Role.Assistant)]
    [ApiController]
    [Route("back/[controller]")]
    public class AlunosController : _BaseController {
        private readonly IAlunoService _alunoService;
        private readonly ILogService _logger;

        public AlunosController(IAlunoService alunoService, ILogService logService)
        {
            _alunoService = alunoService;
            _logger = logService;
        }

        [HttpGet("{alunoId}")]
        public ActionResult Get(int alunoId)
        {
            try {
                var response = _alunoService.Get(alunoId);

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
                var response = _alunoService.GetAll();

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("pessoas/all")]
        public ActionResult GetAllPessoas()
        {
            try {
                var response = _alunoService.GetAllPessoas();

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost()]
        public ActionResult Insert(CreateAlunoRequest model)
        {
            try {
                var response = _alunoService.Insert(model);

                if (response.Success == false) {
                    return BadRequest(response.Message);
                }

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPut()]
        public ActionResult Update(UpdateAlunoRequest model)
        {
            try {
                var response = _alunoService.Update(model);

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }
    }
}
