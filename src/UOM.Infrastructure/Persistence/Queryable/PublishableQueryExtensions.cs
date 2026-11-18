using System.Linq.Expressions;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Domain.Common;
using UOM.Domain.Entities;
using UOM.Domain.Enums;

namespace UOM.Infrastructure.Persistence.Queryable;

/// <summary>
/// Public-facing query helpers (admin queries should use <c>IgnoreQueryFilters()</c> when needed).
/// Visibility rules for <see cref="IPublishable"/> are centralized here for reuse in Faz 3+.
/// </summary>
public static class PublishableQueryExtensions
{
    public static IQueryable<T> WherePubliclyVisible<T>(this IQueryable<T> query, IClock clock)
        where T : class, IPublishable =>
        query.Where(e =>
            e.Status == PublishStatus.Published
            && e.PublishedAtUtc != null
            && e.PublishedAtUtc <= clock.UtcNow);

    public static Expression<Func<T, bool>> PubliclyVisiblePredicate<T>(IClock clock)
        where T : class, IPublishable =>
        e => e.Status == PublishStatus.Published
             && e.PublishedAtUtc != null
             && e.PublishedAtUtc <= clock.UtcNow;

    public static IQueryable<Announcement> WhereListableAnnouncements(
        this IQueryable<Announcement> query,
        IClock clock) =>
        query
            .WherePubliclyVisible(clock)
            .Where(a => a.ExpiresAtUtc > clock.UtcNow);
}