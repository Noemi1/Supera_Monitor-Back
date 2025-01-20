using Microsoft.AspNetCore.Mvc;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Accounts;
using Supera_Monitor_Back.Services;
using System.Reflection;

namespace Supera_Monitor_Back.Controllers {

    [Authorize(Entities.Role.Admin, Entities.Role.Teacher, Entities.Role.Assistant)]
    [ApiController]
    [Route("back/[controller]")]
    public class UsersController : _BaseController {
        private readonly IAccountService _accountService;
        private readonly IUserService _userService;
        private readonly ILogService _logger;

        public UsersController(
            IAccountService accountService,
            IUserService userService,
            ILogService logService
            )
        {
            _accountService = accountService;
            _userService = userService;
            _logger = logService;
        }

        [HttpGet("all/{accountId}")]
        public ActionResult GetAll(int accountId)
        {
            try {
                var response = _userService.GetAll(accountId);

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("{accountId}")]
        public ActionResult Get(int accountId)
        {
            try {
                var response = _userService.Get(accountId);

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpGet("roles")]
        public ActionResult<List<AccountRoleModel>> GetRoles()
        {
            try {
                var response = _userService.GetRoles();

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPost()]
        public ActionResult Insert(CreateAccountRequest model)
        {
            try {
                string origin = Request.Headers["origin"].ToString();

                var response = _userService.Insert(model, origin);

                if (response.Success) {
                    _logger.Log("Insert", "Account", response, Account?.Id);
                }

                return StatusCode(201, response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPut]
        public ActionResult<ResponseModel> Update(UpdateAccountRequest model)
        {
            try {
                ResponseModel response = _userService.Update(model);

                if (response.Success) {
                    _logger.Log("Update", "Account", response, Account?.Id);
                }

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpDelete("{accountId}")]
        [Authorize(Entities.Role.Admin)]
        public ActionResult<AccountResponse> Delete(int accountId)
        {
            try {
                ResponseModel response = _userService.Delete(accountId);

                if (response.Success) {
                    _logger.Log("Delete", "Account", response.Object, Account?.Id);
                }

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPatch("reset-password/{accountId}")]
        public ActionResult ResetPassword(int accountId)
        {
            try {
                ResponseModel response = _userService.ResetPassword(accountId);

                if (response.Success) {
                    _logger.Log("Reset Password", "Account", response, Account?.Id);
                }

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }

        [HttpPatch("{Id}/{Active}")]
        public ActionResult<ResponseModel> Deactivate(int Id, bool Active)
        {
            try {
                string action = Active ? "Enable" : "Disable";

                ResponseModel response = _userService.Deactivated(Id, Active, GetIpAddressFromHeaders());

                if (response.Success) {
                    _logger.Log(action, "Account", response, Account?.Id);
                }

                return Ok(response);
            } catch (Exception e) {
                _logger.LogError(e, MethodBase.GetCurrentMethod()!.DeclaringType!.Name.ToString() + "." + MethodBase.GetCurrentMethod()!.ToString());
                return StatusCode(500, e);
            }
        }
    }
}
