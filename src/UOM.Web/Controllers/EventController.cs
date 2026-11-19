using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using UOM.Application.Caching;
using UOM.Application.Common;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;

namespace UOM.Web.Controllers;

[Route("etkinlikler")]
[OutputCache(Duration = 300, Tags = [CacheTags.Events], VaryByQueryKeys = ["*"])]
public sealed class EventController : Controller
{
    private readonly IPublicEventService _events;

    public EventController(IPublicEventService events) => _events = events;

    [HttpGet("")]
    public async Task<IActionResult> Index(bool? yaklasan, int sayfa = 1, CancellationToken cancellationToken = default)
    {
        var upcomingOnly = yaklasan is null or true;
        var result = await _events.ListAsync(new EventListQuery(upcomingOnly, sayfa), cancellationToken);
        ViewBag.UpcomingOnly = upcomingOnly;
        return View(result);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken = default)
    {
        var evt = await _events.GetBySlugAsync(slug, cancellationToken);
        return evt is null ? NotFound() : View(evt);
    }

    [HttpGet("takvim.ics")]
    public async Task<IActionResult> Calendar(CancellationToken cancellationToken = default)
    {
        var bytes = await _events.BuildCalendarFeedAsync(cancellationToken);
        return File(bytes, "text/calendar", "etkinlikler.ics");
    }

    [HttpGet("{slug}/takvime-ekle.ics")]
    public async Task<IActionResult> Ics(string slug, CancellationToken cancellationToken = default)
    {
        var bytes = await _events.BuildSingleEventIcsAsync(slug, cancellationToken);
        if (bytes.Length == 0)
        {
            return NotFound();
        }

        return File(bytes, "text/calendar", $"{slug}.ics");
    }
}