using UOM.Domain.Common;

namespace UOM.Domain.Entities;

public class SiteSettings : BaseEntity
{
    public string? SiteName { get; set; }

    public int? LogoImageId { get; set; }

    public MediaFile? LogoImage { get; set; }

    /// <summary>Cover photo for the homepage CBÜ Hub lead card. Null shows a gradient.</summary>
    public int? CbuHubCoverImageId { get; set; }

    public MediaFile? CbuHubCoverImage { get; set; }

    public string? Tagline { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? WhatsAppChannelUrl { get; set; }

    public string? OpeningHoursText { get; set; }

    public double? MapLat { get; set; }

    public double? MapLng { get; set; }

    /// <summary>JSON: Instagram, X, YouTube, Facebook, TikTok.</summary>
    public string? SocialLinks { get; set; }

    /// <summary>JSON links for Genel Merkez social media.</summary>
    public string? GenelMerkezSocialLinks { get; set; }

    public string? FooterText { get; set; }

    public bool AnalyticsEnabled { get; set; }

    public bool TurnstileEnabled { get; set; }

    /// <summary>Home page section order/visibility JSON (Faz 3+).</summary>
    public string? HomeSectionsJson { get; set; }
}