using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Accounts;

namespace Supera_Monitor_Back.Services {
    public interface IUserService {
        AccountResponse Get(int accountId);
        List<AccountList> GetAll(int accountId);
        List<AccountRoleModel> GetRoles();
        ResponseModel Insert(CreateAccountRequest model, string origin);
        ResponseModel Update(UpdateAccountRequest model/*, string ip*/);
        ResponseModel Delete(int accountId);
        ResponseModel ResetPassword(int accountId);
        ResponseModel Deactivated(int accountId, bool activate, string ipAddress);
        void SendVerificationEmail(Account account, string url, string randomPassword);
    }

    public class UserService : IUserService {
        private readonly DataContext _db;
        private readonly IMapper _mapper;
        private readonly Account? _account;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;
        private readonly IAccountService _accountService;

        public UserService(
            DataContext db,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IEmailService emailService,
            IAccountService accountService
            )
        {
            _db = db;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _account = ( Account? )httpContextAccessor?.HttpContext?.Items["Account"];
            _emailService = emailService;
            _accountService = accountService;
        }

        public AccountResponse Get(int accountId)
        {
            Account? account = _db.Account
                .Include(acc => acc.AccountRole)
                .FirstOrDefault(acc => acc.Id == accountId);

            if (account == null) {
                throw new Exception("Account not found.");
            }

            AccountResponse response = _mapper.Map<AccountResponse>(account);

            return response;
        }

        public List<AccountList> GetAll(int accountId)
        {
            List<AccountList> accounts = _db.AccountList
                .Where(accList => accList.Id == accountId)
                .ToList();

            return accounts;
        }

        public List<AccountRoleModel> GetRoles()
        {
            List<AccountRole> roles = _db.AccountRole.ToList();

            return _mapper.Map<List<AccountRoleModel>>(roles);
        }

        public ResponseModel Insert(CreateAccountRequest model, string origin)
        {
            Account? account = _db.Account.FirstOrDefault(acc => acc.Email == model.Email);

            if (account != null) {
                return new ResponseModel { Message = "This e-mail has already been registered." };
            }

            if (_account.Deactivated != null) {
                return new ResponseModel { Message = "This account has been deactivated." };
            }

            // Validations passed

            account = _mapper.Map<Account>(model);
            account = SetNewAccount(account);

            (string randomPassword, string passwordHash) = Utils.GenerateRandomHashedPassword();
            account.PasswordHash = passwordHash;

            _db.Account.Add(account);
            _db.SaveChanges();

            SendVerificationEmail(account, origin, randomPassword);

            return new ResponseModel {
                Message = "Conta cadastrada com sucesso!",
                Success = true,
                Object = _db.AccountList.Find(account.Id),
            };
        }

        public ResponseModel Update(UpdateAccountRequest model)
        {
            Account? account = _db.Account.Find(model.Id);

            if (account == null) {
                return new ResponseModel { Message = "Account not found." };
            }

            var emailIsAlreadyTaken = _db.Account.Any(acc => acc.Email == model.Email && acc.Id != model.Id);

            if (emailIsAlreadyTaken) {
                return new ResponseModel { Message = "This e-mail has already been registered." };
            }

            if (account.Deactivated != null) {
                return new ResponseModel { Message = "You cannot update an account if it is deactivated." };
            }

            if (_account.Role_Id != ( int )Role.Admin && _account.Id != account.Id) {
                return new ResponseModel { Message = "You are not allowed to perform this action." };
            }

            // Validations passed

            account.Name = model.Name;
            account.Phone = model.Phone;
            account.Role_Id = model.Role_Id;
            account.LastUpdated = TimeFunctions.HoraAtualBR();

            // If user is editing his own account
            if (account.Id == _account.Id) {
                _httpContextAccessor.HttpContext.Items["Account"] = account;
            }

            AccountList? old = _db.AccountList
                .AsNoTracking()
                .FirstOrDefault(accList => accList.Id == account.Id);

            _db.Account.Update(account);
            _db.SaveChanges();

            return new ResponseModel {
                Message = "Conta atualizada com sucesso!",
                Success = true,
                Object = _db.AccountList.AsNoTracking().FirstOrDefault(x => x.Id == account.Id),
                OldObject = old
            };
        }

        public ResponseModel Delete(int accountId)
        {
            Account? account = _db.Account
                .Include(acc => acc.AccountRefreshToken)
                .Include(acc => acc.AccountRole)
                .FirstOrDefault(acc => acc.Id == accountId);

            if (account == null) {
                return new ResponseModel { Message = "Account not found." };
            }

            if (account.Deactivated != null) {
                return new ResponseModel { Message = "You cannot update an existing account if it is deactivated." };
            }

            if (_account == null || _account.Role_Id != ( int )Role.Admin && _account.Id != account.Id) {
                return new ResponseModel { Message = "You are not allowed to perform this action." };
            }

            // Validations passed

            AccountList? logObject = _db.AccountList.Find(account.Id);

            _db.AccountRefreshToken.RemoveRange(account.AccountRefreshToken);

            _db.Account.Remove(account);
            _db.SaveChanges();

            return new ResponseModel {
                Message = "Conta deletada com sucesso.",
                Success = true,
                Object = logObject,
            };
        }

