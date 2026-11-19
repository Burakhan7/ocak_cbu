using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UOM.Application.Abstractions;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Entities;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Public;

public sealed class RedirectService(AppDbContext db, IMemoryCache cache) : IRedirectRecorder, IPublicRedirectService
{
    public async Task StagePermanentAsync(string fromPath, string toPath, CancellationToken cancellationToken = default)
    {
        var from = RedirectPath.Normalize(fromPath);
        var to = RedirectPath.Normalize(toPath);
        if (!RedirectPath.IsLocalPath(from) || !RedirectPath.IsLocalPath(to))
        {
            return;
        }

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var existing = await db.Redirects.FirstOrDefaultAsync(r => r.FromPath == from, cancellationToken);
        if (existing is null)
        {
            db.Redirects.Add(new Redirect
            {
                FromPath = from,
                ToPath = to,
                StatusCode = 301,
            });
        }
        else
        {
            existing.ToPath = to;
            existing.StatusCode = 301;
        }

        var chained = await db.Redirects
            .Where(r => r.ToPath == from && r.FromPath != to)
            .ToListAsync(cancellationToken);
        foreach (var item in chained)
        {
            if (!string.Equals(item.FromPath, to, StringComparison.OrdinalIgnoreCase))
            {
                item.ToPath = to;
            }
        }

        cache.Remove(PublicCacheKeys.Redirects);
    }

    public async Task<RedirectMatch?> FindAsync(string requestPath, CancellationToken cancellationToken = default)
    {
        var map = await MapAsync(cancellationToken);
        var key = RedirectPath.Normalize(requestPath);
        if (!map.TryGetValue(key, out var match))
        {
            return null;
        }

        if (string.Equals(RedirectPath.Normalize(match.ToPath), key, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return match;
    }

    public async Task RegisterHitAsync(string requestPath, CancellationToken cancellationToken = default)
    {
        var key = RedirectPath.Normalize(requestPath);
        var row = await db.Redirects.FirstOrDefaultAsync(
            r => r.FromPath == key || r.FromPath == requestPath,
            cancellationToken);
        if (row is null)
        {
            return;
        }

        row.HitCount++;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, RedirectMatch>> MapAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(PublicCacheKeys.Redirects, out IReadOnlyDictionary<string, RedirectMatch>? cached)
            && cached is not null)
        {
            return cached;
        }

        var rows = await db.Redirects
            .AsNoTracking()
            .Select(r => new { r.Id, r.FromPath, r.ToPath, r.StatusCode })
            .ToListAsync(cancellationToken);

        var map = new Dictionary<string, RedirectMatch>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var from = RedirectPath.Normalize(row.FromPath);
            if (!RedirectPath.IsLocalPath(from))
            {
                continue;
            }

            var status = row.StatusCode is 301 or 302 ? row.StatusCode : 301;
            map[from] = new RedirectMatch(row.Id, RedirectPath.Normalize(row.ToPath), status);
        }

        cache.Set(PublicCacheKeys.Redirects, map, TimeSpan.FromMinutes(5));
        return map;
    }
}
