using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.ViewComponents;

public sealed class ParentNewsStripViewComponent(IPublicParentHubService parentHub) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var items = await parentHub.GetNewsStripForHomeAsync(4, HttpContext.RequestAborted);
        return View(items);
    }
}