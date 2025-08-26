using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter {
    private readonly IList<Role> _roles;

    public AuthorizeAttribute(params Role[] roles) {
        _roles = roles ?? [];
    }

    public void OnAuthorization(AuthorizationFilterContext context) {
        try {
            var db = context.HttpContext.RequestServices.GetService(typeof(DataContext)) as DataContext;

            var account = (Account?)context.HttpContext.Items["Account"];

            var roleIds = _roles.Select(x => (int)x).ToList();

            var any = _roles.Any();

            if (account == null || account.Deactivated.HasValue || !account.IsVerified) {
                context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
                return;
            }

            var contains = roleIds.Contains(account.Role_Id);

            if ((any && !contains)) {
                context.Result = new JsonResult(new { message = "Forbidden" }) { StatusCode = StatusCodes.Status403Forbidden };
                return;
            }
        }
        catch (Exception e) {
            Console.WriteLine($"Caught an exception on AuthorizeAttributes {e.Message}");
        }
    }
}
