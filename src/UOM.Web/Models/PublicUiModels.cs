namespace UOM.Web.Models;

public sealed record CoverImageModel(
    string? Url,
    string Alt,
    string ModifierClass = "cover-image--card");

public sealed record PageBannerModel(
    string Title,
    string? Subtitle = null,
    string? CoverImageUrl = null,
    string? CoverImageAlt = null,
    string ModifierClass = "");