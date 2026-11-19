using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Domain.Entities;
using UOM.Infrastructure.Features.Public;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Admin;

public sealed class SiteSettingsAdminService(AppDbContext db, IAuditLogger auditLogger, IMemoryCache cache)
{
    public async Task<SiteSettings> GetSingleAsync(CancellationToken cancellationToken = default)
    {
        var settings = await db.SiteSettings.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new SiteSettings();
            db.SiteSettings.Add(settings);
            await db.SaveChangesAsync(cancellationToken);
        }

        return settings;
    }

    public async Task<Result<SiteSettings>> SaveAsync(SiteSettings model, CancellationToken cancellationToken = default)
    {
        var existing = await db.SiteSettings.FirstOrDefaultAsync(s => s.Id == model.Id, cancellationToken);
        if (existing is null)
        {
            return Result<SiteSettings>.Fail("Site ayarları bulunamadı.");
        }

        existing.SiteName = model.SiteName;
        existing.Tagline = model.Tagline;
        existing.Address = model.Address;
        existing.Phone = model.Phone;
        existing.Email = model.Email;
        existing.WhatsAppChannelUrl = model.WhatsAppChannelUrl;
        existing.OpeningHoursText = model.OpeningHoursText;
        existing.MapLat = model.MapLat;
        existing.MapLng = model.MapLng;
        existing.SocialLinks = model.SocialLinks;
        existing.GenelMerkezSocialLinks = model.GenelMerkezSocialLinks;
        existing.FooterText = model.FooterText;
        existing.AnalyticsEnabled = model.AnalyticsEnabled;
        existing.TurnstileEnabled = model.TurnstileEnabled;
        existing.LogoImageId = model.LogoImageId;

        await db.SaveChangesAsync(cancellationToken);
        cache.Remove(SiteSettingsProvider.CacheKey);
        await auditLogger.LogAsync("Update", nameof(SiteSettings), existing.Id, null, cancellationToken);
        return Result<SiteSettings>.Ok(existing);
    }
}