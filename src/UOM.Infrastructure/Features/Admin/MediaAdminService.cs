using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Domain.Entities;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Admin;

public sealed class MediaAdminService(
    AppDbContext db,
    IImageProcessor imageProcessor,
    IClock clock,
    IConfiguration configuration,
    IAuditLogger auditLogger)
{
    private readonly long _maxImageBytes = configuration.GetValue("Storage:MaxImageMb", 10) * 1024L * 1024L;

    public Task<List<MediaFile>> ListRecentAsync(int take, CancellationToken cancellationToken = default) =>
        db.MediaFiles
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<Result<MediaFile>> UploadImageAsync(
        Stream stream,
        long sizeBytes,
        string fileName,
        string altText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(altText))
        {
            return Result<MediaFile>.Fail("Alt metin zorunludur.");
        }

        if (sizeBytes > _maxImageBytes)
        {
            return Result<MediaFile>.Fail("Görsel boyutu sınırı aşıldı.");
        }

        if (!await IsImageAsync(stream, cancellationToken))
        {
            return Result<MediaFile>.Fail("Geçersiz görsel dosyası.");
        }

        var id = Guid.NewGuid().ToString("N");
        var folder = $"images/{clock.UtcNow:yyyy/MM}";
        stream.Position = 0;
        var processed = await imageProcessor.ProcessUploadAsync(stream, folder, id, cancellationToken);

        var media = new MediaFile
        {
            StoragePath = processed.OriginalRelativePath,
            OriginalFileName = fileName,
            ContentType = "image/webp",
            SizeBytes = sizeBytes,
            Width = processed.Width,
            Height = processed.Height,
            AltText = altText.Trim(),
            Variants = JsonSerializer.Serialize(processed.WebpVariantPaths),
        };

        db.MediaFiles.Add(media);
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("Create", nameof(MediaFile), media.Id, new { media.OriginalFileName }, cancellationToken);
        return Result<MediaFile>.Ok(media);
    }

    private static async Task<bool> IsImageAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[12];
        var read = await stream.ReadAsync(header.AsMemory(0, 12), cancellationToken);
        stream.Position = 0;
        if (read < 4)
        {
            return false;
        }

        if (header[0] == 0xFF && header[1] == 0xD8)
        {
            return true;
        }

        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
        {
            return true;
        }

        return read >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
               && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;
    }
}