using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;
using UOM.Domain.Enums;

namespace UOM.Web.Controllers;

[Route("cbu/rehber")]
public sealed class GuideController(IPublicGuideService guides) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(GuideCategory? kategori, CancellationToken cancellationToken)
    {
        var items = await guides.ListPublishedAsync(kategori, cancellationToken);
        ViewBag.Category = kategori;
        return View(items);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken)
    {
        var article = await guides.GetBySlugAsync(slug, cancellationToken);
        return article is null ? NotFound() : View(article);
    }
}