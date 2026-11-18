using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UOM.Web.Models;

namespace UOM.Web.Controllers;

public class HomeController : Controller
{
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
    public IActionResult NotFoundPage()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("NotFound", new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
        });
    }
}