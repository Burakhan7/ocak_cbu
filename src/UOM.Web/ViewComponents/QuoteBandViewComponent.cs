using Microsoft.AspNetCore.Mvc;
using UOM.Application.Features.Public;

namespace UOM.Web.ViewComponents;

public sealed class QuoteBandViewComponent(IPublicQuoteService quotes) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var items = await quotes.GetActiveQuotesAsync(HttpContext.RequestAborted);
        return View(items);
    }
}