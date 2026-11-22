using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.ViewComponents;

public sealed class UpcomingEventsSectionViewComponent(IHomeFeedService homeFeed) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var items = await homeFeed.GetUpcomingEventsAsync(HttpContext.RequestAborted);
        return View(items);
    }
}
