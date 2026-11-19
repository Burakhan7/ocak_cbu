using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Public;

public sealed class MediaAssetReader(AppDbContext db, IMemoryCache cache) : IMediaAssetReader
{
    private static readonly (string Name, int Width)[] VariantWidths =
    [
        ("thumb", 400),
        ("medium", 960),
        ("large", 1920),
    ];

    public async Task<ResponsiveImageDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var key = PublicCacheKeys.MediaAsset(id);
        if (cache.TryGetValue(key, out ResponsiveImageDto? cached))
        {
            return cached;
        }

        var file = await db.MediaFiles.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (file is null)
        {
            return null;
        }

        Dictionary<string, string>? map = null;
        if (!string.IsNullOrWhiteSpace(file.Variants))
        {
            try
            {
                map = JsonSerializer.Deserialize<Dictionary<string, string>>(file.Variants);
            }
            catch (JsonException)
            {
                map = null;
            }
        }

        var ranked = new List<(string Url, int Width, int Rank)>();
        for (var i = 0; i < VariantWidths.Length; i++)
        {
            var (name, preset) = VariantWidths[i];
            if (map is null || !map.TryGetValue(name, out var path) || string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var url = MediaUrlHelper.ToPublicUrl(path);
            if (url is null)
            {
                continue;
            }

            var width = file.Width is int original && original > 0 ? Math.Min(preset, original) : preset;
            ranked.Add((url, width, i));
        }

        var sources = ranked
            .GroupBy(item => item.Width)
            .Select(group => group.OrderByDescending(item => item.Rank).First())
            .OrderBy(item => item.Width)
            .Select(item => new ResponsiveSourceDto(item.Url, item.Width))
            .ToList();

        var src = sources.FirstOrDefault(s => s.Width <= 960)?.Url
            ?? sources.LastOrDefault()?.Url
            ?? MediaUrlHelper.ToPublicUrl(file.StoragePath);

        if (src is null)
        {
            return null;
        }

        var dto = new ResponsiveImageDto(file.AltText, src, sources, file.Width, file.Height);
        cache.Set(key, dto, TimeSpan.FromMinutes(10));
        return dto;
    }
}
