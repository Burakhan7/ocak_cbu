using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Web.Models;
using UOM.Web.Seo;

namespace UOM.Web.TagHelpers;

[HtmlTargetElement("json-ld", TagStructure = TagStructure.WithoutEndTag)]
public sealed class JsonLdTagHelper : TagHelper
{
    private readonly IConfiguration _configuration;
    private readonly ISiteSettingsProvider _settings;

    public JsonLdTagHelper(IConfiguration configuration, ISiteSettingsProvider settings)
    {
        _configuration = configuration;
        _settings = settings;
    }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var site = await _settings.GetAsync(ViewContext.HttpContext.RequestAborted);
        var request = ViewContext.HttpContext.Request;
        string Absolute(string? path) => SiteUrl.Absolute(request, _configuration, path ?? "/");

        var crumbs = ViewContext.ViewData["Breadcrumbs"] as IReadOnlyList<BreadcrumbItem>;
        if (crumbs is null || crumbs.Count == 0)
        {
            var title = ViewContext.ViewData["Title"] as string;
            crumbs = string.IsNullOrWhiteSpace(title) || title == "Ana sayfa"
                ? [new BreadcrumbItem("Ana Sayfa", "/")]
                : [new BreadcrumbItem("Ana Sayfa", "/"), new BreadcrumbItem(title, request.Path.Value ?? "/")];
        }

        var graph = new List<IReadOnlyDictionary<string, object?>>
        {
            JsonLdBuilder.Organization(site, Absolute),
            JsonLdBuilder.Breadcrumbs(crumbs, Absolute),
        };

        if (ViewContext.ViewData.Model is NewsDetailDto news)
        {
            graph.Add(JsonLdBuilder.NewsArticle(news, site, Absolute));
        }
        else if (ViewContext.ViewData.Model is EventDetailDto evt)
        {
            graph.Add(JsonLdBuilder.Event(evt, site, Absolute));
        }

        output.TagName = null;
        var json = JsonLdBuilder.Graph(graph);
        output.Content.SetHtmlContent(new HtmlString(
            "<script type=\"application/ld+json\">" + json + "</script>"));
    }
}
