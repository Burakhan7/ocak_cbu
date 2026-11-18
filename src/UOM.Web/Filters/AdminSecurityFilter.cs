using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UOM.Infrastructure.Identity;

namespace UOM.Web.Filters;

public sealed class AdminSecurityFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.HttpContext.User);

        if (user is null)
        {
            await next();
            return;
        }

        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        var accountPaths = path.Contains("/hesap/", StringComparison.OrdinalIgnoreCase);

        if (user.MustChangePassword && !path.Contains("/sifre-degistir", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new RedirectResult("/yonetim/hesap/sifre-degistir");
            return;
        }

        if (!accountPaths && !user.TwoFactorEnabled)
        {
            context.Result = new RedirectResult("/yonetim/hesap/iki-faktor-kur");
            return;
        }

        await next();
    }
}