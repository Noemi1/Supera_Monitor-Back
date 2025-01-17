using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities;

namespace Supera_Monitor_Back.Controllers {
    [Controller]
    public abstract class _BaseController : ControllerBase {
        // returns the current authenticated account (null if not logged in)
        public Account? Account => ( Account? )HttpContext.Items["Account"];

        // TODO: Null checks? For now I've overridden with '!'
        public string GetIpAddressFromHeaders()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"]!;
            else
                return HttpContext.Connection.RemoteIpAddress!.MapToIPv4().ToString();
        }
    }
}
