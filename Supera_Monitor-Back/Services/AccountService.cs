using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models.Accounts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using BC = BCrypt.Net.BCrypt;

namespace Supera_Monitor_Back.Services {

    public interface IAccountService {
        AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress);
        AuthenticateResponse RefreshToken(string token, string ipAddress);
    }

    public class AccountService : IAccountService {
        private readonly DataContext _db;
        private readonly AppSettings _appSettings;
        private readonly IMapper _mapper;

        public AccountService(
            DataContext context,
            IOptions<AppSettings> appSettings,
            IMapper mapper
            )
        {
            _db = context;
            _appSettings = appSettings.Value;
            _mapper = mapper;
        }


        #region TESTING

        /* Route that hashes a password for testing
        public string Hash(string password)
        {
            return BC.HashPassword(password);
        }
        */

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

            removeOldRefreshTokens(account);

            string jwtToken = generateJwtToken(account);

            AccountRefreshToken refreshToken = generateRefreshToken(ipAddress);
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
            var (refreshToken, account) = getRefreshToken(token);

            // Renew refresh and JWT tokens
            var newRefreshToken = generateRefreshToken(ipAddress);
            refreshToken.Revoked = TimeFunctions.HoraAtualBR();
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = newRefreshToken.Token;
            account.AccountRefreshToken.Add(newRefreshToken);

            removeOldRefreshTokens(account);

            // Save newly refreshed token on database
            _db.Update(account);
            _db.SaveChanges();

            // Send the updated JWT token back to the user
            string jwtToken = generateJwtToken(account);

            AuthenticateResponse response = _mapper.Map<AuthenticateResponse>(account);

            // response.Role = account.AccountRole.Role;
            response.JwtToken = jwtToken;
            response.RefreshToken = newRefreshToken.Token;
            return response;
        }

        public void RevokeToken(string token, string ipAddress)
        {
            var (refreshToken, account) = getRefreshToken(token);

            refreshToken.Revoked = TimeFunctions.HoraAtualBR();
            refreshToken.RevokedByIp = ipAddress;

            _db.AccountRefreshToken.Update(refreshToken);
            _db.SaveChanges();
        }

        #endregion

        #region HELPER FUNCTIONS

        private (AccountRefreshToken, Account) getRefreshToken(string token)
        {
            var accounts = _db.Account
                .Include(x => x.AccountRefreshToken)
                .ToList();

            var account = accounts
                .SingleOrDefault(acc => acc.AccountRefreshToken.Any(t => t.Token == token));

            if (account == null)
                throw new Exception("Invalid token.");

            var refreshToken = account.AccountRefreshToken.Single(t => t.Token == token);

            if (!refreshToken.IsActive)
                throw new Exception("Invalid token.");

            return (refreshToken, account);
        }

        private void removeOldRefreshTokens(Account account)
        {
            account.AccountRefreshToken = account.AccountRefreshToken.Where(token =>
                token.IsActive && token.Created.AddDays(_appSettings.RefreshTokenTTL) > TimeFunctions.HoraAtualBR()).ToList();
        }

        private string generateJwtToken(Account account)
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

        private AccountRefreshToken generateRefreshToken(string ipAddress)
        {
            return new AccountRefreshToken {
                Token = randomTokenString(),
                Expires = TimeFunctions.HoraAtualBR().AddDays(7),
                Created = TimeFunctions.HoraAtualBR(),
                CreatedByIp = ipAddress
            };
        }

        // TODO: RNGCryptoServiceProvider is obsolete
        // Generate a random sequence of bytes and convert them to hex string
        private string randomTokenString()
        {
            using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            var randomBytes = new byte[40];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            return BitConverter.ToString(randomBytes).Replace("-", "");
        }

        #endregion
    }
}
