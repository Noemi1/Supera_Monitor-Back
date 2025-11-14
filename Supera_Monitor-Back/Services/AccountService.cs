using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Entities.Views;
using Supera_Monitor_Back.Helpers;
using Supera_Monitor_Back.Models;
using Supera_Monitor_Back.Models.Accounts;
using Supera_Monitor_Back.Models.Email.Constructors;
using Supera_Monitor_Back.Services.Email;
using BC = BCrypt.Net.BCrypt;

namespace Supera_Monitor_Back.Services;

public interface IAccountService
{
	AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress);
	AuthenticateResponse RefreshToken(string token, string ipAddress);
	ResponseModel VerifyEmail(string token);
	ResponseModel UpdateAccount(UpdateAccountRequest model);
	ResponseModel ForgotPassword(ForgotPasswordRequest model, string origin);
	ResponseModel ChangePassword(ChangePasswordRequest model);
	ResponseModel ResetPassword(ResetPasswordRequest model);
	ResponseModel RevokeToken(string token, string ipAddress);

	public string Hash(string toHash);
}

public class AccountService : IAccountService
{
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
		_account = (Account?)httpContextAccessor.HttpContext?.Items["Account"];
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
		Account? account = _db.Accounts
			.Include(e => e.AccountRefreshTokens)
			.Include(e => e.Role)
			.FirstOrDefault(e => e.Email == model.Email);

		if (account == null || !BC.Verify(model.Password, account.PasswordHash))
			throw new Exception("E-mail e/ou senha incorreto(s).");

		if (account == null || !account.IsVerified)
			throw new Exception("Sua conta ainda não foi verificada. Por favor verifique seu email para ativar sua conta.");

		if (account == null || account.Deactivated.HasValue)
			throw new Exception("Sua conta está desabilitada. Por favor, contate um administrador.");

		RemoveOldRefreshTokens(account);

		string jwtToken = GenerateJwtToken(account);

		AccountRefreshToken refreshToken = GenerateRefreshToken(ipAddress);
		refreshToken.Account_Id = account.Id;
		account.AccountRefreshTokens.Add(refreshToken);

		_db.Update(account);
		_db.SaveChanges();

		var relatedProfessor = _db.Professor.FirstOrDefault(p => p.Account_Id == account.Id);

		AuthenticateResponse response = _mapper.Map<AuthenticateResponse>(account);
		response.Role = account.Role.Role;
		response.Professor_Id = relatedProfessor?.Id;

		response.JwtToken = jwtToken;
		response.RefreshToken = refreshToken.Token;

