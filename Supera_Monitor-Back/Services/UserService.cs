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
        List<AccountList> GetAll();
        List<AccountRoleModel> GetRoles();
        ResponseModel Insert(CreateAccountRequest model, string origin);
        ResponseModel Update(UpdateAccountRequest model/*, string ip*/);
        ResponseModel Delete(int accountId);
        ResponseModel ResetPassword(int accountId);
        ResponseModel ToggleDeactivate(int accountId, string ipAddress);
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
            Account? account = _db.Accounts
                .Include(acc => acc.Role)
                .FirstOrDefault(acc => acc.Id == accountId);

            if (account == null) {
                throw new Exception("Conta não encontrada.");
            }

            AccountResponse response = _mapper.Map<AccountResponse>(account);

            return response;
        }

        public List<AccountList> GetAll()
        {
            List<AccountList> accounts = _db.AccountLists.OrderBy(t => t.Name).ToList();

            return accounts;
        }

        public List<AccountRoleModel> GetRoles()
        {
            List<AccountRole> roles = _db.AccountRoles.ToList();

            return _mapper.Map<List<AccountRoleModel>>(roles);
        }

        public ResponseModel Insert(CreateAccountRequest model, string origin)
        {
            Account? account = _db.Accounts.FirstOrDefault(acc => acc.Email == model.Email);

            if (account != null) {
                return new ResponseModel { Message = "Este e-mail já está em uso." };
            }

            if (_account.Deactivated != null) {
                return new ResponseModel { Message = "Esta conta foi desativada." };
            }

            // Validations passed

            account = _mapper.Map<Account>(model);
            account = SetNewAccount(account);

            (string randomPassword, string passwordHash) = Utils.GenerateRandomHashedPassword();
            account.PasswordHash = passwordHash;

            _db.Accounts.Add(account);
            _db.SaveChanges();

            SendVerificationEmail(account, origin, randomPassword);

            return new ResponseModel {
                Message = "Conta cadastrada com sucesso!",
                Success = true,
                Object = _db.AccountLists.AsNoTracking().FirstOrDefault(accList => accList.Id == account.Id),
            };
        }

        public ResponseModel Update(UpdateAccountRequest model)
        {
            Account? account = _db.Accounts.Find(model.Id);

            if (account == null) {
                return new ResponseModel { Message = "Conta não encontrada." };
            }

            var emailIsAlreadyTaken = _db.Accounts.Any(acc => acc.Email == model.Email && acc.Id != model.Id);

            if (emailIsAlreadyTaken) {
                return new ResponseModel { Message = "Este e-mail já está em uso." };
            }

            if (account.Deactivated != null) {
                return new ResponseModel { Message = "Você não pode atualizar uma conta desativada." };
            }

            if (_account == null || _account.Role_Id < ( int )Role.Teacher && _account.Id != account.Id) {
                return new ResponseModel { Message = "Você não está autorizado a realizar esta ação. Apenas administradores ou o próprio titular da conta podem atualizar esta conta." };
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

            AccountList? old = _db.AccountLists.AsNoTracking().FirstOrDefault(accList => accList.Id == account.Id);

            _db.Accounts.Update(account);
            _db.SaveChanges();

            return new ResponseModel {
                Message = "Conta atualizada com sucesso!",
                Success = true,
                Object = _db.AccountLists.AsNoTracking().FirstOrDefault(x => x.Id == account.Id),
                OldObject = old
            };
        }

        public ResponseModel Delete(int accountId)
        {
            Account? account = _db.Accounts
                .Include(acc => acc.AccountRefreshTokens)
                .Include(acc => acc.Role)
                .FirstOrDefault(acc => acc.Id == accountId);

            if (account == null) {
                return new ResponseModel { Message = "Conta não encontrada." };
            }

            if (account.Deactivated != null) {
                return new ResponseModel { Message = "Você não pode atualizar uma conta desativada." };
            }

            if (_account == null || _account.Role_Id < ( int )Role.Teacher && _account.Id != account.Id) {
                return new ResponseModel { Message = "Você não está autorizado a realizar esta ação. Apenas administradores ou o próprio titular da conta podem deletar esta conta." };
            }

            // Validations passed

            AccountList? logObject = _db.AccountLists.FirstOrDefault(acc => acc.Id == account.Id);

            _db.AccountRefreshTokens.RemoveRange(account.AccountRefreshTokens);

            _db.Accounts.Remove(account);
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
            Account? account = _db.Accounts
                 .Include(x => x.Role)
                 .FirstOrDefault(x => x.Id == accountId);

            if (account == null) {
                return new ResponseModel { Message = "Conta não encontrada." };
            }

            if (account.Deactivated != null) {
                return new ResponseModel { Message = "Você não pode resetar a senha de uma conta desativada." };
            }

            (string randomPassword, string passwordHash) = Utils.GenerateRandomHashedPassword();
            account.PasswordHash = passwordHash;
            account.PasswordReset = TimeFunctions.HoraAtualBR();

            _db.Accounts.Update(account);
            _db.SaveChanges();

            _emailService.SendEmail(
                templateType: "PasswordReset",
                model: new PasswordResetModel { RandomPassword = randomPassword },
                to: account.Email);

            return new ResponseModel {
                Message = "Email de recuperação de senha enviado!",
                Success = true,
                Object = _db.AccountLists.AsNoTracking().FirstOrDefault(accList => accList.Id == account.Id)
            };
        }

        public ResponseModel ToggleDeactivate(int accountId, string ipAddress)
        {
            Account? account = _db.Accounts
                            .Include(acc => acc.AccountRefreshTokens)
                            .FirstOrDefault(acc => acc.Id == accountId);

            if (account == null) {
                return new ResponseModel { Message = "Conta não encontrada." };
            }

            if (_account == null) {
                return new ResponseModel { Message = "Não foi possível completar a ação. Autenticação do autor não encontrada." };
            }

            if (_account.Role_Id < account.Role_Id) {
                return new ResponseModel { Message = "Você não está autorizado a realizar esta ação. Não é possível desativar uma conta que tem um cargo maior que o seu." };
            }

            // Validations passed

            bool IsAccountActive = account.Deactivated == null;

            account.Deactivated = IsAccountActive ? TimeFunctions.HoraAtualBR() : null;

            _db.Accounts.Update(account);
            _db.SaveChanges();

            // If user is editing his own account
            if (account.Id == _account!.Id) {
                _httpContextAccessor.HttpContext.Items["Account"] = account;
            }

            // If account is being deactivated, revoke all its active tokens
            if (account.Deactivated != null) {
                var tokens = account.AccountRefreshTokens.Where(tok => tok.IsActive).ToList();

                foreach (var refreshToken in tokens) {
                    _accountService.RevokeToken(refreshToken.Token, ipAddress);
                }
            }

            return new ResponseModel {
                Success = true,
                Object = _db.AccountLists.AsNoTracking().FirstOrDefault(accList => accList.Id == account.Id),
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
            entity.Role_Id = ( int )Role.Assistant;

            return entity;
        }

        #endregion
    }
}
