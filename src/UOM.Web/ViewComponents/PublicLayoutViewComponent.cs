using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;
using UOM.Web.Models;

namespace UOM.Web.ViewComponents;

public sealed class PublicLayoutViewComponent : ViewComponent
{
    private readonly ISiteSettingsProvider _settings;
    private readonly IPublicPageService _pages;

    public PublicLayoutViewComponent(ISiteSettingsProvider settings, IPublicPageService pages)
    {
        _settings = settings;
        _pages = pages;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var site = await _settings.GetAsync(HttpContext.RequestAborted);
        var footerPages = await _pages.GetFooterPagesAsync(HttpContext.RequestAborted);
        return View(new PublicLayoutModel { Site = site, FooterPages = footerPages });
    }
}