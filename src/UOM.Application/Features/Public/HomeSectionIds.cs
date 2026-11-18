namespace UOM.Application.Features.Public;

public static class HomeSectionIds
{
    public const string Hero = "Hero";
    public const string QuoteBand = "QuoteBand";
    public const string CbuQuickLinks = "CbuQuickLinks";
    public const string UpcomingEvents = "UpcomingEvents";
    public const string News = "News";
    public const string ParentNews = "ParentNews";
    public const string Projects = "Projects";
    public const string MagazineVideo = "MagazineVideo";
    public const string VisitUs = "VisitUs";

    public static readonly IReadOnlyList<string> DefaultOrder =
    [
        Hero,
        QuoteBand,
        CbuQuickLinks,
        UpcomingEvents,
        News,
        ParentNews,
        Projects,
        MagazineVideo,
        VisitUs,
    ];
}