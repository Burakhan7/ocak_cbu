namespace UOM.Application.Features.Public;

public sealed record OriginProbe(
    string Origin,
    IReadOnlyList<ParentSiteItem> RssItems,
    string? RssUrl,
    IReadOnlyList<ParentSiteItem> HtmlItems,
    string? HtmlUrl);

public static class ParentSiteFeedSelector
{
    public static ParentSiteSnapshot Choose(IReadOnlyList<OriginProbe> probes)
    {
        foreach (var probe in probes)
        {
            if (probe.RssItems.Count > 0)
            {
                return new ParentSiteSnapshot(probe.RssItems, probe.RssUrl);
            }
        }

        foreach (var probe in probes)
        {
            if (probe.HtmlItems.Count > 0)
            {
                return new ParentSiteSnapshot(probe.HtmlItems, probe.HtmlUrl);
            }
        }

        return ParentSiteSnapshot.Empty;
    }
}
