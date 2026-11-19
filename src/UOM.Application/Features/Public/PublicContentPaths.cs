namespace UOM.Application.Features.Public;

public static class PublicContentPaths
{
    public static string News(string slug) => "/haberler/" + slug.Trim('/');

    public static string Event(string slug) => "/etkinlikler/" + slug.Trim('/');

    public static string Guide(string slug) => "/cbu/rehber/" + slug.Trim('/');

    public static string Announcement(string slug) => "/duyurular/" + slug.Trim('/');

    public static string District(string slug) => "/teskilat/" + slug.Trim('/');

    public static string Page(string slug) => "/" + slug.Trim('/');
}
