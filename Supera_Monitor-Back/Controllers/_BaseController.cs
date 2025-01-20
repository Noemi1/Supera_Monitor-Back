using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities;

namespace Supera_Monitor_Back.Controllers {
    [Controller]
    public abstract class _BaseController : ControllerBase {
        // Returns the current authenticated account, else returns null (not logged in)
        public Account? Account => ( Account? )HttpContext.Items["Account"];

        // TODO: Null checks? For now I've overridden with '!'
        protected string GetIpAddressFromHeaders()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For")) {
                return Request.Headers["X-Forwarded-For"]!;
            }

            return HttpContext.Connection.RemoteIpAddress!.MapToIPv4().ToString();

        }
    }
}
