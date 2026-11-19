using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.ViewComponents;

public sealed class ParentProjectsSectionViewComponent(IPublicParentHubService parentHub) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var items = await parentHub.GetActiveProjectsAsync(HttpContext.RequestAborted);
        return View(items);
    }
}