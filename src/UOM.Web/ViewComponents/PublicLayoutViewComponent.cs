using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using UOM.Application.Features.Public;
using UOM.Web.Models;

namespace UOM.Web.ViewComponents;

public sealed class PublicLayoutViewComponent : ViewComponent
{
    private readonly ISiteSettingsProvider _settings;
    private readonly IPublicPageService _pages;
    private readonly IConfiguration _configuration;

    public PublicLayoutViewComponent(ISiteSettingsProvider settings, IPublicPageService pages, IConfiguration configuration)
    {
        _settings = settings;
        _pages = pages;
        _configuration = configuration;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var site = await _settings.GetAsync(HttpContext.RequestAborted);
        var footerPages = await _pages.GetFooterPagesAsync(HttpContext.RequestAborted);
        return View(new PublicLayoutModel
        {
            Site = site,
            FooterPages = footerPages,
            SearchEnabled = _configuration.GetValue("Features:Search", true),
        });
    }
}