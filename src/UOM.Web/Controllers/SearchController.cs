using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.Controllers;

[Route("ara")]
public sealed class SearchController(IPublicSearchService search) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Arama";
        ViewData["Canonical"] = "/ara";
        ViewData["Description"] = "Haber, etkinlik, rehber, duyuru ve kaynak araması.";
        var result = await search.SearchAsync(q, cancellationToken);
        return View(result);
    }
}
