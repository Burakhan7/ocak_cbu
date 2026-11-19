using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.Controllers;

[Route("teskilat")]
public sealed class OrganizationController(IPublicOrganizationService organization) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var page = await organization.GetOrganizationPageAsync(cancellationToken);
        return View(page);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> District(string slug, CancellationToken cancellationToken = default)
    {
        var district = await organization.GetDistrictBySlugAsync(slug, cancellationToken);
        if (district is null)
        {
            return NotFound();
        }

        return View(district);
    }
}