using FluentAssertions;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Infrastructure.Features.Public;

namespace UOM.Application.Tests.Features.Public;

public class ParentSiteFillTests
{
    [Fact]
    public void Summarize_drops_markup_and_does_not_keep_the_rest_of_the_body()
    {
        var body = "<p>Kısa özet cümlesi.</p><p>" + new string('a', 400) + " GIZLI-SON</p>";

        var summary = ParentSiteText.Summarize(body);

        summary.Should().NotBeNull();
        summary!.Length.Should().BeLessThanOrEqualTo(ParentSiteText.SummaryMaxLength + 1);
        summary.Should().NotContain("GIZLI-SON");
        summary.Should().NotContain("<p>");
        summary.Should().StartWith("Kısa özet cümlesi.");
    }

    [Fact]
    public void Listing_html_reads_title_date_image_and_short_summary()
    {
        var tail = new string('b', 400) + " GIZLI-SON";
        var html = """
            <div class="blogs-item">
              <a href="https://ulkuocaklari.org.tr/blog/i/ornek-haber">
                <div class="blogs-img" data-background-image="https://ulkuocaklari.org.tr/uploads/blog/20261117124228-kapak.jpg"></div>
              </a>
              <h5><a href="https://ulkuocaklari.org.tr/blog/i/ornek-haber">Örnek haber başlığı</a></h5>
              <div class="blogs-desc">Kısa özet. 
            """ + tail + "</div></div>";

        var items = ParentSiteListingParser.ParseListingHtml(html, "https://ulkuocaklari.org.tr/blog");

        items.Should().ContainSingle();
        items[0].Title.Should().Be("Örnek haber başlığı");
        items[0].Kind.Should().Be(ParentSiteItemKind.News);
        items[0].PublishedDate.Should().Be(new DateOnly(2026, 11, 17));
        items[0].ImageUrl.Should().Be("https://ulkuocaklari.org.tr/uploads/blog/20261117124228-kapak.jpg");
        items[0].ExternalUrl.Should().Be("https://ulkuocaklari.org.tr/blog/i/ornek-haber");
        items[0].Summary.Should().NotContain("GIZLI-SON");
    }

    [Fact]
    public void Listing_html_marks_events_and_skips_duplicate_urls()
    {
        const string html = """
            <div class="blogs-item">
              <a href="https://ulkuocaklari.org.tr/blog/i/turnuva"></a>
              <h5>İL TURNUVASI FİNALİ</h5>
            </div>
            <div class="blogs-item">
              <a href="https://ulkuocaklari.org.tr/blog/i/turnuva/"></a>
              <h5>Aynı turnuva</h5>
            </div>
            <div class="blogs-item">
              <a href="/blog/i/atama"></a>
              <h5>Yeni atama</h5>
            </div>
            """;

        var items = ParentSiteListingParser.ParseListingHtml(html, "https://ulkuocaklari.org.tr/blog");

        items.Should().HaveCount(2);
        items[0].Kind.Should().Be(ParentSiteItemKind.Event);
        items[1].Kind.Should().Be(ParentSiteItemKind.News);
        items[1].ExternalUrl.Should().Be("https://ulkuocaklari.org.tr/blog/i/atama");
    }

    [Fact]
    public void Empty_or_soft_404_html_yields_nothing()
    {
        ParentSiteListingParser.ParseListingHtml("<html><title>Ülkü Ocakları</title></html>", "https://ulkuocaklari.org.tr/feed")
            .Should().BeEmpty();
        ParentSiteListingParser.ParseFeedXml("<html><title>Ülkü Ocakları</title></html>", "https://ulkuocaklari.org.tr/feed")
            .Should().BeEmpty();
    }

    [Fact]
    public void Rss_keeps_summary_not_the_article_body()
    {
        var xml = """
            <?xml version="1.0"?>
            <rss version="2.0"><channel>
              <item>
                <title>RSS haber</title>
                <link>https://ulkuocaklari.org.tr/blog/i/rss-haber</link>
                <pubDate>Tue, 17 Nov 2026 12:00:00 GMT</pubDate>
                <description><![CDATA[<p>Özet metin.</p><p>]]></description>
                <enclosure url="https://ulkuocaklari.org.tr/uploads/a.jpg" type="image/jpeg" />
              </item>
            </channel></rss>
            """;
        var xmlWithTail = xml.Replace("</description>", new string('c', 500) + " GIZLI-SON</description>", StringComparison.Ordinal);

        var items = ParentSiteListingParser.ParseFeedXml(xmlWithTail, "https://ulkuocaklari.org.tr/feed");

        items.Should().ContainSingle();
        items[0].Title.Should().Be("RSS haber");
        items[0].Kind.Should().Be(ParentSiteItemKind.News);
        items[0].PublishedDate.Should().Be(new DateOnly(2026, 11, 17));
        items[0].ImageUrl.Should().Contain("uploads/a.jpg");
        items[0].Summary.Should().StartWith("Özet metin.");
        items[0].Summary.Should().NotContain("GIZLI-SON");
        items[0].Summary.Should().NotContain("<p>");
    }

