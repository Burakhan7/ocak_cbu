using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using UOM.Application.Abstractions;
using UOM.Application.Caching;
using UOM.Domain.Entities;
using UOM.Infrastructure.Features.Public;

namespace UOM.Infrastructure.Caching;

public sealed class NullCacheInvalidator : ICacheInvalidator
{
    public Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] tags) =>
        Task.CompletedTask;
}

public sealed class ContentCacheInvalidationInterceptor(ICacheInvalidator cacheInvalidator, IMemoryCache memoryCache)
    : SaveChangesInterceptor
{
    private readonly ConditionalWeakTable<DbContext, CacheFlushBox> _pending = new();

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await FlushAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        FlushAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    private void Capture(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var tags = new HashSet<string>(StringComparer.Ordinal);
        var clearSettings = false;
        var clearQuotes = false;
        var clearRedirects = false;
        var mediaIds = new List<int>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added
                or EntityState.Modified
                or EntityState.Deleted))
            {
                continue;
            }

            if (entry.Entity is Redirect && entry.State == EntityState.Modified)
            {
                var meaningful = entry.Properties.Any(property =>
                    property.IsModified
                    && property.Metadata.Name is not ("HitCount" or "UpdatedAtUtc" or "UpdatedByUserId"));
                if (!meaningful)
                {
                    continue;
                }
            }

            switch (entry.Entity)
            {
                case NewsPost or NewsCategory:
                    tags.Add(CacheTags.News);
                    tags.Add(CacheTags.Home);
                    tags.Add(CacheTags.Sitemap);
                    tags.Add(CacheTags.Feed);
                    break;
                case Event or Venue:
                    tags.Add(CacheTags.Events);
                    tags.Add(CacheTags.Home);
                    tags.Add(CacheTags.Campus);
                    tags.Add(CacheTags.Sitemap);
                    break;
                case GuideArticle:
                    tags.Add(CacheTags.Guides);
                    tags.Add(CacheTags.Campus);
                    tags.Add(CacheTags.Sitemap);
                    break;
                case Announcement:
                    tags.Add(CacheTags.Announcements);
                    tags.Add(CacheTags.Home);
                    tags.Add(CacheTags.Campus);
                    tags.Add(CacheTags.Sitemap);
                    break;
                case Resource or Campus or Faculty:
                    tags.Add(CacheTags.Resources);
                    tags.Add(CacheTags.Campus);
                    break;
                case Quote:
                    tags.Add(CacheTags.Home);
                    clearQuotes = true;
                    break;
                case SiteSettings:
                    foreach (var tag in CacheTags.AllPublic)
                    {
                        tags.Add(tag);
                    }

                    clearSettings = true;
                    break;
                case ParentNewsLink or ParentProject or MagazineIssue:
                    tags.Add(CacheTags.Parent);
                    tags.Add(CacheTags.Home);
                    break;
                case Video:
                    tags.Add(CacheTags.Videos);
                    tags.Add(CacheTags.Parent);
                    tags.Add(CacheTags.Home);
                    break;
                case District or BoardMember:
                    tags.Add(CacheTags.Organization);
                    tags.Add(CacheTags.Sitemap);
                    break;
                case StaticPage:
                    tags.Add(CacheTags.Pages);
                    tags.Add(CacheTags.Sitemap);
                    break;
                case MediaFile media:
                    foreach (var tag in CacheTags.AllPublic)
                    {
                        tags.Add(tag);
                    }

                    mediaIds.Add(media.Id);
                    clearSettings = true;
                    break;
                case Redirect:
                    clearRedirects = true;
                    break;
            }
        }

        _pending.GetOrCreateValue(context).Flush = new CacheFlush(tags, clearSettings, clearQuotes, clearRedirects, mediaIds);
    }

    private async Task FlushAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null || !_pending.TryGetValue(context, out var box) || box.Flush is not { } flush)
        {
            return;
        }

        box.Flush = null;
        if (flush.ClearSettings)
        {
            memoryCache.Remove(PublicCacheKeys.SiteSettings);
        }

        if (flush.ClearQuotes)
        {
            memoryCache.Remove(PublicQuoteService.CacheKey);
        }

        if (flush.ClearRedirects)
        {
            memoryCache.Remove(PublicCacheKeys.Redirects);
        }

        foreach (var id in flush.MediaIds)
        {
            memoryCache.Remove(PublicCacheKeys.MediaAsset(id));
        }

        if (flush.Tags.Count == 0)
        {
            return;
        }

        try
        {
            await cacheInvalidator.InvalidateAsync(cancellationToken, flush.Tags.ToArray());
        }
        catch (Exception)
        {
            // Kayıt başarılı. Önbellek temizliği bir sonraki süre dolumuna kalır.
        }
    }

    private sealed class CacheFlushBox
    {
        public CacheFlush? Flush { get; set; }
    }

    private sealed record CacheFlush(
        HashSet<string> Tags,
        bool ClearSettings,
        bool ClearQuotes,
        bool ClearRedirects,
        List<int> MediaIds);
}
