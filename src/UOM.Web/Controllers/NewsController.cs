using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Enums;

namespace UOM.Web.Controllers;

[Route("haberler")]
public sealed class NewsController : Controller
{
    private readonly IPublicNewsService _news;
    private readonly IPublicEventService _events;

    public NewsController(IPublicNewsService news, IPublicEventService events)
    {
        _news = news;
        _events = events;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(NewsScope? kapsam, int? kategori, int sayfa = 1, CancellationToken cancellationToken = default)
    {
        var result = await _news.ListAsync(new NewsListQuery(kapsam, kategori, sayfa, 12), cancellationToken);
        return View(result);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken = default)
    {
        var article = await _news.GetBySlugAsync(slug, cancellationToken);
        if (article is null)
        {
            return NotFound();
        }

        var related = article.CategoryId is { } categoryId
            ? await _news.GetRelatedAsync(categoryId, article.Id, 3, cancellationToken)
            : [];
        var upcoming = await _events.GetNextUpcomingAsync(cancellationToken);

        ViewBag.Related = related;
        ViewBag.UpcomingEvent = upcoming;
        return View(article);
    }
}