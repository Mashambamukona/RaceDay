using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// Put [SessionAuthorize] on a controller to require a login,
/// or [SessionAuthorize("Organiser")] to require that role as well.
/// </summary>
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _role;

    public SessionAuthorizeAttribute(string? role = null) => _role = role;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (!session.IsLoggedIn())
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        if (_role is not null && session.GetRole() != _role)
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}