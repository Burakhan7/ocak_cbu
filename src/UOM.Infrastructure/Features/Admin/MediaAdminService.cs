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
    IFileStorage fileStorage,
    IClock clock,
    IConfiguration configuration,
    IAuditLogger auditLogger)
{
    private readonly long _maxImageBytes = configuration.GetValue("Storage:MaxImageMb", 10) * 1024L * 1024L;
    private readonly long _maxFileBytes = configuration.GetValue("Storage:MaxFileMb", 25) * 1024L * 1024L;

    public Task<MediaFile?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.MediaFiles.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<Result<int?>> ApplyCoverImageAsync(
        int? currentCoverImageId,
        bool removeCoverImage,
        Stream? uploadStream,
        long uploadSizeBytes,
        string? uploadFileName,
        string? altText,
        string defaultAltText,
        CancellationToken cancellationToken = default)
    {
        if (removeCoverImage)
        {
            return Result<int?>.Ok(null);
        }

        if (uploadStream is not null && uploadSizeBytes > 0)
        {
            var alt = string.IsNullOrWhiteSpace(altText) ? defaultAltText : altText.Trim();
            var upload = await UploadImageAsync(
                uploadStream,
                uploadSizeBytes,
                uploadFileName ?? "cover.webp",
                alt,
                cancellationToken);
            if (!upload.Succeeded)
            {
                return Result<int?>.Fail(upload.Error ?? "Görsel yüklenemedi.");
            }

            return Result<int?>.Ok(upload.Value!.Id);
        }

        return Result<int?>.Ok(currentCoverImageId);
    }

    public async Task<Result<int?>> ApplyResourceFileAsync(
        int? currentFileId,
        bool removeResourceFile,
        Stream? uploadStream,
        long uploadSizeBytes,
        string? uploadFileName,
        string defaultAltText,
        CancellationToken cancellationToken = default)
    {
        if (removeResourceFile)
        {
            return Result<int?>.Ok(null);
        }

        if (uploadStream is not null && uploadSizeBytes > 0)
        {
            var upload = await UploadResourceFileAsync(
                uploadStream,
                uploadSizeBytes,
                uploadFileName ?? "resource.bin",
                defaultAltText,
                cancellationToken);
            if (!upload.Succeeded)
            {
                return Result<int?>.Fail(upload.Error ?? "Dosya yüklenemedi.");
            }

            return Result<int?>.Ok(upload.Value!.Id);
        }

        return Result<int?>.Ok(currentFileId);
    }

    public async Task<Result<MediaFile>> UploadResourceFileAsync(
        Stream stream,
        long sizeBytes,
        string fileName,
        string altText,
        CancellationToken cancellationToken = default)
    {
        if (sizeBytes > _maxFileBytes)
        {
            return Result<MediaFile>.Fail("Dosya boyutu sınırı aşıldı.");
        }

        if (!await IsAllowedResourceFileAsync(stream, cancellationToken))
        {
            return Result<MediaFile>.Fail("Geçersiz dosya türü. İzin verilen: PDF, DOCX, PPTX, XLSX, ZIP.");
        }

        var id = Guid.NewGuid().ToString("N");
        var folder = $"files/{clock.UtcNow:yyyy/MM}";
        var safeName = Path.GetFileName(fileName);
        var relativePath = $"{folder}/{id}_{safeName}";
        stream.Position = 0;
        await fileStorage.SaveAsync(stream, relativePath, cancellationToken);

        var media = new MediaFile
        {
            StoragePath = relativePath,
            OriginalFileName = safeName,
            ContentType = GuessContentType(safeName),
            SizeBytes = sizeBytes,
            AltText = string.IsNullOrWhiteSpace(altText) ? safeName : altText.Trim(),
        };

        db.MediaFiles.Add(media);
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("Create", nameof(MediaFile), media.Id, new { media.OriginalFileName, kind = "resource" }, cancellationToken);
        return Result<MediaFile>.Ok(media);
    }

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

    private static async Task<bool> IsAllowedResourceFileAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[8];
        var read = await stream.ReadAsync(header.AsMemory(0, 8), cancellationToken);
        stream.Position = 0;
        if (read < 4)
        {
            return false;
        }

        // PDF
        if (header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46)
        {
            return true;
        }

        // ZIP (Office Open XML)
        if (header[0] == 0x50 && header[1] == 0x4B)
        {
            return true;
        }

        return false;
    }

    private static string GuessContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".zip" => "application/zip",
            _ => "application/octet-stream",
        };
    }
}