using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Public;

public sealed class SiteSettingsProvider : ISiteSettingsProvider
{
    private const string CacheKey = "site-settings-public";
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public SiteSettingsProvider(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<SiteSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out SiteSettingsDto? cached) && cached is not null)
        {
            return cached;
        }

        var row = await _db.SiteSettings
            .AsNoTracking()
            .Include(s => s.LogoImage)
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var logoUrl = MediaUrlHelper.ResolveUrl(row?.LogoImage?.StoragePath, row?.LogoImage?.Variants, "thumb");
        var dto = new SiteSettingsDto(
            row?.SiteName ?? "Ülkü Ocakları Manisa",
            row?.Tagline,
            row?.Address,
            row?.Phone,
            row?.Email,
            row?.WhatsAppChannelUrl,
            row?.OpeningHoursText,
            row?.MapLat,
            row?.MapLng,
            row?.TurnstileEnabled == true,
            logoUrl);

        _cache.Set(CacheKey, dto, TimeSpan.FromMinutes(5));
        return dto;
    }

    public async Task<IReadOnlyList<string>> GetHomeSectionOrderAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.SiteSettings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(row?.HomeSectionsJson))
        {
            return HomeSectionIds.DefaultOrder;
        }

        try
        {
            var sections = JsonSerializer.Deserialize<List<HomeSectionConfig>>(row.HomeSectionsJson);
            if (sections is null || sections.Count == 0)
            {
                return HomeSectionIds.DefaultOrder;
            }

            return sections
                .Where(s => s.Visible)
                .OrderBy(s => s.Order)
                .Select(s => s.Id)
                .ToList();
        }
        catch (JsonException)
        {
            return HomeSectionIds.DefaultOrder;
        }
    }

    private sealed record HomeSectionConfig(string Id, int Order, bool Visible);
}