        public void SendVerificationEmail(Account account, string url, string randomPassword)
        {
            var verifyUrl = $"{url}/account/verify-email?token={account.VerificationToken}";

            string message = $@"<p>A registration was made on Supera_Back with your email.</p>
                            <p>Please click the link below to verify your account.:</p>
                            <p><a href='{verifyUrl}'>{verifyUrl}</a></p>
                            <p>To login, enter your e-mail ({account.Email}) and password ({randomPassword})</p>
                            <p>If this was a mistake, please disregard this message.</p>";

            _emailService.Send(
                to: account.Email,
                subject: "Supera - Account Registration",
                html: $@"<h4>Account Registration</h4>
                         {message}
                        <br>
                         <p>Your password is personal and non-transferable and must be kept confidential and in a secure environment. Do not share your password.</p>
                        <br>
                        <p> Warning: This automatic message is intended exclusively for the person(s) to whom it is addressed, and may contain confidential and legally protected information. If you are not the intended recipient of this Message, you are hereby notified to refrain from disclosing, copying, distributing, examining or, in any way, using the information contained in this Message, as it is illegal. If you have received this Message by mistake, please reply to this Message informing us of what happened.</p>"
            );
        }

        public ResponseModel ResetPassword(int accountId)
        {
            Account? account = _db.Account
                 .Include(x => x.AccountRole)
                 .FirstOrDefault(x => x.Id == accountId);

            if (account == null) {
                return new ResponseModel { Message = "Account not found." };
            }

            if (account.Deactivated != null) {
                return new ResponseModel { Message = "You cannot reset password from a deactivated account." };
            }

            (string randomPassword, string passwordHash) = Utils.GenerateRandomHashedPassword();
            account.PasswordHash = passwordHash;
            account.PasswordReset = TimeFunctions.HoraAtualBR();

            _db.Account.Update(account);
            _db.SaveChanges();

            var html = @$"<h4>Password reset e-mail</h4>
                        <p>Your password has been reseted by admin.</p>
                        <p>Use your new password below to login.</p>
                        <p>New passsword: <b> {randomPassword} </b></p>
                        <br>
                        <p>Your password is personal and non-transferable and must be kept confidential and in a secure environment. Do not share your password.</p>
                        <br>
                        <p> Warning: This automatic Message is intended exclusively for the person(s) to whom it is addressed, and may contain confidential and legally protected information. If you are not the intended recipient of this Message, you are hereby notified to refrain from disclosing, copying, distributing, examining or, in any way, using the information contained in this Message, as it is illegal. If you have received this Message by mistake, please reply to this Message informing us of what happened.</p>
                ";

            _emailService.Send(account.Email, "Supera - Password reset", html);

            return new ResponseModel {
                Message = "Email de recuperação de senha enviado!",
                Success = true,
                Object = _db.AccountList.Find(account.Id)
            };
        }

        public ResponseModel Deactivated(int accountId, bool activate, string ipAddress)
        {
            Account? account = _db.Account
                            .Include(acc => acc.AccountRefreshToken)
                            .FirstOrDefault(acc => acc.Id == accountId);

            if (account == null) {
                return new ResponseModel { Message = "Account not found." };
            }

            if (account.Deactivated != null && activate) {
                return new ResponseModel { Message = "You cannot enable a deactivated account." };
            }

            if (_account.Role_Id < account.Role_Id) {
                return new ResponseModel { Message = "You are not allowed to perform this action." };
            }

            account.Deactivated = activate ? null : TimeFunctions.HoraAtualBR();

            _db.Account.Update(account);
            _db.SaveChanges();

            account.AccountRefreshToken = _db.AccountRefreshToken
                .Where(x => x.Account_Id == accountId)
                .ToList();

            // If user is editing his own account
            if (account.Id == _account!.Id) {
                _httpContextAccessor.HttpContext.Items["Account"] = account;
            }

            if (!activate) {
                var tokens = account.AccountRefreshToken.Where(tok => tok.IsActive).ToList();

                foreach (var refreshToken in tokens) {
                    _accountService.RevokeToken(refreshToken.Token, ipAddress);
                }
            }

            return new ResponseModel {
                Success = true,
                Object = _db.AccountList.Find(account.Id),
            };
        }

        #region HELPER FUNCTIONS

        private Account SetNewAccount(Account entity)
        {
            entity.AcceptTerms = true;
            entity.Created = TimeFunctions.HoraAtualBR();
            entity.VerificationToken = Utils.RandomTokenString();
            entity.Deactivated = null;
            entity.Verified = null;

            entity.Account_Created_Id = _account.Id;
            entity.Created = TimeFunctions.HoraAtualBR();

            return entity;
        }


        #endregion
    }
}
