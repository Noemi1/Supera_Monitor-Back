using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
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
                //_logger.LogError(e, MethodBase.GetCurrentMethod().DeclaringType.Name.ToString() + "." + MethodBase.GetCurrentMethod().ToString());
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
                //_logger.LogError(e, MethodBase.GetCurrentMethod().DeclaringType.Name.ToString() + "." + MethodBase.GetCurrentMethod().ToString());
                return StatusCode(500, $"Unexpected error: {e.Message}");
            }
        }

        [Authorize]
        [HttpPost("revoke-token")]
        public ActionResult RevokeToken()
        {
            try {
                var token = Request.Cookies["refreshToken"];

                if (string.IsNullOrEmpty(token)) {
                    return Unauthorized(new { message = "Token is required." });
                }

                Account? account = _db.Account.Find(Account.Id);
                account!.AccountRefreshToken = _db.AccountRefreshToken
                    .Where(x => x.Account_Id == account.Id)
                    .ToList();

                var ownsToken = Account.OwnsToken(token);

                // Users can only revoke their own tokens and admins can revoke any token
                if (!ownsToken && Account.Role_Id != ( int )Role.Admin) {
                    return Unauthorized(new { message = $"Unauthorized, can't revoke token {token}" });
                }

                _accountService.RevokeToken(token, ipAddress());

                return Ok(new { message = "Token revoked" });
            } catch (Exception e) {
                //_logger.LogError(e, MethodBase.GetCurrentMethod().DeclaringType.Name.ToString() + "." + MethodBase.GetCurrentMethod().ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("forgot-password")]
        public ActionResult ForgotPassword(ForgotPasswordRequest model)
        {
            try {
                ResponseModel response = _accountService.ForgotPassword(model, Request.Headers["origin"]);

                if (response.Success) {
                    //_logger.Log("Forgot Password", "Account", response, response.Customer_Id, response.Object!.Id);
                }

                return Ok(response);
            } catch (Exception e) {
                //_logger.LogError(e, MethodBase.GetCurrentMethod().DeclaringType.Name.ToString() + "." + MethodBase.GetCurrentMethod().ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("reset-password")]
        public ActionResult ResetPassword(ResetPasswordRequest model)
        {
            try {
                ResponseModel response = _accountService.ResetPassword(model);

                if (response.Success) {
                    //_logger.Log("Password Updated", "Account", response, response.Customer_Id, response.Object!.Id);
                }

                return Ok(response);
            } catch (Exception e) {
                //_logger.LogError(e, MethodBase.GetCurrentMethod().DeclaringType.Name.ToString() + "." + MethodBase.GetCurrentMethod().ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("change-password")]
        public ActionResult ChangePassword(ChangePasswordRequest model)
        {
            try {
                ResponseModel response = _accountService.ChangePassword(model);

                if (response.Success) {
                    //_logger.Log("Change Password", "Account", response, response.Customer_Id, response.Object.Id);
                }

                return Ok(response);
            } catch (Exception e) {
                //_logger.LogError(e, MethodBase.GetCurrentMethod().DeclaringType.Name.ToString() + "." + MethodBase.GetCurrentMethod().ToString());
                return StatusCode(500, e);
            }
        }

        [Authorize]
        [HttpPost("update-account")]
        public ActionResult<ResponseModel> UpdateAccount(UpdateAccountRequest model)
        {
            try {
                ResponseModel response = _accountService.UpdateAccount(model);

                if (response.Success) {
                    //_logger.Log("Update", "Account", response, response.Customer_Id, response.Object.Id);
                }

                return Ok(response);
            } catch (Exception e) {
                //_logger.LogError(e, MethodBase.GetCurrentMethod().DeclaringType.Name.ToString() + "." + MethodBase.GetCurrentMethod().ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("verify-email")]
        public ActionResult VerifyEmail(VerifyEmailRequest model)
        {
            try {
                ResponseModel response = _accountService.VerifyEmail(model.Token);

                if (response.Success) {
                    //_logger.Log("Verification", "Account", response, response.Customer_Id, response.Object.Id);
                }

                return Ok(response);
            } catch (Exception e) {
                //_logger.LogError(e, MethodBase.GetCurrentMethod().DeclaringType.Name.ToString() + "." + MethodBase.GetCurrentMethod().ToString());
                return StatusCode(500, e);
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
            if (Request.Headers.ContainsKey("X-Forwarded-For")) {
                return Request.Headers["X-Forwarded-For"]!;
            }

            return HttpContext.Connection.RemoteIpAddress!.MapToIPv4().ToString();
        }

        #endregion

    }
}
