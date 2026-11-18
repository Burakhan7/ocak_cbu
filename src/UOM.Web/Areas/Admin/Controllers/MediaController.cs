using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UOM.Application.Authorization;
using UOM.Infrastructure.Features.Admin;
using UOM.Web.Filters;

namespace UOM.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Policy = PolicyNames.CanWriteCampusContent)]
[ServiceFilter(typeof(AdminSecurityFilter))]
[Route("yonetim/medya")]
public sealed class MediaController(MediaAdminService mediaService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await mediaService.ListRecentAsync(48, cancellationToken));

    [HttpPost("yukle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? file, string altText, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Dosya seçilmedi.";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = file.OpenReadStream();
        var result = await mediaService.UploadImageAsync(stream, file.Length, file.FileName, altText, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Görsel yüklendi."
            : result.Error;
        return RedirectToAction(nameof(Index));
    }
}