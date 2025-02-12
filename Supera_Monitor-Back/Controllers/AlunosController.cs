using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models;
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
        public ActionResult<AlunoList> Get(int alunoId)
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
        public ActionResult<List<AlunoList>> GetAll()
        {
            try {
                var response = _alunoService.GetAll();

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }


        [HttpPost()]
        public ActionResult<ResponseModel> Insert(CreateAlunoRequest model)
        {
            try {
                var response = _alunoService.Insert(model);

                if (response.Success) {
                    int alunoId = response.Object!.Id;
                    _logger.Log("Insert", "Alunos", response, Account?.Id);
                    return Created(@$"/alunos/{alunoId}", response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPut()]
        public ActionResult<ResponseModel> Update(UpdateAlunoRequest model)
        {
            try {
                var response = _alunoService.Update(model);

                if (response.Success) {
                    _logger.Log("Update", "Alunos", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPatch("toggle-active/{alunoId}")]
        public ActionResult<ResponseModel> ToggleDeactivate(int alunoId)
        {
            try {
                ResponseModel response = _alunoService.ToggleDeactivate(alunoId);

                if (response.Success) {
                    string action = response.Object!.Deactivated is null ? "Enable" : "Disable";

                    _logger.Log(action, "Alunos", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("image/{alunoId}")]
        public ActionResult<ResponseModel> GetProfileImage(int alunoId)
        {
            try {
                ResponseModel response = _alunoService.GetProfileImage(alunoId);

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("reposicao")]
        public ActionResult<ResponseModel> NewReposicao(NewReposicaoRequest model)
        {
            try {
                ResponseModel response = _alunoService.NewReposicao(model);

                if (response.Success) {
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        //[HttpPost("reposicao")]
        //public ActionResult<ResponseModel> InsertReposicao(CreateReposicaoRequest model)
        //{
        //    try {
        //        ResponseModel response = _alunoService.InsertReposicao(model);

        //        if (response.Success) {
        //            return Ok(response);
        //        }

        //        return BadRequest(response);
        //    } catch (Exception e) {
        //        _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
        //        return StatusCode(500, e);
        //    }
        //}

        //[HttpPost("{alunoId}/image")]
        //public async Task<ActionResult<ResponseModel>> UploadImage(int alunoId, [FromBody] UploadImageRequest request)
        //{
        //    if (request.BinaryImage == null || request.BinaryImage.Length == 0) {
        //        return new ResponseModel { Success = false, Message = "Arquivo inválido." };
        //    }

        //    ResponseModel response = await _alunoService.UploadImage(alunoId, request.BinaryImage);

        //    return Ok(response);
        //}
    }
}
