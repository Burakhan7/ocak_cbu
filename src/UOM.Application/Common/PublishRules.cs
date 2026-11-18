using UOM.Domain.Common;
using UOM.Domain.Enums;

namespace UOM.Application.Common;

public static class PublishRules
{
    public static bool IsPubliclyVisible(this IPublishable entity, DateTime utcNow) =>
        entity.Status == PublishStatus.Published
        && entity.PublishedAtUtc is { } publishedAt
        && publishedAt <= utcNow;

    public static bool IsAnnouncementListable(DateTime expiresAtUtc, DateTime utcNow) =>
        expiresAtUtc > utcNow;
}