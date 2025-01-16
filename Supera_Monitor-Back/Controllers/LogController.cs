using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Services;
using System.Reflection;

namespace Supera_Monitor_Back.Controllers {
    [Route("back/log-acoes")]
    [ApiController]
    [Authorize]
    public class LogController : ControllerBase {
        private readonly ILogService _service;

        public LogController(ILogService service)
        {

            _service = service;
        }

        [HttpGet("list/{Account_Id}")]
        public ActionResult<List<LogList>> GetList(int Account_Id)
        {
            try {
                var resp = _service.GetList(Account_Id);
                return Ok(resp);
            } catch (Exception ex) {
                _service.LogError(ex, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return BadRequest(ex);
            }
        }

        [HttpGet("{id}")]
        public ActionResult<LogModel> Get(int Id)
        {
            try {
                LogModel result = _service.GetLogAcao(Id);
                return Ok(result);
            } catch (Exception ex) {
                _service.LogError(ex, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return BadRequest(ex);
            }
        }
    }
}
