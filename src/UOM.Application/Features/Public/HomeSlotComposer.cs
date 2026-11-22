using System.Globalization;
using UOM.Application.Features.Public.Models;

namespace UOM.Application.Features.Public;

public static class HomeSlotComposer
{
    public const int NewsSlotCount = 5;
    public const int EventSlotCount = 3;

    public static IReadOnlyList<HomeFeedCardDto> Fill(
        IReadOnlyList<HomeFeedCardDto> local,
        IReadOnlyList<ParentSiteItem> parent,
        int slots,
        bool eventsFirst)
    {
        if (slots <= 0)
        {
            return [];
        }

        var result = new List<HomeFeedCardDto>(Math.Min(slots, local.Count + parent.Count));
        foreach (var card in local)
        {
            if (result.Count >= slots)
            {
                break;
            }

            result.Add(card);
        }

        if (result.Count >= slots)
        {
            return result;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var card in result)
        {
            var key = ParentSiteText.NormalizeUrl(card.ExternalUrl);
            if (key is not null)
            {
                seen.Add(key);
            }
        }

        IEnumerable<ParentSiteItem> ordered = eventsFirst
            ? parent.Where(item => item.Kind == ParentSiteItemKind.Event).Concat(parent.Where(item => item.Kind != ParentSiteItemKind.Event))
            : parent.Where(item => item.Kind != ParentSiteItemKind.Event).Concat(parent.Where(item => item.Kind == ParentSiteItemKind.Event));

        foreach (var item in ordered)
        {
            if (result.Count >= slots)
            {
                break;
            }

            var key = ParentSiteText.NormalizeUrl(item.ExternalUrl);
            if (key is null || !seen.Add(key))
            {
                continue;
            }

            var card = ToExternalCard(item);
            if (card is not null)
            {
                result.Add(card);
            }
        }

        return result;
    }

    public static HomeFeedCardDto? Hero(HomeFeedCardDto? local, IReadOnlyList<ParentSiteItem> parent)
    {
        if (local is not null)
        {
            return local;
        }

        var filled = Fill([], parent, 1, eventsFirst: true);
        return filled.Count == 0 ? null : filled[0];
    }

    public static IReadOnlyList<ParentNewsCardDto> MergeHubNews(
        IReadOnlyList<ParentNewsCardDto> admin,
        IReadOnlyList<ParentSiteItem> fetched)
    {
        var result = new List<ParentNewsCardDto>(admin.Count + fetched.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var card in admin)
        {
            result.Add(card);
            var key = ParentSiteText.NormalizeUrl(card.ExternalUrl);
            if (key is not null)
            {
                seen.Add(key);
            }
        }

        foreach (var item in fetched)
        {
            var key = ParentSiteText.NormalizeUrl(item.ExternalUrl);
            if (key is null || !seen.Add(key))
            {
                continue;
            }

            var card = ToParentCard(item);
            if (card is not null)
            {
                result.Add(card);
            }
        }

        return result;
    }

    public static HomeFeedCardDto? ToExternalCard(ParentSiteItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Title) || ParentSiteText.NormalizeUrl(item.ExternalUrl) is null)
        {
            return null;
        }

        return new HomeFeedCardDto(
            item.Title.Trim(),
            ParentSiteText.Summarize(item.Summary),
            ParentSiteText.SafeImageUrl(item.ImageUrl),
            item.Title.Trim(),
            FormatDate(item.PublishedDate),
            null,
            item.ExternalUrl.Trim(),
            "Genel Merkez");
    }

    private static ParentNewsCardDto? ToParentCard(ParentSiteItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Title) || ParentSiteText.NormalizeUrl(item.ExternalUrl) is null)
        {
            return null;
        }

        return new ParentNewsCardDto(
            0,
            item.Title.Trim(),
            ParentSiteText.Summarize(item.Summary),
            item.ExternalUrl.Trim(),
            item.PublishedDate ?? DateOnly.MinValue,
            ParentSiteText.SafeImageUrl(item.ImageUrl));
    }

    private static string? FormatDate(DateOnly? date) =>
        date is { Year: >= 2000 } value
            ? value.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("tr-TR"))
            : null;
}
