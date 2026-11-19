using System.Globalization;
using System.Text;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Features.Public;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Persistence.Queryable;

namespace UOM.Infrastructure.Features.Public;

public sealed class SitemapService(AppDbContext db, IClock clock) : ISitemapService
{
    public async Task<string> BuildXmlAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        var urls = new List<SitemapEntry>();
        foreach (var index in PublicIndexPaths.All)
        {
            urls.Add(new SitemapEntry(index.Path, null, index.ChangeFrequency, index.Priority));
        }

        var news = await db.NewsPosts.AsNoTracking().WherePubliclyVisible(clock)
            .Select(n => new { n.Slug, n.UpdatedAtUtc, n.PublishedAtUtc })
            .ToListAsync(cancellationToken);
        urls.AddRange(news.Select(n => new SitemapEntry(
            PublicContentPaths.News(n.Slug),
            n.UpdatedAtUtc ?? n.PublishedAtUtc,
            "weekly",
            "0.7")));

        var events = await db.Events.AsNoTracking().WherePubliclyVisible(clock)
            .Select(e => new { e.Slug, e.UpdatedAtUtc, e.PublishedAtUtc })
            .ToListAsync(cancellationToken);
        urls.AddRange(events.Select(e => new SitemapEntry(
            PublicContentPaths.Event(e.Slug),
            e.UpdatedAtUtc ?? e.PublishedAtUtc,
            "weekly",
            "0.7")));

        var guides = await db.GuideArticles.AsNoTracking().WherePubliclyVisible(clock)
            .Select(g => new { g.Slug, g.UpdatedAtUtc, g.PublishedAtUtc })
            .ToListAsync(cancellationToken);
        urls.AddRange(guides.Select(g => new SitemapEntry(
            PublicContentPaths.Guide(g.Slug),
            g.UpdatedAtUtc ?? g.PublishedAtUtc,
            "monthly",
            "0.6")));

        var announcements = await db.Announcements.AsNoTracking().WhereListableAnnouncements(clock)
            .Select(a => new { a.Slug, a.UpdatedAtUtc, a.PublishedAtUtc })
            .ToListAsync(cancellationToken);
        urls.AddRange(announcements.Select(a => new SitemapEntry(
            PublicContentPaths.Announcement(a.Slug),
            a.UpdatedAtUtc ?? a.PublishedAtUtc,
            "weekly",
            "0.5")));

        var pages = await db.StaticPages.AsNoTracking().WherePubliclyVisible(clock)
            .Select(p => new { p.Slug, p.UpdatedAtUtc, p.PublishedAtUtc })
            .ToListAsync(cancellationToken);
        urls.AddRange(pages.Select(p => new SitemapEntry(
            PublicContentPaths.Page(p.Slug),
            p.UpdatedAtUtc ?? p.PublishedAtUtc,
            "monthly",
            "0.4")));

        var districts = await db.Districts.AsNoTracking()
            .Select(d => new { d.Slug, d.UpdatedAtUtc })
            .ToListAsync(cancellationToken);
        urls.AddRange(districts.Select(d => new SitemapEntry(
            PublicContentPaths.District(d.Slug),
            d.UpdatedAtUtc,
            "monthly",
            "0.4")));

        // TODO(spec): galeri albümleri public rota (`/galeri/{slug}`) gelince site haritasına eklenir.

        return Write(baseUrl, urls);
    }

    private static string Write(string baseUrl, IReadOnlyList<SitemapEntry> urls)
    {
        var root = baseUrl.TrimEnd('/');
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            OmitXmlDeclaration = false,
        };
        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
            foreach (var url in urls)
            {
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", root + url.Path);
                if (url.LastModifiedUtc is { } last)
                {
                    var utc = DateTime.SpecifyKind(last, DateTimeKind.Utc);
                    writer.WriteElementString("lastmod", utc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
                }

                writer.WriteElementString("changefreq", url.ChangeFrequency);
                writer.WriteElementString("priority", url.Priority);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private sealed record SitemapEntry(string Path, DateTime? LastModifiedUtc, string ChangeFrequency, string Priority);
}

public static class PublicIndexPaths
{
    public static readonly (string Path, string ChangeFrequency, string Priority)[] All =
    [
        ("/", "daily", "1.0"),
        ("/cbu", "daily", "0.9"),
        ("/cbu/rehber", "weekly", "0.7"),
        ("/cbu/kaynaklar", "weekly", "0.6"),
        ("/duyurular", "daily", "0.7"),
        ("/haberler", "daily", "0.8"),
        ("/etkinlikler", "daily", "0.8"),
        ("/genel-merkez", "weekly", "0.6"),
        ("/teskilat", "weekly", "0.6"),
        ("/videolar", "weekly", "0.5"),
        ("/iletisim", "monthly", "0.5"),
    ];
}
