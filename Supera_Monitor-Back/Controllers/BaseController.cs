using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities;

namespace Supera_Monitor_Back.Controllers {
    [Controller]
    public abstract class BaseController : ControllerBase {
        // returns the current authenticated account (null if not logged in)
        public Account? Account => ( Account? )HttpContext.Items["Account"];
    }
}
