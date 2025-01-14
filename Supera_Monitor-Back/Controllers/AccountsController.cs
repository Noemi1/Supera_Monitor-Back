using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models.Accounts;
using Supera_Monitor_Back.Services;

namespace Supera_Monitor_Back.Controllers {
    [ApiController]
    [Route("back/[controller]")]
    public class AccountsController : BaseController {
        private readonly IAccountService _accountService;
        private readonly DataContext _db;

        // TODO: Add logger
        public AccountsController(
            IAccountService accountService,
            DataContext db
            )
        {
            _accountService = accountService;
            _db = db;
        }

        #region TEST ROUTES
        [HttpGet("dbcheck")]
        public async Task<ActionResult> CheckConnection()
        {
            try {
                var canConnect = await _db.Database.CanConnectAsync();

                if (canConnect) {
                    return Ok("Database connection is OK.");
                }

                return BadRequest("Couldn't connect to database.");
            } catch (Exception e) {
                return StatusCode(500, $"Database check failed: {e.Message}");
            }
        }

        /*
        Create account manually
        Email: galax1y@test.com
        Password: galax2y

        [HttpPost("createAccount")]
        public ActionResult CreateAccount()
        {
            Account account = new Account {
                Name = "galax1y",
                AcceptTerms = true,
                PasswordHash = "$2b$10$a46QGCAIbzhXEKJl36cD1OBQE5xMNyATdvrrfh1s/wtqTdawg2lHu",
                Email = "galax1y@test.com",
                Phone = "123456789",
                VerificationToken = "",
                ResetTokenExpires = DateTime.Now,
                PasswordReset = DateTime.Now
            };

            _db.Account.Add(account);
            _db.SaveChanges();

            return Ok("Account created manually");
        }
        */
        #endregion

        #region CONTROLLER ROUTES

        [HttpPost("authenticate")]
        public ActionResult<AuthenticateResponse> Authenticate(AuthenticateRequest model)
        {
            try {
                var response = _accountService.Authenticate(model, ipAddress());
                setTokenCookie(response.RefreshToken);
                return Ok(response);
            } catch (Exception e) {
                return StatusCode(500, e);
            }
        }

        [HttpPost("refresh-token")]
        public ActionResult<AuthenticateResponse> RefreshToken()
        {
            try {
                var refreshToken = Request.Cookies["refreshToken"];

                if (string.IsNullOrEmpty(refreshToken)) {
                    return Unauthorized(new { message = "Token is required." });
                }

                var response = _accountService.RefreshToken(refreshToken, ipAddress());
                setTokenCookie(response.RefreshToken);
                return Ok(response);
            } catch (Exception e) {
                // TODO: LogError
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }

        #endregion

        #region HELPER FUNCTIONS

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

        // TODO: Null checks? For now I've overidden the warnings with !
        private string ipAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"]!;
            else
                return HttpContext.Connection.RemoteIpAddress!.MapToIPv4().ToString();
        }

        #endregion

    }
}
