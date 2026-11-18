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
    string? CoverImageAlt);

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
    string? MetaDescription);

public sealed record EventListItemDto(
    int Id,
    string Title,
    string Slug,
    string? Summary,
    DateTime StartsAtUtc,
    DateTime? EndsAtUtc,
    string? VenueName,
    string? CoverImageUrl);

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
    string? MetaDescription);

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
    bool TurnstileEnabled);

public sealed record FacultyOptionDto(int Id, string Name);

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