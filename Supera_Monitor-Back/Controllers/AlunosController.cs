using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Aluno;
using Supera_Monitor_Back.Services;

namespace Supera_Monitor_Back.Controllers {
    [ApiController]
    [Route("back/[controller]")]
    public class AlunosController : _BaseController {
        private readonly IAlunoService _alunoService;
        private readonly ILogService _logger;

        public AlunosController(IAlunoService alunoService, ILogService logService) {
            _alunoService = alunoService;
            _logger = logService;
        }

        [HttpGet("{alunoId}")]
        [Authorize(Role.Admin, Role.Teacher, Role.Assistant)]
        public ActionResult<AlunoListWithChecklist> Get(int alunoId) {
            try {
                var response = _alunoService.Get(alunoId);

                return Ok(response);
            }
            catch (Exception e) {
                // _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("all")]
        [Authorize(Role.Admin, Role.Teacher, Role.Assistant)]
        public ActionResult<List<AlunoList>> GetAll() {
            try {
                var response = _alunoService.GetAll();

                return Ok(response);
            }
            catch (Exception e) {
                // _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

		[HttpGet("historico/{alunoId}")]
		[Authorize(Role.Admin, Role.Teacher, Role.Assistant)]
		public ActionResult<List<AlunoHistoricoList>> GetHistoricoById(int alunoId)
		{
			try
			{
				List<AlunoHistoricoList> response = _alunoService.GetHistoricoById(alunoId);

				return Ok(response);
			}
			catch (Exception e)
			{
				// _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
				return StatusCode(500, e);
			}
		}
		[HttpGet("vigencia/{alunoId}")]
		[Authorize(Role.Admin, Role.Teacher, Role.Assistant)]
		public ActionResult<List<AlunoVigenciaList>> GetVigenciaById(int alunoId)
		{
			try
			{
				List<AlunoVigenciaList> response = _alunoService.GetVigenciaById(alunoId);

				return Ok(response);
			}
			catch (Exception e)
			{
				// _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
				return StatusCode(500, e);
			}
		}

		[HttpPost()]
        public ActionResult<ResponseModel> Insert(CreateAlunoRequest model) {
            try {
                ResponseModel response = _alunoService.Insert(model);

                if (response.Success) {
                    int alunoId = response.Object!.Id;
                    // _logger.Log("Insert", "Alunos", response, Account?.Id);
                    return Created(@$"/alunos/{alunoId}", response);
                }

                return BadRequest(response);
            }
            catch (Exception e) {
                // _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPut()]
        [Authorize(Role.Admin, Role.Teacher, Role.Assistant)]
        public ActionResult<ResponseModel> Update(UpdateAlunoRequest model) {
            try {
                var response = _alunoService.Update(model);

                if (response.Success) {
                    // _logger.Log("Update", "Alunos", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            }
            catch (Exception e) {
                // _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPatch("toggle-active/{alunoId}")]
        [Authorize(Role.Admin, Role.Teacher, Role.Assistant)]
        public ActionResult<ResponseModel> ToggleDeactivate(int alunoId) {
            try {
                ResponseModel response = _alunoService.ToggleDeactivate(alunoId);

                if (response.Success) {
                    string action = response.Object!.Deactivated is null ? "Enable" : "Disable";

                    // _logger.Log(action, "Alunos", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            }
            catch (Exception e) {
                // _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("image/{alunoId}")]
        [Authorize(Role.Admin, Role.Teacher, Role.Assistant)]
        public ActionResult<ResponseModel> GetProfileImage(int alunoId) {
            try {
                ResponseModel response = _alunoService.GetProfileImage(alunoId);

                return Ok(response);
            }
            catch (Exception e) {
                // _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("reposicao")]
        [Authorize(Role.Admin, Role.Teacher, Role.Assistant)]
        public ActionResult<ResponseModel> Reposicao(ReposicaoRequest model) {
            try {
                ResponseModel response = _alunoService.Reposicao(model);

                if (response.Success) {
                    // _logger.Log("Reposicao", "Aluno", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            }
            catch (Exception e) {
                // _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("primeira-aula")]
        [Authorize(Role.Admin, Role.Teacher, Role.Assistant)]
        public ActionResult<ResponseModel> PrimeiraAula(PrimeiraAulaRequest model) {
            try {
                ResponseModel response = _alunoService.PrimeiraAula(model);

                if (response.Success) {
                    // _logger.Log("Primeira Aula", "Aluno", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            }
            catch (Exception e) {
                // _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("apostilas/{alunoId}")]
        [Authorize(Role.Admin, Role.Teacher, Role.Assistant)]
        public ActionResult<List<ApostilaList>> GetApostilasByAluno(int alunoId) {
            try {
                var response = _alunoService.GetApostilasByAluno(alunoId);

                return Ok(response);
            }
            catch (Exception e) {
                // _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("resumo/{alunoId}")]
        [Authorize(Role.Admin, Role.Teacher, Role.Assistant)]
        public ActionResult<ResponseModel> GetSummaryByAluno(int alunoId) {
            try {
                var response = _alunoService.GetSummaryByAluno(alunoId);

                return Ok(response);
            }
            catch (Exception e) {
                // _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("all/with-checklist")]
        [Authorize(Role.Admin, Role.Teacher, Role.Assistant)]
        public ActionResult<List<AlunoListWithChecklist>> GetAllWithChecklist(AlunoRequest request) {
            try {
                var response = _alunoService.GetAllWithChecklist(request);

                return Ok(response);
            }
            catch (Exception e) {
                // _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("checklists/all")]
        [Authorize(Role.Admin, Role.Teacher, Role.Assistant)]
        public ActionResult<List<AlunoListWithChecklist>> GetAllAlunoChecklists(AlunoRequest request) {
            try {
                var response = _alunoService.GetAlunoChecklistItemList(request);

                return Ok(response);
            }
            catch (Exception e) {
                // _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }
    }
}
