using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Services;

namespace Supera_Monitor_Back.Controllers {
    public class AulasController : _BaseController {
        private readonly IAulaService _aulaService;

        public AulasController(IAulaService aulaService)
        {
            _aulaService = aulaService;
        }

        [HttpGet()]
        public ActionResult Get()
        {
            throw new NotImplementedException();
        }
    }
}