    [Fact]
    public void Selector_prefers_rss_and_then_the_first_origin()
    {
        var orgRss = Item("Org RSS", "https://ulkuocaklari.org.tr/blog/i/org-rss");
        var govRss = Item("Gov RSS", "https://ulkuocaklari.gov.tr/blog/i/gov-rss");
        var orgHtml = Item("Org HTML", "https://ulkuocaklari.org.tr/blog/i/org-html");

        ParentSiteFeedSelector.Choose(
        [
            new OriginProbe("https://ulkuocaklari.org.tr", [orgRss], "https://ulkuocaklari.org.tr/feed", [orgHtml], "https://ulkuocaklari.org.tr/blog"),
            new OriginProbe("https://ulkuocaklari.gov.tr", [govRss], "https://ulkuocaklari.gov.tr/rss", [], null),
        ]).Items.Should().ContainSingle().Which.Title.Should().Be("Org RSS");

        ParentSiteFeedSelector.Choose(
        [
            new OriginProbe("https://ulkuocaklari.org.tr", [], null, [orgHtml], "https://ulkuocaklari.org.tr/blog"),
            new OriginProbe("https://ulkuocaklari.gov.tr", [govRss], "https://ulkuocaklari.gov.tr/rss", [], null),
        ]).SourceUrl.Should().Be("https://ulkuocaklari.gov.tr/rss");

        ParentSiteFeedSelector.Choose(
        [
            new OriginProbe("https://ulkuocaklari.org.tr", [], null, [orgHtml], "https://ulkuocaklari.org.tr/blog"),
            new OriginProbe("https://ulkuocaklari.gov.tr", [], null, [Item("Gov HTML", "https://ulkuocaklari.gov.tr/blog/i/x")], "https://ulkuocaklari.gov.tr/blog"),
        ]).Items.Single().Title.Should().Be("Org HTML");
    }

    [Fact]
    public void Home_slots_keep_local_items_first_and_stop_at_the_slot_count()
    {
        var local = new[]
        {
            Card("Manisa Yerel Haber", "/haberler/yerel"),
        };
        var parent = Enumerable.Range(1, 6)
            .Select(i => Item($"GM Haber {i}", $"https://ulkuocaklari.org.tr/blog/i/haber-{i}"))
            .Append(Item("GM Turnuva", "https://ulkuocaklari.org.tr/blog/i/turnuva", ParentSiteItemKind.Event))
            .ToList();

        var news = HomeSlotComposer.Fill(local, parent, HomeSlotComposer.NewsSlotCount, eventsFirst: false);

        news.Should().HaveCount(5);
        news[0].Title.Should().Be("Manisa Yerel Haber");
        news[1].Title.Should().Be("GM Haber 1");
        news.Select(card => card.Title).Should().NotContain("GM Haber 5");
        news.Select(card => card.Title).Should().NotContain("GM Turnuva");

        var events = HomeSlotComposer.Fill(
            [Card("Manisa Yerel Etkinlik", "/etkinlikler/yerel")],
            parent,
            HomeSlotComposer.EventSlotCount,
            eventsFirst: true);

        events.Select(card => card.Title).Should().Equal("Manisa Yerel Etkinlik", "GM Turnuva", "GM Haber 1");
        HomeSlotComposer.Hero(Card("Manisa Yerel Etkinlik", "/etkinlikler/yerel"), parent)!
            .LocalPath.Should().Be("/etkinlikler/yerel");
        HomeSlotComposer.Hero(null, parent)!.Title.Should().Be("GM Turnuva");
    }

    [Fact]
    public void Hub_merge_puts_admin_links_first_and_skips_the_same_url()
    {
        var admin = new[]
        {
            new ParentNewsCardDto(3, "Elle Eklenen GM", "özet", "https://ulkuocaklari.org.tr/blog/i/elle/", new DateOnly(2026, 11, 1), null),
        };
        var fetched = new[]
        {
            Item("Ayni Link", "https://UlkuOcaklari.org.tr/blog/i/elle"),
            Item("GM Haber 1", "https://ulkuocaklari.org.tr/blog/i/haber-1"),
        };

        var merged = HomeSlotComposer.MergeHubNews(admin, fetched);

        merged.Select(card => card.Title).Should().Equal("Elle Eklenen GM", "GM Haber 1");
    }

    private static ParentSiteItem Item(string title, string url, ParentSiteItemKind kind = ParentSiteItemKind.News) =>
        new(title, "özet", null, url, new DateOnly(2026, 11, 17), kind);

    private static HomeFeedCardDto Card(string title, string path) =>
        new(title, null, null, title, null, path, null, null);
}
