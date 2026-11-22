namespace UOM.Application.Features.Public;

public enum ParentSiteItemKind
{
    News = 0,
    Event = 1,
}

public sealed record ParentSiteItem(
    string Title,
    string? Summary,
    string? ImageUrl,
    string ExternalUrl,
    DateOnly? PublishedDate,
    ParentSiteItemKind Kind);

public sealed record ParentSiteSnapshot(IReadOnlyList<ParentSiteItem> Items, string? SourceUrl)
{
    public static ParentSiteSnapshot Empty { get; } = new([], null);
}

public interface IParentSiteFeed
{
    Task<ParentSiteSnapshot> GetAsync(CancellationToken cancellationToken = default);
}
