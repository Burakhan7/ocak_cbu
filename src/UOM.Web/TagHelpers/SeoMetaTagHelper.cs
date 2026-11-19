using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;
using UOM.Application.Features.Public;
using UOM.Web.Seo;

namespace UOM.Web.TagHelpers;

[HtmlTargetElement("seo-meta", TagStructure = TagStructure.WithoutEndTag)]
public sealed class SeoMetaTagHelper : TagHelper
{
    private readonly IConfiguration _configuration;
    private readonly ISiteSettingsProvider _settings;

    public SeoMetaTagHelper(IConfiguration configuration, ISiteSettingsProvider settings)
    {
        _configuration = configuration;
        _settings = settings;
    }

    [HtmlAttributeName("title")]
    public string? Title { get; set; }

    [HtmlAttributeName("description")]
    public string? Description { get; set; }

    [HtmlAttributeName("canonical")]
    public string? Canonical { get; set; }

    [HtmlAttributeName("image")]
    public string? Image { get; set; }

    [HtmlAttributeName("type")]
    public string? OgType { get; set; }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var site = await _settings.GetAsync(ViewContext.HttpContext.RequestAborted);
        var rawTitle = First(
            Title,
            ViewContext.ViewData["Title"] as string,
            site.SiteName);
        var fullTitle = rawTitle.Contains(site.SiteName, StringComparison.Ordinal)
            ? rawTitle
            : $"{rawTitle} — {site.SiteName}";
        var description = First(
            Description,
            ViewContext.ViewData["Description"] as string,
            site.Tagline,
            site.SiteName);
        var path = First(
            Canonical,
            ViewContext.ViewData["Canonical"] as string,
            ViewContext.HttpContext.Request.Path.Value,
            "/");
        var canonical = SiteUrl.Absolute(ViewContext.HttpContext.Request, _configuration, path);
        var imagePath = FirstOrNull(Image, ViewContext.ViewData["OgImage"] as string, site.LogoUrl);
        var image = string.IsNullOrWhiteSpace(imagePath)
            ? null
            : SiteUrl.Absolute(ViewContext.HttpContext.Request, _configuration, imagePath);
        var type = First(OgType, ViewContext.ViewData["OgType"] as string, "website");
        var card = image is null ? "summary" : "summary_large_image";

        output.TagName = null;
        output.Content.SetHtmlContent($"""
            <title>{E(fullTitle)}</title>
            <meta name="description" content="{E(description)}" />
            <link rel="canonical" href="{E(canonical)}" />
            <meta property="og:locale" content="tr_TR" />
            <meta property="og:site_name" content="{E(site.SiteName)}" />
            <meta property="og:title" content="{E(fullTitle)}" />
            <meta property="og:description" content="{E(description)}" />
            <meta property="og:url" content="{E(canonical)}" />
            <meta property="og:type" content="{E(type)}" />
            {(image is null ? "" : $"""<meta property="og:image" content="{E(image)}" />""")}
            <meta name="twitter:card" content="{card}" />
            <meta name="twitter:title" content="{E(fullTitle)}" />
            <meta name="twitter:description" content="{E(description)}" />
            {(image is null ? "" : $"""<meta name="twitter:image" content="{E(image)}" />""")}
            """);
    }

    private static string E(string value) => HtmlEncoder.Default.Encode(value);

    private static string First(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static string? FirstOrNull(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
