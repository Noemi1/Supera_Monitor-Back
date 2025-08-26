using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Models.Pessoa;
using Supera_Monitor_Back.Services;

namespace Supera_Monitor_Back.Controllers {
    [Authorize(Entities.Role.Admin, Entities.Role.Teacher, Entities.Role.Assistant)]
    [ApiController]
    [Route("back/[controller]")]
    public class PessoasController : _BaseController {
        private readonly IPessoaService _pessoaService;
        private readonly ILogService _logger;

        public PessoasController(IPessoaService pessoaService, ILogService logService) {
            _pessoaService = pessoaService;
            _logger = logService;
        }

        [HttpGet("geracoes/all")]
        public ActionResult<List<PessoaGeracaoModel>> GetAllGeracoes() {
            try {
                var response = _pessoaService.GetAllGeracoes();

                return Ok(response);
            }
            catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("faixas-etarias/all")]
        public ActionResult<List<PessoaFaixaEtariaModel>> GetAllFaixasEtarias() {
            try {
                var response = _pessoaService.GetAllFaixasEtarias();

                return Ok(response);
            }
            catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("status/all")]
        public ActionResult<List<PessoaStatusModel>> GetAllStatus() {
            try {
                var response = _pessoaService.GetAllStatus();

                return Ok(response);
            }
            catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("sexos/all")]
        public ActionResult<List<PessoaSexoModel>> GetAllSexos() {
            try {
                var response = _pessoaService.GetAllSexos();

                return Ok(response);
            }
            catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

    }
}

