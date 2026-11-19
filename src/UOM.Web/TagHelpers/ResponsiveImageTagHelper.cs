using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using UOM.Application.Features.Public;

namespace UOM.Web.TagHelpers;

[HtmlTargetElement("responsive-image", TagStructure = TagStructure.WithoutEndTag)]
public sealed class ResponsiveImageTagHelper : TagHelper
{
    private readonly IMediaAssetReader _media;

    public ResponsiveImageTagHelper(IMediaAssetReader media) => _media = media;

    [HtmlAttributeName("media-id")]
    public int? MediaId { get; set; }

    [HtmlAttributeName("sizes")]
    public string? Sizes { get; set; }

    [HtmlAttributeName("alt")]
    public string? Alt { get; set; }

    [HtmlAttributeName("src")]
    public string? Src { get; set; }

    [HtmlAttributeName("class")]
    public string? CssClass { get; set; }

    [HtmlAttributeName("loading")]
    public string? Loading { get; set; }

    [HtmlAttributeName("fetchpriority")]
    public string? FetchPriority { get; set; }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        var asset = MediaId is > 0
            ? await _media.GetAsync(MediaId.Value, ViewContext.HttpContext.RequestAborted)
            : null;

        var alt = string.IsNullOrWhiteSpace(Alt) ? asset?.Alt ?? "" : Alt;
        var src = !string.IsNullOrWhiteSpace(Src) ? Src : asset?.Src;
        if (string.IsNullOrWhiteSpace(src))
        {
            output.SuppressOutput();
            return;
        }

        var sizes = string.IsNullOrWhiteSpace(Sizes) ? "(min-width: 768px) 50vw, 100vw" : Sizes;
        var loading = string.IsNullOrWhiteSpace(Loading) ? "lazy" : Loading;
        var css = string.IsNullOrWhiteSpace(CssClass) ? "" : $" class=\"{E(CssClass)}\"";
        var priority = string.IsNullOrWhiteSpace(FetchPriority) ? "" : $" fetchpriority=\"{E(FetchPriority)}\"";
        var dimensions = asset?.Width is int width && asset.Height is int height && width > 0 && height > 0
            ? $" width=\"{width}\" height=\"{height}\""
            : "";

        if (asset is { Sources.Count: > 0 })
        {
            var srcset = string.Join(", ", asset.Sources.Select(source => $"{E(source.Url)} {source.Width}w"));
            output.Content.SetHtmlContent($"""
                <picture>
                  <source type="image/webp" srcset="{srcset}" sizes="{E(sizes)}" />
                  <img src="{E(src)}" alt="{E(alt)}"{css}{dimensions} loading="{E(loading)}" decoding="async"{priority} />
                </picture>
                """);
            return;
        }

        output.Content.SetHtmlContent(
            $"""<img src="{E(src)}" alt="{E(alt)}"{css}{dimensions} loading="{E(loading)}" decoding="async"{priority} />""");
    }

    private static string E(string value) => HtmlEncoder.Default.Encode(value);
}
