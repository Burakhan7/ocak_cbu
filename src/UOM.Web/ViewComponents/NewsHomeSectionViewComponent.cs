using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.ViewComponents;

public sealed class NewsHomeSectionViewComponent : ViewComponent
{
    private readonly IPublicNewsService _news;

    public NewsHomeSectionViewComponent(IPublicNewsService news) => _news = news;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var items = await _news.GetFeaturedForHomeAsync(1, 4, HttpContext.RequestAborted);
        return View(items);
    }
}