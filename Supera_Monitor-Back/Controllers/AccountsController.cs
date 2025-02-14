using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Accounts;
using Supera_Monitor_Back.Services;
using System.Reflection;

namespace Supera_Monitor_Back.Controllers {
    [ApiController]
    [Route("back/[controller]")]
    public class AccountsController : _BaseController {
        private readonly IAccountService _accountService;
        private readonly DataContext _db;
        private readonly ILogService _logger;

        public AccountsController(
            IAccountService accountService,
            DataContext db,
            ILogService logger
            )
        {
            _accountService = accountService;
            _db = db;
            _logger = logger;
        }

        #region TEST ROUTES

        /*
         * Tests database connection
         * Should be disabled in production
         */
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
        Creates account manually
        Should be disabled in production
        */
        [HttpPost("register")]
        public ActionResult CreateAccount(RegisterRequest model)
        {
            Account account = new() {
                Name = model.Name,
                AcceptTerms = model.AcceptTerms,
                PasswordHash = _accountService.Hash(model.Password),
                Email = model.Email,
                Phone = model.Phone,
                VerificationToken = "",
                ResetTokenExpires = DateTime.Now,
                PasswordReset = DateTime.Now,
                Created = DateTime.Now
            };

            _db.Accounts.Add(account);
            _db.SaveChanges();

            return Ok("Account created manually");
        }

        #endregion

        #region CONTROLLER ROUTES

        [HttpPost("authenticate")]
        public ActionResult<AuthenticateResponse> Authenticate(AuthenticateRequest model)
        {
            try {
                var response = _accountService.Authenticate(model, GetIpAddressFromHeaders());
                SetTokenCookie(response.RefreshToken);
                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("refresh-token")]
        public ActionResult<AuthenticateResponse> RefreshToken()
        {
            try {
                var refreshToken = Request.Cookies["refreshToken"];

                if (string.IsNullOrEmpty(refreshToken)) {
                    return Unauthorized(new { message = "Token não encontrado." });
                }

                var response = _accountService.RefreshToken(refreshToken, GetIpAddressFromHeaders());
                SetTokenCookie(response.RefreshToken);
                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, $"Ocorreu um erro inesperado: {e.Message}");
            }
        }

        [Authorize]
        [HttpPost("revoke-token")]
        public ActionResult<ResponseModel> RevokeToken()
        {
            try {
                var token = Request.Cookies["refreshToken"];

                if (string.IsNullOrEmpty(token)) {
                    return Unauthorized(new { message = "Token não encontrado." });
                }

                Account? account = _db.Accounts.Find(Account.Id);
                account!.AccountRefreshToken = _db.AccountRefreshTokens
                    .Where(x => x.Account_Id == account.Id)
                    .ToList();

                var ownsToken = Account.OwnsToken(token);

                // Users can only revoke their own tokens and admins can revoke any token
                if (!ownsToken && Account.Role_Id != ( int )Role.Admin) {
                    return Unauthorized(new { message = $"Não autorizado, não foi possível anular o token {token}" });
                }

                // Auth validations passed

                var response = _accountService.RevokeToken(token, GetIpAddressFromHeaders());

                if (response.Success) {
                    return Ok(response);
                }

                return BadRequest(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("forgot-password")]
        public ActionResult<ResponseModel> ForgotPassword(ForgotPasswordRequest model)
        {
            try {
                ResponseModel response = _accountService.ForgotPassword(model, Request.Headers["origin"]);

                // Email was not registered nor sent, but the response should still show success, to avoid email fishing
                if (!response.Success) {
                    response.Success = true;
                    return Ok(response);
                }

                // Email was sent. Log and clear object, to avoid sending sensitive data to the client
                if (response.Success) {
                    _logger.Log("Forgot Password", "Account", response, response.Object?.Id);
                    response.Object = null;
                }

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("reset-password")]
        public ActionResult<ResponseModel> ResetPassword([FromBody] ResetPasswordRequest model)
        {
            try {
                ResponseModel response = _accountService.ResetPassword(model);

                if (response.Success) {
                    _logger.Log("Password Updated", "Account", response, response.Object!.Id);
                }

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [Authorize]
        [HttpPost("change-password")]
        public ActionResult<ResponseModel> ChangePassword(ChangePasswordRequest model)
        {
            try {
                ResponseModel response = _accountService.ChangePassword(model);

                if (response.Success) {
                    _logger.Log("Change Password", "Account", response, response.Object!.Id);
                }

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
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
                    _logger.Log("Update", "Account", response, response.Object!.Id);
                }

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost("verify-email")]
        public ActionResult<ResponseModel> VerifyEmail(VerifyEmailRequest model)
        {
            try {
                ResponseModel response = _accountService.VerifyEmail(model.Token);

                if (response.Success) {
                    _logger.Log("Verification", "Account", response, response.Object!.Id);
                }

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        #endregion

        #region HELPER FUNCTIONS

        private void SetTokenCookie(string token)
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

        #endregion

    }
}
