using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.ViewComponents;

public sealed class HeroSectionViewComponent : ViewComponent
{
    private readonly IPublicEventService _events;
    private readonly ISiteSettingsProvider _settings;

    public HeroSectionViewComponent(IPublicEventService events, ISiteSettingsProvider settings)
    {
        _events = events;
        _settings = settings;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        ViewBag.Site = await _settings.GetAsync(HttpContext.RequestAborted);
        var next = await _events.GetNextUpcomingAsync(HttpContext.RequestAborted);
        return View(next);
    }
}