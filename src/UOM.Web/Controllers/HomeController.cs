using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using UOM.Application.Caching;
using UOM.Application.Features.Public;
using UOM.Web.Models;

namespace UOM.Web.Controllers;

public class HomeController : Controller
{
    private readonly IPublicEventService _events;

    public HomeController(IPublicEventService events) => _events = events;

    [OutputCache(Duration = 120, Tags = [CacheTags.Home], VaryByQueryKeys = ["*"])]
    public IActionResult Index() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        Response.StatusCode = 500;
        return View("Error500", new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            IsServerError = true,
        });
    }

    [Route("hata")]
    public IActionResult Error500() =>
        View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            IsServerError = true,
        });

    [Route("bulunamadi")]
    public async Task<IActionResult> NotFoundPage(CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        var upcoming = await _events.GetUpcomingAsync(5, cancellationToken);
        return View("NotFound", new NotFoundPageModel { Upcoming = upcoming });
    }
}