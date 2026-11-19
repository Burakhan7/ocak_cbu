using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using UOM.Application.Features.Public.Models;
using UOM.Web.Models;

namespace UOM.Web.Seo;

public static class JsonLdBuilder
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Graph(IReadOnlyList<IReadOnlyDictionary<string, object?>> graph)
    {
        var payload = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = graph,
        };
        var json = JsonSerializer.Serialize(payload, Options);
        return json.Replace("<", "\\u003c", StringComparison.Ordinal);
    }

    public static Dictionary<string, object?> Organization(SiteSettingsDto site, Func<string?, string> absolute)
    {
        var node = new Dictionary<string, object?>
        {
            ["@type"] = "Organization",
            ["name"] = site.SiteName,
            ["url"] = absolute("/"),
        };

        // TODO(spec): kurumsal logo dosyası gelene kadar yer tutucu SVG kullanılır (ARCHITECTURE §9, §15 madde 1).
        var logo = string.IsNullOrWhiteSpace(site.LogoUrl) ? "/img/brand/logo-placeholder.svg" : site.LogoUrl;
        node["logo"] = absolute(logo);

        if (!string.IsNullOrWhiteSpace(site.Email))
        {
            node["email"] = site.Email;
        }

        if (!string.IsNullOrWhiteSpace(site.Phone))
        {
            node["telephone"] = site.Phone;
        }

        if (!string.IsNullOrWhiteSpace(site.Address))
        {
            node["address"] = new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["streetAddress"] = site.Address,
                ["addressCountry"] = "TR",
            };
        }

        var sameAs = ReadSameAs(site.SocialLinksJson, site.WhatsAppChannelUrl);
        if (sameAs.Count > 0)
        {
            node["sameAs"] = sameAs;
        }

        return node;
    }

    public static Dictionary<string, object?> Breadcrumbs(
        IReadOnlyList<BreadcrumbItem> crumbs,
        Func<string?, string> absolute)
    {
        var items = new List<Dictionary<string, object?>>();
        for (var i = 0; i < crumbs.Count; i++)
        {
            items.Add(new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = i + 1,
                ["name"] = crumbs[i].Name,
                ["item"] = absolute(crumbs[i].Path),
            });
        }

        return new Dictionary<string, object?>
        {
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = items,
        };
    }

    public static Dictionary<string, object?> NewsArticle(
        NewsDetailDto news,
        SiteSettingsDto site,
        Func<string?, string> absolute)
    {
        var node = new Dictionary<string, object?>
        {
            ["@type"] = "NewsArticle",
            ["headline"] = news.Title,
            ["mainEntityOfPage"] = absolute("/haberler/" + news.Slug),
            ["inLanguage"] = "tr-TR",
            ["author"] = new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["name"] = site.SiteName,
            },
            ["publisher"] = Publisher(site, absolute),
        };

        var description = First(news.MetaDescription, news.Summary);
        if (description is not null)
        {
            node["description"] = description;
        }

        if (news.PublishedAtUtc is { } published)
        {
            node["datePublished"] = Iso(published);
        }

        var image = ImageUrl(news.CoverImageUrl, site, absolute);
        if (image is not null)
        {
            node["image"] = new[] { image };
        }

        return node;
    }

    public static Dictionary<string, object?> Event(
        EventDetailDto evt,
        SiteSettingsDto site,
        Func<string?, string> absolute)
    {
        var node = new Dictionary<string, object?>
        {
            ["@type"] = "Event",
            ["name"] = evt.Title,
            ["startDate"] = Iso(evt.StartsAtUtc),
            ["eventAttendanceMode"] = "https://schema.org/OfflineEventAttendanceMode",
            ["eventStatus"] = "https://schema.org/EventScheduled",
            ["url"] = absolute("/etkinlikler/" + evt.Slug),
            ["inLanguage"] = "tr-TR",
            ["organizer"] = new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["name"] = site.SiteName,
                ["url"] = absolute("/"),
            },
        };

        if (evt.EndsAtUtc is { } ends)
        {
            node["endDate"] = Iso(ends);
        }

        var description = First(evt.MetaDescription, evt.Summary);
        if (description is not null)
        {
            node["description"] = description;
        }

        var image = ImageUrl(evt.CoverImageUrl, site, absolute);
        if (image is not null)
        {
            node["image"] = new[] { image };
        }

        if (evt.Venue is not null)
        {
            node["location"] = Place(evt.Venue.Name, evt.Venue.Address, evt.Venue.Latitude, evt.Venue.Longitude);
        }
        else if (!string.IsNullOrWhiteSpace(site.Address))
        {
            node["location"] = Place(site.SiteName, site.Address, site.MapLat, site.MapLng);
        }
        // TODO(spec): mekan ve site adresi boşsa Event rich result konum alanı eksik kalır.

        return node;
    }

    private static Dictionary<string, object?> Publisher(SiteSettingsDto site, Func<string?, string> absolute)
    {
        var logo = string.IsNullOrWhiteSpace(site.LogoUrl) ? "/img/brand/logo-placeholder.svg" : site.LogoUrl;
        return new Dictionary<string, object?>
        {
            ["@type"] = "Organization",
            ["name"] = site.SiteName,
            ["logo"] = new Dictionary<string, object?>
            {
                ["@type"] = "ImageObject",
                ["url"] = absolute(logo),
            },
        };
    }

    private static Dictionary<string, object?> Place(string name, string? address, double? latitude, double? longitude)
    {
        var place = new Dictionary<string, object?>
        {
            ["@type"] = "Place",
            ["name"] = name,
        };
        if (!string.IsNullOrWhiteSpace(address))
        {
            place["address"] = new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["streetAddress"] = address,
                ["addressCountry"] = "TR",
            };
        }

        if (latitude is not null && longitude is not null)
        {
            place["geo"] = new Dictionary<string, object?>
            {
                ["@type"] = "GeoCoordinates",
                ["latitude"] = latitude,
                ["longitude"] = longitude,
            };
        }

        return place;
    }

    private static string? ImageUrl(string? cover, SiteSettingsDto site, Func<string?, string> absolute)
    {
        if (!string.IsNullOrWhiteSpace(cover))
        {
            return absolute(cover);
        }

        if (!string.IsNullOrWhiteSpace(site.LogoUrl))
        {
            return absolute(site.LogoUrl);
        }

        return null;
    }

    private static string Iso(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    private static string? First(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static IReadOnlyList<string> ReadSameAs(string? json, string? whatsApp)
    {
        var urls = new List<string>();
        if (IsHttp(whatsApp))
        {
            urls.Add(whatsApp!);
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            return urls;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String && IsHttp(property.Value.GetString()))
                    {
                        urls.Add(property.Value.GetString()!);
                    }
                }
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String && IsHttp(item.GetString()))
                    {
                        urls.Add(item.GetString()!);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // TODO(spec): sosyal bağlantı JSON'u il başkanlığı verisi gelince netleşir.
        }

        return urls.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool IsHttp(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
}
