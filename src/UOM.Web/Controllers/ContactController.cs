using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Enums;
using UOM.Web.Models;

namespace UOM.Web.Controllers;

[Route("iletisim")]
public sealed class ContactController : Controller
{
    private readonly IContactFormService _contact;
    private readonly ISiteSettingsProvider _siteSettings;
    private readonly IConfiguration _configuration;

    public ContactController(
        IContactFormService contact,
        ISiteSettingsProvider siteSettings,
        IConfiguration configuration)
    {
        _contact = contact;
        _siteSettings = siteSettings;
        _configuration = configuration;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var settings = await _siteSettings.GetAsync(cancellationToken);
        var faculties = await _contact.GetFacultyOptionsAsync(cancellationToken);
        var model = new ContactFormViewModel
        {
            Site = settings,
            Faculties = faculties,
            TurnstileSiteKey = _configuration["Turnstile:SiteKey"],
            TurnstileEnabled = _configuration.GetValue("Turnstile:Enabled", false),
        };
        return View(model);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("forms")]
    public async Task<IActionResult> Index(ContactFormViewModel model, CancellationToken cancellationToken = default)
    {
        var settings = await _siteSettings.GetAsync(cancellationToken);
        model.Site = settings;
        model.Faculties = await _contact.GetFacultyOptionsAsync(cancellationToken);
        model.TurnstileSiteKey = _configuration["Turnstile:SiteKey"];
        model.TurnstileEnabled = _configuration.GetValue("Turnstile:Enabled", false);

        var request = new ContactFormRequest
        {
            FullName = model.FullName,
            Email = model.Email,
            Phone = model.Phone,
            Subject = model.Subject,
            Message = model.Message,
            IsStudentAtCbu = model.IsStudentAtCbu,
            FacultyId = model.FacultyId,
            KvkkConsent = model.KvkkConsent,
            Website = model.Website,
            TurnstileToken = model.TurnstileToken
                ?? Request.Form["cf-turnstile-response"].ToString(),
        };

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _contact.SubmitAsync(request, ip, cancellationToken);
        if (!result.Succeeded)
        {
            if (string.Equals(result.Error, "Spam", StringComparison.Ordinal))
            {
                model.Submitted = true;
                return View(model);
            }

            ModelState.AddModelError(string.Empty, result.Error ?? "Gönderim başarısız.");
            return View(model);
        }

        model.Submitted = true;
        return View(model);
    }
}