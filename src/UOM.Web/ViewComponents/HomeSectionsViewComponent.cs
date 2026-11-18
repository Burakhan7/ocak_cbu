using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.ViewComponents;

public sealed class HomeSectionsViewComponent : ViewComponent
{
    private readonly ISiteSettingsProvider _settings;

    public HomeSectionsViewComponent(ISiteSettingsProvider settings) => _settings = settings;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var order = await _settings.GetHomeSectionOrderAsync(HttpContext.RequestAborted);
        return View(order);
    }
}