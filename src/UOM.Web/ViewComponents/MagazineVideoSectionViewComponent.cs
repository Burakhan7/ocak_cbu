using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.ViewComponents;

public sealed class MagazineVideoSectionViewComponent(IPublicParentHubService parentHub) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var block = await parentHub.GetMagazineAndFeaturedVideoForHomeAsync(HttpContext.RequestAborted);
        return View(block);
    }
}