using UOM.Application.Common;
using UOM.Domain.Enums;

namespace UOM.Application.Features.Public.Models;

public sealed record NewsListQuery(
    NewsScope? Scope,
    int? CategoryId,
    int Page = 1,
    int PageSize = 12);

public sealed record EventListQuery(
    bool UpcomingOnly,
    int Page = 1,
    int PageSize = 12);

public sealed record NewsListItemDto(
    int Id,
    string Title,
    string Slug,
    string? Summary,
    NewsScope Scope,
    string? CategoryName,
    DateTime? PublishedAtUtc,
    string? CoverImageUrl,
    string? CoverImageAlt,
    int? CoverImageId = null);

public sealed record NewsDetailDto(
    int Id,
    string Title,
    string Slug,
    string? Summary,
    string BodyHtml,
    NewsScope Scope,
    string? CategoryName,
    int? CategoryId,
    DateTime? PublishedAtUtc,
    string? CoverImageUrl,
    string? CoverImageAlt,
    string? MetaTitle,
    string? MetaDescription,
    int? CoverImageId = null);

public sealed record EventListItemDto(
    int Id,
    string Title,
    string Slug,
    string? Summary,
    DateTime StartsAtUtc,
    DateTime? EndsAtUtc,
    string? VenueName,
    string? CoverImageUrl,
    int? CoverImageId = null);

public sealed record EventDetailDto(
    int Id,
    string Title,
    string Slug,
    string? Summary,
    string BodyHtml,
    DateTime StartsAtUtc,
    DateTime? EndsAtUtc,
    EventAudience Audience,
    EventType Type,
    EventCtaType CallToActionType,
    string? CallToActionUrl,
    EventCtaDisplay Cta,
    VenueDto? Venue,
    string? CoverImageUrl,
    string? CoverImageAlt,
    bool IsPast,
    string? MetaTitle,
    string? MetaDescription,
    int? CoverImageId = null);

public sealed record VenueDto(
    string Name,
    string? Address,
    double? Latitude,
    double? Longitude);

public sealed record EventCtaDisplay(
    EventCtaType Type,
    string PrimaryLabel,
    string? PrimaryUrl,
    bool OpensInNewTab,
    string? SecondaryHint);

public sealed record StaticPageDto(
    string Title,
    string Slug,
    string BodyHtml,
    string? MetaTitle,
    string? MetaDescription);

public sealed record StaticPageLinkDto(string Title, string Slug);

public sealed record RedirectDto(string ToPath, int StatusCode);

public sealed record RedirectMatch(int Id, string ToPath, int StatusCode);

public sealed record SiteSettingsDto(
    string SiteName,
    string? Tagline,
    string? Address,
    string? Phone,
    string? Email,
    string? WhatsAppChannelUrl,
    string? OpeningHoursText,
    double? MapLat,
    double? MapLng,
    bool TurnstileEnabled,
    string? LogoUrl,
    string? SocialLinksJson = null,
    string? CbuHubCoverUrl = null);

public sealed record FacultyOptionDto(int Id, string Name);

public sealed record CampusHubDto(
    string? IntroText,
    IReadOnlyList<GuideListItemDto> NewStudentGuides,
    IReadOnlyList<EventListItemDto> CampusWeekEvents,
    IReadOnlyList<AnnouncementListItemDto> RecentAnnouncements,
    int PublishedResourceCount,
    IReadOnlyList<CampusMapPinDto> Campuses,
    double? LodgeLatitude,
    double? LodgeLongitude,
    string? WhatsAppChannelUrl);

public sealed record CampusMapPinDto(
    string Name,
    string? ShortName,
    double Latitude,
    double Longitude,
    double? DistanceToLodgeKm,
    string? CoverImageUrl,
    string? CoverImageAlt,
    int? CoverImageId = null);

public sealed record GuideListItemDto(
    string Title,
    string Slug,
    string? Summary,
    GuideCategory Category,
    DateTime? LastReviewedAtUtc,
    string? CoverImageUrl,
    string? CoverImageAlt,
    int? CoverImageId = null);

