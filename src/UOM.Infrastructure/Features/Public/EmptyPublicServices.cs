using UOM.Application.Common;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Enums;

namespace UOM.Infrastructure.Features.Public;

internal sealed class EmptySiteSettingsProvider : ISiteSettingsProvider
{
    public Task<SiteSettingsDto> GetAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new SiteSettingsDto(
            "Ülkü Ocakları Manisa",
            null, null, null, null, null, null, null, null, false));

    public Task<IReadOnlyList<string>> GetHomeSectionOrderAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(HomeSectionIds.DefaultOrder);
}

internal sealed class EmptyPublicNewsService : IPublicNewsService
{
    public Task<PagedResult<NewsListItemDto>> ListAsync(NewsListQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult(EmptyPage<NewsListItemDto>(query));

    public Task<NewsDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        Task.FromResult<NewsDetailDto?>(null);

    public Task<IReadOnlyList<NewsListItemDto>> GetFeaturedForHomeAsync(int featuredCount, int regularCount, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NewsListItemDto>>([]);

    public Task<IReadOnlyList<NewsListItemDto>> GetRelatedAsync(int categoryId, int excludeId, int count, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NewsListItemDto>>([]);

    private static PagedResult<T> EmptyPage<T>(NewsListQuery query) => new()
    {
        Items = [],
        Page = query.Page,
        PageSize = query.PageSize,
        TotalCount = 0,
    };
}

internal sealed class EmptyPublicEventService : IPublicEventService
{
    public Task<PagedResult<EventListItemDto>> ListAsync(EventListQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<EventListItemDto>
        {
            Items = [],
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = 0,
        });

    public Task<EventDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        Task.FromResult<EventDetailDto?>(null);

    public Task<EventDetailDto?> GetNextUpcomingAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<EventDetailDto?>(null);

    public Task<IReadOnlyList<EventListItemDto>> GetUpcomingAsync(int count, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EventListItemDto>>([]);

    public Task<byte[]> BuildCalendarFeedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Array.Empty<byte>());

    public Task<byte[]> BuildSingleEventIcsAsync(string slug, CancellationToken cancellationToken = default) =>
        Task.FromResult(Array.Empty<byte>());
}

internal sealed class EmptyPublicPageService : IPublicPageService
{
    public Task<StaticPageDto?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        Task.FromResult<StaticPageDto?>(null);

    public Task<IReadOnlyList<StaticPageLinkDto>> GetFooterPagesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StaticPageLinkDto>>([]);

    public Task<RedirectDto?> FindRedirectAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult<RedirectDto?>(null);
}

internal sealed class EmptyContactFormService : IContactFormService
{
    public Task<IReadOnlyList<FacultyOptionDto>> GetFacultyOptionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<FacultyOptionDto>>([]);

    public Task<Result> SubmitAsync(ContactFormRequest request, string? ipAddress, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Fail("Veritabanı bağlantısı yok."));
}

internal sealed class EmptyPublicCampusHubService : IPublicCampusHubService
{
    public Task<CampusHubDto> GetHubAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new CampusHubDto(null, [], [], [], 0, [], null, null, null));
}

internal sealed class EmptyPublicGuideService : IPublicGuideService
{
    public Task<IReadOnlyList<GuideListItemDto>> ListPublishedAsync(GuideCategory? category, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GuideListItemDto>>([]);

    public Task<GuideDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        Task.FromResult<GuideDetailDto?>(null);
}

internal sealed class EmptyPublicResourceService : IPublicResourceService
{
    public Task<PagedResult<ResourceListItemDto>> ListAsync(ResourceListQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<ResourceListItemDto> { Items = [], Page = query.Page, PageSize = query.PageSize, TotalCount = 0 });

    public Task<ResourceDownloadDto?> GetForDownloadAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult<ResourceDownloadDto?>(null);

    public Task<bool> TryIncrementDownloadCountAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}

internal sealed class EmptyPublicAnnouncementService : IPublicAnnouncementService
{
    public Task<IReadOnlyList<AnnouncementListItemDto>> ListListableAsync(AnnouncementType? type, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AnnouncementListItemDto>>([]);

    public Task<AnnouncementListItemDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        Task.FromResult<AnnouncementListItemDto?>(null);
}