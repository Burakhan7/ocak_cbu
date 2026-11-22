using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.ViewComponents;

public sealed class CbuHubHomeViewComponent(ISiteSettingsProvider settings) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var site = await settings.GetAsync(HttpContext.RequestAborted);
        return View(site);
    }
}
