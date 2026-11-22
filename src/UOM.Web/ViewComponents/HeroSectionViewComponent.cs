using Microsoft.AspNetCore.Mvc;

namespace UOM.Web.ViewComponents;

public sealed class HeroSectionViewComponent : ViewComponent
{
    public IViewComponentResult Invoke() => View();
}
