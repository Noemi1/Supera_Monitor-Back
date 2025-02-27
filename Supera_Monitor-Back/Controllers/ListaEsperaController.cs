using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.ListaEspera;
using Supera_Monitor_Back.Services;
using System.Reflection;

namespace Supera_Monitor_Back.Controllers {
    [Authorize(Entities.Role.Admin, Entities.Role.Teacher, Entities.Role.Assistant)]
    [ApiController]
    [Route("back/lista-espera")]
    public class ListaEsperaController : _BaseController {
        private readonly IListaEsperaService _listaEsperaService;
        private readonly ILogService _logger;

        public ListaEsperaController(IListaEsperaService listaEsperaService, ILogService logger)
        {
            _listaEsperaService = listaEsperaService;
            _logger = logger;
        }

        [HttpGet("all/{aulaId}")]
        public ActionResult<ResponseModel> GetAllByAulaId(int aulaId)
        {
            try {
                var response = _listaEsperaService.GetAllByAulaId(aulaId);

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost()]
        public ActionResult<ResponseModel> Insert(CreateEsperaRequest model)
        {
            try {
                var response = _listaEsperaService.Insert(model);

                if (response.Success) {
                    _logger.Log("Insert", "Aula_ListaEspera", response.Object, Account?.Id);
                    return Created($"/all/{model.Aula_Id}", response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPut("promote/{listaEsperaId}")]
        public ActionResult<ResponseModel> Promote(int listaEsperaId)
        {
            try {
                var response = _listaEsperaService.Promote(listaEsperaId);

                if (response.Success) {
                    _logger.Log("Promote", "Aula_ListaEspera", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpDelete("{listaEsperaId}")]
        public ActionResult<ResponseModel> Delete(int listaEsperaId)
        {
            try {
                var response = _listaEsperaService.Remove(listaEsperaId);

                if (response.Success) {
                    _logger.Log("Delete", "Aula_ListaEspera", response, Account?.Id);
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }
    }
}
