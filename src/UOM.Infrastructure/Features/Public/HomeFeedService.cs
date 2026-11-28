using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Application.Formatting;

namespace UOM.Infrastructure.Features.Public;

public sealed class HomeFeedService(
    IPublicNewsService news,
    IPublicEventService events,
    IParentSiteFeed parentSite) : IHomeFeedService
{
    public async Task<IReadOnlyList<HomeFeedCardDto>> GetNewsAsync(CancellationToken cancellationToken = default)
    {
        var local = await news.GetFeaturedForHomeAsync(1, 4, cancellationToken);
        var cards = local.Select(MapNews).ToList();
        if (cards.Count >= HomeSlotComposer.NewsSlotCount)
        {
            return cards;
        }

        var feed = await parentSite.GetAsync(cancellationToken);
        return HomeSlotComposer.Fill(cards, feed.Items, HomeSlotComposer.NewsSlotCount, eventsFirst: false);
    }

    public async Task<IReadOnlyList<HomeFeedCardDto>> GetUpcomingEventsAsync(CancellationToken cancellationToken = default)
    {
        var local = await events.GetUpcomingAsync(HomeSlotComposer.EventSlotCount, cancellationToken);
        var cards = local.Select(MapEvent).ToList();
        if (cards.Count >= HomeSlotComposer.EventSlotCount)
        {
            return cards;
        }

        var feed = await parentSite.GetAsync(cancellationToken);
        return HomeSlotComposer.Fill(cards, feed.Items, HomeSlotComposer.EventSlotCount, eventsFirst: true);
    }

    public async Task<IReadOnlyList<HomeFeedCardDto>> GetShowcaseAsync(CancellationToken cancellationToken = default)
    {
        var localNews = await news.GetFeaturedForHomeAsync(1, HomeSlotComposer.ShowcaseSlotCount - 1, cancellationToken);
        var localEvents = await events.GetUpcomingAsync(HomeSlotComposer.ShowcaseSlotCount, cancellationToken);
        var newsCards = localNews.Select(item => MapNews(item) with { Badge = "Haber" }).ToList();
        var eventCards = localEvents.Select(item => MapEvent(item) with { Badge = "Etkinlik" }).ToList();
        if (newsCards.Count + eventCards.Count >= HomeSlotComposer.ShowcaseSlotCount)
        {
            return HomeSlotComposer.Showcase(newsCards, eventCards, [], HomeSlotComposer.ShowcaseSlotCount);
        }

        var feed = await parentSite.GetAsync(cancellationToken);
        return HomeSlotComposer.Showcase(newsCards, eventCards, feed.Items, HomeSlotComposer.ShowcaseSlotCount);
    }

    public async Task<HomeFeedCardDto?> GetHeroAsync(CancellationToken cancellationToken = default)
    {
        var next = await events.GetNextUpcomingAsync(cancellationToken);
        if (next is not null)
        {
            return MapDetail(next);
        }

        var feed = await parentSite.GetAsync(cancellationToken);
        return HomeSlotComposer.Hero(null, feed.Items);
    }

    private static HomeFeedCardDto MapNews(NewsListItemDto item) =>
        new(
            item.Title,
            item.Summary,
            item.CoverImageUrl,
            item.CoverImageAlt ?? item.Title,
            item.PublishedAtUtc?.ToLocalDisplay(),
            "/haberler/" + item.Slug,
            null,
            item.Scope.ToString());

    private static HomeFeedCardDto MapEvent(EventListItemDto item) =>
        new(
            item.Title,
            item.Summary,
            item.CoverImageUrl,
            item.Title,
            item.StartsAtUtc.ToLocalDisplay(),
            "/etkinlikler/" + item.Slug,
            null,
            null);

    private static HomeFeedCardDto MapDetail(EventDetailDto item) =>
        new(
            item.Title,
            item.Summary,
            item.CoverImageUrl,
            item.CoverImageAlt ?? item.Title,
            item.StartsAtUtc.ToLocalDisplay(),
            "/etkinlikler/" + item.Slug,
            null,
            null);
}
