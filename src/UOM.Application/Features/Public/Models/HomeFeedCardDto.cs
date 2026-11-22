namespace UOM.Application.Features.Public.Models;

public sealed record HomeFeedCardDto(
    string Title,
    string? Summary,
    string? ImageUrl,
    string? ImageAlt,
    string? WhenText,
    string? LocalPath,
    string? ExternalUrl,
    string? Badge);
