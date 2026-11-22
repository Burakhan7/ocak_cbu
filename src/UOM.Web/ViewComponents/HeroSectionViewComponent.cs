using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.ViewComponents;

public sealed class HeroSectionViewComponent(IHomeFeedService homeFeed) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var next = await homeFeed.GetHeroAsync(HttpContext.RequestAborted);
        return View(next);
    }
}
