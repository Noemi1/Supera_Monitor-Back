using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Accounts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BC = BCrypt.Net.BCrypt;

namespace Supera_Monitor_Back.Services {

    public interface IAccountService {
        AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress);
        AuthenticateResponse RefreshToken(string token, string ipAddress);
        ResponseModel VerifyEmail(string token);
        ResponseModel UpdateAccount(UpdateAccountRequest model);
        ResponseModel ForgotPassword(ForgotPasswordRequest model, string origin);
        ResponseModel ChangePassword(ChangePasswordRequest model);
        ResponseModel ResetPassword(ResetPasswordRequest model);

        public string Hash(string toHash);
        void ValidateResetToken(ValidateResetTokenRequest model);
        void RevokeToken(string token, string ipAddress);
    }

    public class AccountService : IAccountService {
        private readonly DataContext _db;
        private readonly AppSettings _appSettings;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly Account? _account;

        public AccountService(
            DataContext context,
            IOptions<AppSettings> appSettings,
            IMapper mapper,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor
            )
        {
            _db = context;
            _appSettings = appSettings.Value;
            _mapper = mapper;
            _emailService = emailService;
            _account = ( Account? )httpContextAccessor.HttpContext?.Items["Account"];
        }


        #region TESTING

        // Use case that hashes a password for testing
        public string Hash(string password)
        {
            return BC.HashPassword(password);
        }


        #endregion

        #region USE CASES

        public AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress)
        {
            Account? account = _db.Account
                .Include(e => e.AccountRefreshToken)
                .Include(e => e.AccountRole)
                .SingleOrDefault(x => x.Email == model.Email);

            if (account == null || !BC.Verify(model.Password, account.PasswordHash))
                throw new Exception("E-mail or password is incorrect.");

            if (account == null || !account.IsVerified)
                throw new Exception("Your account has not been verified. Please check your email to activate your account.");

            if (account == null || account.Deactivated.HasValue)
                throw new Exception("Your account is disabled. Please contact your administrator.");

            RemoveOldRefreshTokens(account);

            string jwtToken = GenerateJwtToken(account);

            AccountRefreshToken refreshToken = GenerateRefreshToken(ipAddress);
            refreshToken.Account_Id = account.Id;
            account.AccountRefreshToken.Add(refreshToken);

            _db.Update(account);
            _db.SaveChanges();

            AuthenticateResponse response = _mapper.Map<AuthenticateResponse>(account);
            response.Role = account.AccountRole.Role;

            response.JwtToken = jwtToken;
            response.RefreshToken = refreshToken.Token;

            return response;
        }

        public AuthenticateResponse RefreshToken(string token, string ipAddress)
        {
            var (refreshToken, account) = GetRefreshToken(token);

            // Renew refresh and JWT tokens
            var newRefreshToken = GenerateRefreshToken(ipAddress);
            refreshToken.Revoked = TimeFunctions.HoraAtualBR();
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = newRefreshToken.Token;
            account.AccountRefreshToken.Add(newRefreshToken);

            RemoveOldRefreshTokens(account);

            // Save newly refreshed token on database
            _db.Update(account);
            _db.SaveChanges();

            // Send the updated JWT token back to the user
            string jwtToken = GenerateJwtToken(account);

            AuthenticateResponse response = _mapper.Map<AuthenticateResponse>(account);

            response.Role = account.AccountRole.Role;
            response.JwtToken = jwtToken;
            response.RefreshToken = newRefreshToken.Token;
            return response;
        }

        public void RevokeToken(string token, string ipAddress)
        {
            var (refreshToken, _) = GetRefreshToken(token);

            refreshToken.Revoked = TimeFunctions.HoraAtualBR();
            refreshToken.RevokedByIp = ipAddress;

            _db.AccountRefreshToken.Update(refreshToken);
            _db.SaveChanges();
        }

        public ResponseModel UpdateAccount(UpdateAccountRequest model)
        {
            Account? account = _db.Account.FirstOrDefault(acc => acc.Email == model.Email);

            if (account == null) {
                return new ResponseModel { Message = "Account not found." };
            }

            if (account.Email != _account!.Email) {
                return new ResponseModel { Message = "Invalid attempt." };
            }

            AccountList oldObj = _db.AccountList.AsNoTracking()
                                                .FirstOrDefault(acc => acc.Id == account.Id)!;

            account.Name = model.Name;
            account.Phone = model.Phone;

            _db.Account.Update(account);
            _db.SaveChanges();

            return new ResponseModel {
                Success = true,
                Message = "Account updated successfully.",
                Object = _db.AccountList.AsNoTracking().FirstOrDefault(acc => acc.Id == account.Id),
                OldObject = oldObj,
            };
        }

        public ResponseModel ForgotPassword(ForgotPasswordRequest model, string origin)
        {
            Account? account = _db.Account.SingleOrDefault(acc => acc.Email == model.Email);
            ResponseModel response = new();

            // Create a reset token that lasts 1 day
            if (account != null) {
                account.ResetToken = Utils.RandomTokenString();
                account.ResetTokenExpires = TimeFunctions.HoraAtualBR().AddDays(1);

                _db.Account.Update(account);
                _db.SaveChanges();

                SendPasswordResetEmail(account, origin);

                response.Success = true;
                response.Object = _db.AccountList.Find(account.Id); // gera exceção
            }

            response.Message = @"Please, check your inbox e-mail (" + model.Email + ") for password recovery instructions.";

            return response;
        }

        public ResponseModel ChangePassword(ChangePasswordRequest model)
        {
            Account? account = _db.Account.FirstOrDefault(acc => acc.Email == _account.Email);

            if (account == null) {
                return new ResponseModel { Message = "Account not found." };
            }

            var IsValidPassword = BC.Verify(model.CurrentPassword, account.PasswordHash);

            if (!IsValidPassword) {
                return new ResponseModel { Message = "Current Password is invalid." };
            }

            if (model.NewPassword != model.ConfirmPassword) {
                return new ResponseModel { Message = "Password does not match Confirm Password." };
            }

            var IsTheSamePassword = BC.Verify(model.NewPassword, account.PasswordHash);

            if (IsTheSamePassword) {
                return new ResponseModel { Message = "New password cannot match Current Password." };
            }

            // Validations passed, hash new password, clear reset token and save
            account.PasswordHash = BC.HashPassword(model.NewPassword);
            account.PasswordReset = TimeFunctions.HoraAtualBR();
            account.ResetToken = null;
            account.ResetTokenExpires = null;

            _db.Account.Update(account);
            _db.SaveChanges();

            return new ResponseModel {
                Object = _db.AccountList.Find(account.Id),
                Success = true,
                Message = "Password changed successfully."
            };
        }

        public ResponseModel ResetPassword(ResetPasswordRequest model)
        {
            DateTime now = TimeFunctions.HoraAtualBR();

            Account? account = _db.Account.SingleOrDefault(x =>
                x.ResetToken == model.Token &&
                x.ResetTokenExpires >= now);

            if (account == null) {
                return new ResponseModel { Message = "Invalid token." };
            }

            // Update password and remove reset token
            account.PasswordHash = BC.HashPassword(model.Password);
            account.PasswordReset = TimeFunctions.HoraAtualBR();
            account.ResetToken = null;
            account.ResetTokenExpires = null;

            _db.Account.Update(account);
            _db.SaveChanges();

            return new ResponseModel {
                Success = true,
                Object = _db.AccountList.Find(account.Id),
                Message = "Password updated successfully. Please, login."
            };
        }

        public ResponseModel VerifyEmail(string token)
        {
            Account? account = _db.Account.FirstOrDefault(acc => acc.VerificationToken == token);

            if (account == null) {
                return new ResponseModel { Message = "Verification failed." };
            }

            // Set account as verified and invalidate the token
            account.Verified = TimeFunctions.HoraAtualBR();
            account.VerificationToken = null;

            _db.Account.Update(account);
            _db.SaveChanges();

            return new ResponseModel {
                Object = _db.AccountList.Find(account.Id),
                Success = true,
                Message = "Verification completed successfully. Please, login."
            };
        }

        public void ValidateResetToken(ValidateResetTokenRequest model)
        {
            var account = _db.Account.SingleOrDefault(acc =>
                acc.ResetToken == model.Token &&
                acc.ResetTokenExpires > TimeFunctions.HoraAtualBR());

            if (account == null) {
                throw new Exception("Invalid token.");
            }
        }

        #endregion

        #region HELPER FUNCTIONS

        private (AccountRefreshToken, Account) GetRefreshToken(string token)
        {
            var accounts = _db.Account
                .Include(x => x.AccountRefreshToken)
                .ToList();

            var account = accounts
                .SingleOrDefault(acc => acc.AccountRefreshToken.Any(t => t.Token == token));

            if (account == null) {
                throw new Exception("Invalid token.");
            }

            var refreshToken = account.AccountRefreshToken.Single(t => t.Token == token);

            if (!refreshToken.IsActive) {
                throw new Exception("Invalid token.");
            }

            return (refreshToken, account);
        }

        private void RemoveOldRefreshTokens(Account account)
        {
            account.AccountRefreshToken = account.AccountRefreshToken.Where(token =>
                token.IsActive && token.Created.AddDays(_appSettings.RefreshTokenTTL) > TimeFunctions.HoraAtualBR()).ToList();
        }

        private string GenerateJwtToken(Account account)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = System.Text.Encoding.UTF8.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity(new[] { new Claim("id", account.Id.ToString()) }),
                Expires = TimeFunctions.HoraAtualBR().AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static AccountRefreshToken GenerateRefreshToken(string ipAddress)
        {
            return new AccountRefreshToken {
                Token = Utils.RandomTokenString(),
                Expires = TimeFunctions.HoraAtualBR().AddDays(7),
                Created = TimeFunctions.HoraAtualBR(),
                CreatedByIp = ipAddress
            };
        }

        private void SendPasswordResetEmail(Account account, string url)
        {
            var resetUrl = $"{url}/account/reset-password?token={account.ResetToken}";
            string message = $" <p>Please, follow link below to reset password:</p>"
             + $"<p><a href='{resetUrl}'>{resetUrl}</a></p>"
             + $"<p style='color: red'>Obs.: The link is valid for 1 day.</p>";

            _emailService.Send(
                to: account.Email,
                subject: "Supera - Password Reset",
                html: $@"<h4>Password Reset Email.</h4> 
                    {message}
                    <br>
                    <p>Your password is personal and non-transferable and must be kept confidential and in a secure environment. Do not share your password.</p>
                    <br>
                    <p>Warning: This automatic Message is intended exclusively for the person(s) to whom it is addressed, and may contain confidential and legally protected information. If you are not the intended recipient of this Message, you are hereby notified to refrain from disclosing, copying, distributing, examining or, in any way, using the information contained in this Message, as it is illegal. If you have received this Message by mistake, please reply to this Message informing us of what happened.</p>"
            );
        }

        #endregion
    }
}
