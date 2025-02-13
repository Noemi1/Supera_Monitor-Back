using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Supera_Monitor_Back.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Supera_Monitor_Back.Middlewares {
    public class JwtMiddleware {
        private readonly RequestDelegate _next;
        private readonly AppSettings _appSettings;
        //private readonly ILogServices _logger;

        public JwtMiddleware(RequestDelegate next, IOptions<AppSettings> appSettings
            //, ILogServices logger
            )
        {
            _next = next;
            _appSettings = appSettings.Value;
            //_logger = logger;
        }

        public async Task Invoke(HttpContext context, DataContext dataContext)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token != null)
                await AttachAccountToContext(context, dataContext, token);

            await _next(context);
        }

        private async Task AttachAccountToContext(HttpContext context, DataContext dataContext, string token)
        {
            try {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_appSettings.Secret);
                tokenHandler.ValidateToken(token, new TokenValidationParameters {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = ( JwtSecurityToken )validatedToken;
                var accountId = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);

                // attach account to context on successful jwt validation
                context.Items["Account"] = await dataContext.Accounts.FindAsync(accountId);
            } catch (Exception ex) {
                Console.WriteLine("AttachAccountToContext triggered an exception " + ex.ToString());
                //_logger.Add(ex, MethodBase.GetCurrentMethod().DeclaringType.Name.ToString() + "." + MethodBase.GetCurrentMethod().ToString());
                // do nothing if jwt validation fails
                // account is not attached to context so request won't have access to secure routes
            }
        }
    }
}




