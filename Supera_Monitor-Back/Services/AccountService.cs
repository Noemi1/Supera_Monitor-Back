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

        public AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress)
        {
            Account? account = _db.Account
                .Include(e => e.AccountRefreshToken)
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
            // TODO: Add account roles

            response.JwtToken = jwtToken;
            response.RefreshToken = refreshToken.Token;

            return response;
        }

        public void removeOldRefreshTokens(Account account)
        {
            account.AccountRefreshToken = account.AccountRefreshToken.Where(x => x.IsActive && x.Created.AddDays(_appSettings.RefreshTokenTTL) > TimeFunctions.HoraAtualBR()).ToList();
        }

        public string generateJwtToken(Account account)
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

        public AccountRefreshToken generateRefreshToken(string ipAddress)
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
        public string randomTokenString()
        {
            using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            var randomBytes = new byte[40];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            return BitConverter.ToString(randomBytes).Replace("-", "");
        }
    }
}
