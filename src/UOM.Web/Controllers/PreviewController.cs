using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Infrastructure.Persistence;

namespace UOM.Web.Controllers;

public sealed class PreviewController(AppDbContext db, IPreviewTokenService previewTokenService) : Controller
{
    [HttpGet("/onizleme/haber/{id:int}")]
    public async Task<IActionResult> News(int id, string token, CancellationToken cancellationToken)
    {
        if (!previewTokenService.TryValidate("haber", id, token))
        {
            return NotFound();
        }

        var post = await db.NewsPosts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        Response.Headers["X-Robots-Tag"] = "noindex";
        return View(post);
    }

    [HttpGet("/onizleme/etkinlik/{id:int}")]
    public async Task<IActionResult> Event(int id, string token, CancellationToken cancellationToken)
    {
        if (!previewTokenService.TryValidate("etkinlik", id, token))
        {
            return NotFound();
        }

        var entity = await db.Events.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        Response.Headers["X-Robots-Tag"] = "noindex";
        return View(entity);
    }
}