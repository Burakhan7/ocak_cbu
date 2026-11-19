using System.Globalization;
using System.Text;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Features.Public;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Persistence.Queryable;

namespace UOM.Infrastructure.Features.Public;

public sealed class FeedService(AppDbContext db, IClock clock, ISiteSettingsProvider settings) : IFeedService
{
    public async Task<string> BuildRssAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        var site = await settings.GetAsync(cancellationToken);
        var posts = await db.NewsPosts.AsNoTracking()
            .WherePubliclyVisible(clock)
            .OrderByDescending(n => n.PublishedAtUtc)
            .Take(20)
            .Select(n => new { n.Title, n.Slug, n.Summary, n.PublishedAtUtc })
            .ToListAsync(cancellationToken);

        var root = baseUrl.TrimEnd('/');
        var xmlSettings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
        };
        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, xmlSettings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("rss");
            writer.WriteAttributeString("version", "2.0");
            writer.WriteStartElement("channel");
            writer.WriteElementString("title", site.SiteName);
            writer.WriteElementString("link", root + "/haberler");
            writer.WriteElementString("description", site.Tagline ?? site.SiteName);
            writer.WriteElementString("language", "tr-TR");
            foreach (var post in posts)
            {
                var link = root + PublicContentPaths.News(post.Slug);
                writer.WriteStartElement("item");
                writer.WriteElementString("title", post.Title);
                writer.WriteElementString("link", link);
                writer.WriteElementString("guid", link);
                writer.WriteElementString("description", SearchText.Excerpt(post.Summary, null) ?? post.Title);
                if (post.PublishedAtUtc is { } published)
                {
                    var utc = DateTime.SpecifyKind(published, DateTimeKind.Utc);
                    writer.WriteElementString(
                        "pubDate",
                        utc.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture));
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
