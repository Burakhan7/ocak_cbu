using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.ViewComponents;

public sealed class UpcomingEventsSectionViewComponent : ViewComponent
{
    private readonly IPublicEventService _events;

    public UpcomingEventsSectionViewComponent(IPublicEventService events) => _events = events;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var items = await _events.GetUpcomingAsync(3, HttpContext.RequestAborted);
        return View(items);
    }
}