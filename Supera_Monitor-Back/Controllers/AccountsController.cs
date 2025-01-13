using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models.Accounts;
using Supera_Monitor_Back.Services;

namespace Supera_Monitor_Back.Controllers {
    [ApiController]
    [Route("back/[controller]")]
    public class AccountsController : Controller {
        private readonly IAccountService _accountService;

        public AccountsController(
             IAccountService accountService
            )
        {
            _accountService = accountService;
        }


        [HttpPost("authenticate")]
        public ActionResult<AuthenticateResponse> Authenticate(AuthenticateRequest model)
        {
            try {
                var response = _accountService.Authenticate(model);
                setTokenCookie(response.RefreshToken);
                return Ok(response);
            } catch (Exception e) {
                return StatusCode(500, e);
            }
        }

        private void setTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions {
                HttpOnly = true,
                Expires = TimeFunctions.HoraAtualBR().AddDays(7),
                IsEssential = true,
                Secure = true,
                SameSite = SameSiteMode.None,
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }
    }
}
