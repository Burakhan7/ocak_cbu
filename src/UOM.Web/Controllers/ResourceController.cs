using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using UOM.Application.Abstractions;
using UOM.Application.Caching;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Enums;

namespace UOM.Web.Controllers;

[Route("cbu/kaynaklar")]
public sealed class ResourceController(
    IPublicResourceService resources,
    IContactFormService contact,
    IFileStorage fileStorage) : Controller
{
    [HttpGet("")]
    [OutputCache(Duration = 300, Tags = [CacheTags.Resources], VaryByQueryKeys = ["*"])]
    public async Task<IActionResult> Index(int? fakulte, string? ders, int sayfa = 1, CancellationToken cancellationToken = default)
    {
        var query = new ResourceListQuery(fakulte, ders, sayfa, 24);
        var page = await resources.ListAsync(query, cancellationToken);
        ViewBag.Faculties = await contact.GetFacultyOptionsAsync(cancellationToken);
        ViewBag.FacultyId = fakulte;
        ViewBag.CourseCode = ders;
        return View(page);
    }

    [HttpGet("{id:int}/indir")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var meta = await resources.GetForDownloadAsync(id, cancellationToken);
        if (meta is null)
        {
            return NotFound();
        }

        if (meta.Type == ResourceType.Link && !string.IsNullOrWhiteSpace(meta.ExternalUrl))
        {
            await resources.TryIncrementDownloadCountAsync(id, cancellationToken);
            return Redirect(meta.ExternalUrl);
        }

        if (meta.Type != ResourceType.File || string.IsNullOrWhiteSpace(meta.FileRelativePath))
        {
            return NotFound();
        }

        var absolute = fileStorage.GetAbsolutePath(meta.FileRelativePath);
        if (!fileStorage.Exists(meta.FileRelativePath))
        {
            return NotFound();
        }

        await resources.TryIncrementDownloadCountAsync(id, cancellationToken);
        var fileName = meta.OriginalFileName ?? $"{meta.Title}.bin";
        return PhysicalFile(absolute, meta.ContentType ?? "application/octet-stream", fileName);
    }
}