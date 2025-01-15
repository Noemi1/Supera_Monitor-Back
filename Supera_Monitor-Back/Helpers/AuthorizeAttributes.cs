using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter {
    private readonly IList<Role> _roles;

    public AuthorizeAttribute(params Role[] roles)
    {
        _roles = roles ?? new Role[] { };
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        try {
            var db = context.HttpContext.RequestServices.GetService(typeof(DataContext)) as DataContext;
            var account = ( Account )context.HttpContext.Items["Account"];

            List<int> rolesId = _roles.Select(x => ( int )x).ToList();

            // usahiudhasuid oi =) ?
            //var oi = _roles.ToList();
            var any = _roles.Any();

            if (account == null || account.Deactivated.HasValue || !account.IsVerified) {
                //Console.WriteLine("Account is null", account == null);
                //Console.WriteLine("Account is deactivated", account.Deactivated.HasValue);
                //Console.WriteLine("Account is not verified", account.IsVerified);
                context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
                return;
            }

            var contains = rolesId.Contains(account.Role_Id);
            //var a = any && !contains;
            if ((any && !contains)) {
                // not logged in or role not authorized
                context.Result = new JsonResult(new { message = "Forbidden" }) { StatusCode = StatusCodes.Status403Forbidden };
                return;
            }
        } catch (Exception e) {

        }
    }
}
