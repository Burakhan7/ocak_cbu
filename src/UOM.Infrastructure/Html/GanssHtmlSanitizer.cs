using Ganss.Xss;
using UOM.Application.Abstractions;
using AppHtmlSanitizer = UOM.Application.Abstractions.IHtmlSanitizer;

namespace UOM.Infrastructure.Html;

public sealed class GanssHtmlSanitizer : AppHtmlSanitizer
{
    private readonly HtmlSanitizer _inner = Create();

    public string Sanitize(string? html) =>
        string.IsNullOrWhiteSpace(html) ? string.Empty : _inner.Sanitize(html);

    private static HtmlSanitizer Create()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Add("iframe");
        sanitizer.AllowedAttributes.Add("class");
        sanitizer.AllowedAttributes.Add("target");
        sanitizer.AllowedAttributes.Add("rel");
        return sanitizer;
    }
}