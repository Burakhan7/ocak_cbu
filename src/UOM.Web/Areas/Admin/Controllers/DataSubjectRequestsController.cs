using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UOM.Application.Authorization;
using UOM.Infrastructure.Privacy;
using UOM.Web.Filters;

namespace UOM.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Policy = PolicyNames.CanViewContactMessages)]
[ServiceFilter(typeof(AdminSecurityFilter))]
[Route("yonetim/kvkk-talepleri")]
public sealed class DataSubjectRequestsController(DataSubjectRequestService requests) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(new DataSubjectPageModel(null, null));

    [HttpPost("ara")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Search(string? email, CancellationToken cancellationToken)
    {
        var result = await requests.SearchAsync(email, cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Arama başarısız.");
            return View(nameof(Index), new DataSubjectPageModel(email, null));
        }

        return View(nameof(Index), new DataSubjectPageModel(email, result.Value));
    }

    [HttpPost("sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Erase(string? email, CancellationToken cancellationToken)
    {
        var result = await requests.EraseAsync(email, cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "Silme başarısız.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = result.Value.Total == 0
            ? "Bu e-posta ile eşleşen kayıt yok."
            : $"{result.Value.Total} kayıt kalıcı olarak silindi.";
        return RedirectToAction(nameof(Index));
    }
}

public sealed record DataSubjectPageModel(string? Email, DataSubjectSearchResult? Result);