public sealed record GuideDetailDto(
    string Title,
    string Slug,
    string? Summary,
    string BodyHtml,
    GuideCategory Category,
    DateTime? LastReviewedAtUtc,
    string? CoverImageUrl,
    string? CoverImageAlt,
    string? MetaTitle,
    string? MetaDescription,
    int? CoverImageId = null);

public sealed record ResourceListQuery(int? FacultyId, string? CourseCode, int Page = 1, int PageSize = 24);

public sealed record ResourceListItemDto(
    int Id,
    string Title,
    string? Description,
    ResourceType Type,
    string? FacultyName,
    string? CourseCode,
    string? Semester,
    string? ExternalUrl);

public sealed record ResourceDownloadDto(
    int Id,
    ResourceType Type,
    string Title,
    string? ExternalUrl,
    string? FileRelativePath,
    string? ContentType,
    string? OriginalFileName);

public sealed record AnnouncementListItemDto(
    int Id,
    string Title,
    string Slug,
    string Body,
    AnnouncementType Type,
    DateTime? DeadlineUtc,
    DateTime ExpiresAtUtc,
    string? ExternalUrl,
    bool IsPinned,
    bool IsUrgent,
    string? CoverImageUrl,
    string? CoverImageAlt,
    int? CoverImageId = null);

public sealed record SearchHitDto(
    string Kind,
    string Title,
    string? Excerpt,
    string Url,
    DateTime? WhenUtc);

public sealed record SearchResultDto(
    string Query,
    bool Enabled,
    bool UsedFullText,
    IReadOnlyList<SearchHitDto> Hits);

public sealed record ResponsiveSourceDto(string Url, int Width);

public sealed record ResponsiveImageDto(
    string Alt,
    string Src,
    IReadOnlyList<ResponsiveSourceDto> Sources,
    int? Width,
    int? Height);

public sealed class ContactFormRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public ContactSubject Subject { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool IsStudentAtCbu { get; set; }

    public int? FacultyId { get; set; }

    public bool KvkkConsent { get; set; }

  // Honeypot — must stay empty
    public string? Website { get; set; }

    public string? TurnstileToken { get; set; }
}

public sealed record QuoteDto(int Id, string Text, QuoteAuthor Author, string AuthorLabel, string? SourceNote);

public sealed record ParentNewsCardDto(
    int Id,
    string Title,
    string? Summary,
    string ExternalUrl,
    DateOnly PublishedDate,
    string? ImageUrl);

public sealed record ParentProjectCardDto(
    int Id,
    string Name,
    string? ShortDescription,
    string ExternalUrl,
    string? LogoUrl,
    string? LogoAlt);

public sealed record MagazineCardDto(
    int Id,
    string Title,
    DateOnly IssueDate,
    string ExternalUrl,
    string? CoverUrl,
    string? CoverAlt);

public sealed record VideoListItemDto(
    int Id,
    string Title,
    string YouTubeVideoId,
    VideoSource Source,
    DateOnly PublishedDate,
    bool IsFeatured);

public sealed record MagazineVideoHomeDto(MagazineCardDto? LatestMagazine, VideoListItemDto? FeaturedVideo);

public sealed record ParentHubPageDto(
    PagedResult<ParentNewsCardDto> News,
    IReadOnlyList<ParentProjectCardDto> Projects,
    IReadOnlyList<MagazineCardDto> Magazines,
    IReadOnlyList<VideoListItemDto> Videos);

public sealed record BoardMemberPublicDto(
    int Id,
    string FullName,
    string Title,
    string? PhotoUrl,
    string? PhotoAlt,
    bool ShowPhoto);

public sealed record DistrictMapPinDto(
    int Id,
    string Name,
    string Slug,
    double Latitude,
    double Longitude,
    string? PresidentName,
    string? Address,
    string? Phone,
    bool IsActive);

public sealed record DistrictDetailDto(
    int Id,
    string Name,
    string Slug,
    string? PresidentName,
    string? Address,
    string? Phone,
    double? Latitude,
    double? Longitude,
    bool IsActive);

public sealed record OrganizationPageDto(
    IReadOnlyList<BoardMemberPublicDto> BoardMembers,
    IReadOnlyList<DistrictMapPinDto> DistrictsForMap,
    IReadOnlyList<DistrictMapPinDto> DistrictList);