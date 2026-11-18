using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.Controllers;

public sealed class PageController : Controller
{
    private readonly IPublicPageService _pages;

    public PageController(IPublicPageService pages) => _pages = pages;

    [HttpGet("{slug:nonreserved}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken = default)
    {
        var redirect = await _pages.FindRedirectAsync(slug, cancellationToken);
        if (redirect is not null)
        {
            return RedirectPermanent(redirect.ToPath.StartsWith('/') ? redirect.ToPath : "/" + redirect.ToPath);
        }

        var page = await _pages.GetPublishedBySlugAsync(slug, cancellationToken);
        return page is null ? NotFound() : View(page);
    }
}