using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Accounts;
using Supera_Monitor_Back.Services.Email;
using Supera_Monitor_Back.Services.Email.Models;

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

        private void SendVerificationEmail(Account account, string url, string randomPassword)
        {
            _emailService.SendEmail(
                templateType: "VerifyAccount",
                model: new VerificationEmailModel {
                    Url = url,
                    VerificationToken = account.VerificationToken,
                    Email = account.Email,
                    RandomPassword = randomPassword
                },
                to: account.Email);
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

            _emailService.SendEmail(
                templateType: "PasswordReset",
                model: new PasswordResetModel { RandomPassword = randomPassword },
                to: account.Email);

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
