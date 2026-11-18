using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.ViewComponents;

public sealed class VisitUsSectionViewComponent : ViewComponent
{
    private readonly ISiteSettingsProvider _settings;

    public VisitUsSectionViewComponent(ISiteSettingsProvider settings) => _settings = settings;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var site = await _settings.GetAsync(HttpContext.RequestAborted);
        return View(site);
    }
}