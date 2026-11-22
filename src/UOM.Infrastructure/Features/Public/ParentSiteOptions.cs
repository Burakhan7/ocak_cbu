namespace UOM.Infrastructure.Features.Public;

public sealed class ParentSiteOptions
{
    public const string SectionName = "ParentSite";

    public static readonly string[] DefaultOrigins =
    [
        "https://ulkuocaklari.org.tr",
        "https://ulkuocaklari.gov.tr",
    ];

    public bool Enabled { get; set; } = true;

    public int CacheMinutes { get; set; } = 30;

    public string[] Origins { get; set; } = DefaultOrigins;
}
