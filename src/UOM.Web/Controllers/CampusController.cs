using Microsoft.AspNetCore.Mvc;

namespace UOM.Web.Controllers;

/// <summary>Faz 4'te genişletilecek CBÜ Hub yer tutucusu.</summary>
[Route("cbu")]
public sealed class CampusController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}