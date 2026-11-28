using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.ViewComponents;

public sealed class HomeShowcaseViewComponent(IHomeFeedService homeFeed) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var items = await homeFeed.GetShowcaseAsync(HttpContext.RequestAborted);
        return View(items);
    }
}
