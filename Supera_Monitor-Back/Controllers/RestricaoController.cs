﻿using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Restricao;
using Supera_Monitor_Back.Services;

namespace Supera_Monitor_Back.Controllers {
    [Authorize(Entities.Role.Admin, Entities.Role.Teacher, Entities.Role.Assistant)]
    [ApiController]
    [Route("back/restricoes")]
    public class RestricaoController : _BaseController {
        private readonly IRestricaoService _restricaoService;
        private readonly ILogService _logger;

        public RestricaoController(IRestricaoService restricaoService, ILogService logger) {
            _restricaoService = restricaoService;
            _logger = logger;
        }

        [HttpGet("all")]
        public ActionResult<ResponseModel> GetAll() {
            try {
                var response = _restricaoService.GetAll();

                return Ok(response);
            }
            catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("all/{alunoId}")]
        public ActionResult<List<RestricaoModel>> GetAllByAluno(int alunoId) {
            try {
                var response = _restricaoService.GetAllByAluno(alunoId);

                return Ok(response);
            }
            catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost()]
        public ActionResult<ResponseModel> Insert(CreateRestricaoRequest model) {
            try {
                var response = _restricaoService.Insert(model);

                if (response.Success) {
                    _logger.Log("Insert", "Aluno_Restricao", response.Object, Account?.Id);
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
        public ActionResult<ResponseModel> Update(UpdateRestricaoRequest model) {
            try {
                var response = _restricaoService.Update(model);

                if (response.Success) {
                    _logger.Log("Update", "Aluno_Restricao", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            }
            catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPatch("toggle-active/{restricaoId}")]
        public ActionResult<ResponseModel> ToggleActive(int restricaoId) {
            try {
                var response = _restricaoService.ToggleActive(restricaoId);

                if (response.Success) {
                    _logger.Log("Deactivate", "Aluno_Restricao", response, Account?.Id);
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