		return response;
	}

	public AuthenticateResponse RefreshToken(string token, string ipAddress)
	{
		try
		{
			var (refreshToken, account) = GetRefreshToken(token);

			var relatedProfessor = _db.Professor.FirstOrDefault(p => p.Account_Id == account.Id);

			// Renew refresh and JWT tokens
			var newRefreshToken = GenerateRefreshToken(ipAddress);
			refreshToken.Revoked = TimeFunctions.HoraAtualBR();
			refreshToken.RevokedByIp = ipAddress;
			refreshToken.ReplacedByToken = newRefreshToken.Token;
			account.AccountRefreshTokens.Add(newRefreshToken);

			_db.SaveChanges();

			RemoveOldRefreshTokens(account);
			_db.SaveChanges();

			// Save newly refreshed token on database
			_db.Update(account);
			_db.SaveChanges();

			// Send the updated JWT token back to the user
			string jwtToken = GenerateJwtToken(account);

			AuthenticateResponse response = _mapper.Map<AuthenticateResponse>(account);

			response.Professor_Id = relatedProfessor?.Id;
			response.Role = account.Role.Role;
			response.JwtToken = jwtToken;
			response.RefreshToken = newRefreshToken.Token;
			return response;
		}
		catch (Exception ex)
		{
			throw new Exception(ex.Message);
		}
	}

	public ResponseModel RevokeToken(string token, string ipAddress)
	{
		ResponseModel response = new() { Success = false };

		try
		{
			var (refreshToken, _) = GetRefreshToken(token);

			refreshToken.Revoked = TimeFunctions.HoraAtualBR();
			refreshToken.RevokedByIp = ipAddress;

			_db.AccountRefreshTokens.Update(refreshToken);
			_db.SaveChanges();

			response.Success = true;
			response.Message = "Token anulado com sucesso";
		}
		catch (Exception ex)
		{
			response.Message = "Falha ao anular token: " + ex.ToString();
		}

		return response;
	}

	public ResponseModel UpdateAccount(UpdateAccountRequest model)
	{
		Account? account = _db.Accounts.FirstOrDefault(acc => acc.Email == model.Email);

		if (account == null)
		{
			return new ResponseModel { Message = "Conta não encontrada." };
		}

		if (account.Email != _account!.Email)
		{
			return new ResponseModel { Message = "Tentativa inválida." };
		}

		AccountList oldObj = _db.AccountLists.AsNoTracking()
											.FirstOrDefault(acc => acc.Id == account.Id)!;

		account.Name = model.Name;
		account.Phone = model.Phone;

		_db.Accounts.Update(account);
		_db.SaveChanges();

		return new ResponseModel
		{
			Success = true,
			Message = "Conta atualizada com sucesso.",
			Object = _db.AccountLists.AsNoTracking().FirstOrDefault(acc => acc.Id == account.Id),
			OldObject = oldObj,
		};
	}

	public ResponseModel ForgotPassword(ForgotPasswordRequest model, string origin)
	{
		Account? account = _db.Accounts.SingleOrDefault(acc => acc.Email == model.Email);
		ResponseModel response = new();

		// Create a reset token that lasts 1 day
		if (account != null)
		{
			account.ResetToken = Utils.RandomTokenString();
			account.ResetTokenExpires = TimeFunctions.HoraAtualBR().AddDays(1);

			_db.Accounts.Update(account);
			_db.SaveChanges();

			// Send password reset email
			_emailService.SendEmail(
				templateType: "ForgotPassword",
				model: new ForgotPasswordModel { ResetToken = account.ResetToken, Url = origin },
				to: account.Email);

			response.Success = true;
			response.Object = _db.AccountLists
				.AsNoTracking()
				.FirstOrDefault(acc => acc.Id == account.Id)!;
		}

		response.Message = $"Por favor, verifique sua caixa de e-mail ({model.Email}) para mais instruções sobre a recuperação de senha.";

		return response;
	}

	public ResponseModel ChangePassword(ChangePasswordRequest model)
	{
		Account? account = _db.Accounts.FirstOrDefault(acc => acc.Email == _account!.Email);

		if (account == null)
		{
			return new ResponseModel { Message = "Conta não encontrada." };
		}

		var IsValidPassword = BC.Verify(model.CurrentPassword, account.PasswordHash);

		if (!IsValidPassword)
		{
			return new ResponseModel { Message = "Senha inválida." };
		}

		if (model.NewPassword != model.ConfirmPassword)
		{
			return new ResponseModel { Message = "As senhas informadas não coincidem." };
		}

		var IsTheSamePassword = BC.Verify(model.NewPassword, account.PasswordHash);

		if (IsTheSamePassword)
		{
			return new ResponseModel { Message = "A nova senha deve ser diferente da senha anterior." };
		}

		// Validations passed, hash new password, clear reset token and save
		account.PasswordHash = BC.HashPassword(model.NewPassword);
		account.PasswordReset = TimeFunctions.HoraAtualBR();
		account.ResetToken = null;
		account.ResetTokenExpires = null;

		_db.Accounts.Update(account);
		_db.SaveChanges();

		return new ResponseModel
		{
			Object = _db.AccountLists.FirstOrDefault(acc => acc.Id == account.Id),
			Success = true,
			Message = "Senha foi trocada com sucesso."
		};
	}

	public ResponseModel ResetPassword(ResetPasswordRequest model)
	{
		DateTime now = TimeFunctions.HoraAtualBR();

		Account? account = _db.Accounts.SingleOrDefault(x =>
			x.ResetToken == model.Token &&
			x.ResetTokenExpires >= now);

		if (account == null)
		{
			return new ResponseModel { Message = "Token inválido." };
		}

		bool passwordsMatch = model.Password == model.ConfirmPassword;

		if (!passwordsMatch)
		{
			return new ResponseModel { Message = "As senhas informadas não coincidem." };
		}

		// Update password and remove reset token
		account.PasswordHash = BC.HashPassword(model.Password);
		account.PasswordReset = TimeFunctions.HoraAtualBR();
		account.ResetToken = null;
		account.ResetTokenExpires = null;

		_db.Accounts.Update(account);
		_db.SaveChanges();

		return new ResponseModel
		{
			Success = true,
			Object = _db.AccountLists.FirstOrDefault(acc => acc.Id == account.Id),
			Message = "Senha atualizada com sucesso. Por favor, faça o login."
		};
	}

	public ResponseModel VerifyEmail(string token)
	{
		Account? account = _db.Accounts.FirstOrDefault(acc => acc.VerificationToken == token);

		if (account == null)
		{
			return new ResponseModel { Message = "Verificação falhou." };
		}

		// Set account as verified and invalidate the token
		account.Verified = TimeFunctions.HoraAtualBR();
		account.VerificationToken = null;

		_db.Accounts.Update(account);
		_db.SaveChanges();

		return new ResponseModel
		{
			Object = _db.AccountLists.FirstOrDefault(acc => acc.Id == account.Id),
			Success = true,
			Message = "Verificação foi realizada com sucesso. Por favor, faça o login."
		};
	}

	#endregion

	#region HELPER FUNCTIONS

	private (AccountRefreshToken, Account) GetRefreshToken(string token)
	{
		var accounts = _db.Accounts
			.Include(x => x.Role)
			.Include(x => x.AccountRefreshTokens)
			.ToList();

		var account = accounts
			.SingleOrDefault(acc => acc.AccountRefreshTokens.Any(t => t.Token == token));

		if (account == null)
		{
			throw new Exception("Invalid token. Account not found.");
		}

		var refreshToken = account.AccountRefreshTokens.Single(t => t.Token == token);

		if (!refreshToken.IsActive)
		{
			throw new Exception("Invalid token. Refresh token not active.");
		}

		return (refreshToken, account);
	}

	private void RemoveOldRefreshTokens(Account account)
	{
		account.AccountRefreshTokens = account.AccountRefreshTokens
			.Where(token => token.IsActive && token.Created.AddDays(_appSettings.RefreshTokenTTL) > TimeFunctions.HoraAtualBR())
			.ToList();
	}

	private string GenerateJwtToken(Account account)
	{
		var tokenHandler = new JwtSecurityTokenHandler();
		var key = System.Text.Encoding.UTF8.GetBytes(_appSettings.Secret);
		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity([new Claim("id", account.Id.ToString())]),
			Expires = TimeFunctions.HoraAtualBR().AddDays(1),
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
		};

		var token = tokenHandler.CreateToken(tokenDescriptor);
		return tokenHandler.WriteToken(token);
	}

	private static AccountRefreshToken GenerateRefreshToken(string ipAddress)
	{
		return new AccountRefreshToken
		{
			Token = Utils.RandomTokenString(),
			Expires = TimeFunctions.HoraAtualBR().AddDays(7),
			Created = TimeFunctions.HoraAtualBR(),
			CreatedByIp = ipAddress
		};
	}

	#endregion
}
