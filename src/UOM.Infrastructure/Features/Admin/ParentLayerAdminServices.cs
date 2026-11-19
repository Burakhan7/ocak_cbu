using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Domain.Entities;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Admin;

public sealed class ParentNewsLinkAdminService(AppDbContext db, IAuditLogger auditLogger)
{
    public Task<List<ParentNewsLink>> ListAsync(CancellationToken cancellationToken = default) =>
        db.ParentNewsLinks.OrderBy(n => n.Order).ThenByDescending(n => n.PublishedDate).ToListAsync(cancellationToken);

    public Task<ParentNewsLink?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.ParentNewsLinks.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<Result<ParentNewsLink>> SaveAsync(ParentNewsLink model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            return Result<ParentNewsLink>.Fail("Başlık zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(model.ExternalUrl))
        {
            return Result<ParentNewsLink>.Fail("Dış bağlantı zorunludur.");
        }

        if (model.Id == 0)
        {
            db.ParentNewsLinks.Add(model);
        }
        else
        {
            var existing = await db.ParentNewsLinks.FirstOrDefaultAsync(n => n.Id == model.Id, cancellationToken);
            if (existing is null)
            {
                return Result<ParentNewsLink>.Fail("Kayıt bulunamadı.");
            }

            Copy(existing, model);
            model = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(model.Id == 0 ? "Create" : "Update", nameof(ParentNewsLink), model.Id, new { model.Title }, cancellationToken);
        return Result<ParentNewsLink>.Ok(model);
    }

    public Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        AdminSoftDelete.DeleteAsync<ParentNewsLink>(db, auditLogger, id, "Kayıt bulunamadı.", cancellationToken);

    private static void Copy(ParentNewsLink existing, ParentNewsLink model)
    {
        existing.Title = model.Title.Trim();
        existing.Summary = model.Summary;
        existing.ImageUrl = model.ImageUrl;
        existing.ImageId = model.ImageId;
        existing.ExternalUrl = model.ExternalUrl.Trim();
        existing.PublishedDate = model.PublishedDate;
        existing.Order = model.Order;
        existing.IsActive = model.IsActive;
    }
}

public sealed class ParentProjectAdminService(AppDbContext db, IAuditLogger auditLogger)
{
    public Task<List<ParentProject>> ListAsync(CancellationToken cancellationToken = default) =>
        db.ParentProjects.OrderBy(p => p.Order).ThenBy(p => p.Name).ToListAsync(cancellationToken);

    public Task<ParentProject?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.ParentProjects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Result<ParentProject>> SaveAsync(ParentProject model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return Result<ParentProject>.Fail("Proje adı zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(model.ExternalUrl))
        {
            return Result<ParentProject>.Fail("Dış bağlantı zorunludur.");
        }

        if (model.Id == 0)
        {
            db.ParentProjects.Add(model);
        }
        else
        {
            var existing = await db.ParentProjects.FirstOrDefaultAsync(p => p.Id == model.Id, cancellationToken);
            if (existing is null)
            {
                return Result<ParentProject>.Fail("Kayıt bulunamadı.");
            }

            existing.Name = model.Name.Trim();
            existing.ShortDescription = model.ShortDescription;
            existing.LogoImageId = model.LogoImageId;
            existing.ExternalUrl = model.ExternalUrl.Trim();
            existing.Order = model.Order;
            existing.IsActive = model.IsActive;
            model = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(model.Id == 0 ? "Create" : "Update", nameof(ParentProject), model.Id, new { model.Name }, cancellationToken);
        return Result<ParentProject>.Ok(model);
    }

    public Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        AdminSoftDelete.DeleteAsync<ParentProject>(db, auditLogger, id, "Kayıt bulunamadı.", cancellationToken);
}

public sealed class MagazineIssueAdminService(AppDbContext db, IAuditLogger auditLogger)
{
    public Task<List<MagazineIssue>> ListAsync(CancellationToken cancellationToken = default) =>
        db.MagazineIssues.OrderByDescending(m => m.IssueDate).ToListAsync(cancellationToken);

    public Task<MagazineIssue?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.MagazineIssues.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<Result<MagazineIssue>> SaveAsync(MagazineIssue model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            return Result<MagazineIssue>.Fail("Başlık zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(model.ExternalUrl))
        {
            return Result<MagazineIssue>.Fail("Dış bağlantı zorunludur.");
        }

        if (model.Id == 0)
        {
            db.MagazineIssues.Add(model);
        }
        else
        {
            var existing = await db.MagazineIssues.FirstOrDefaultAsync(m => m.Id == model.Id, cancellationToken);
            if (existing is null)
            {
                return Result<MagazineIssue>.Fail("Kayıt bulunamadı.");
            }

            existing.Title = model.Title.Trim();
            existing.CoverImageId = model.CoverImageId;
            existing.ExternalUrl = model.ExternalUrl.Trim();
            existing.IssueDate = model.IssueDate;
            existing.IsActive = model.IsActive;
            model = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(model.Id == 0 ? "Create" : "Update", nameof(MagazineIssue), model.Id, new { model.Title }, cancellationToken);
        return Result<MagazineIssue>.Ok(model);
    }

    public Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        AdminSoftDelete.DeleteAsync<MagazineIssue>(db, auditLogger, id, "Kayıt bulunamadı.", cancellationToken);
}

public sealed class VideoAdminService(AppDbContext db, IAuditLogger auditLogger)
{
    public Task<List<Video>> ListAsync(CancellationToken cancellationToken = default) =>
        db.Videos.OrderBy(v => v.Order).ThenByDescending(v => v.PublishedDate).ToListAsync(cancellationToken);

    public Task<Video?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.Videos.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<Result<Video>> SaveAsync(Video model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            return Result<Video>.Fail("Başlık zorunludur.");
        }

        var videoId = NormalizeYouTubeId(model.YouTubeVideoId);
        if (string.IsNullOrWhiteSpace(videoId))
        {
            return Result<Video>.Fail("YouTube video kimliği zorunludur.");
        }

        model.YouTubeVideoId = videoId;

        if (model.Id == 0)
        {
            db.Videos.Add(model);
        }
        else
        {
            var existing = await db.Videos.FirstOrDefaultAsync(v => v.Id == model.Id, cancellationToken);
            if (existing is null)
            {
                return Result<Video>.Fail("Kayıt bulunamadı.");
            }

            existing.Title = model.Title.Trim();
            existing.YouTubeVideoId = videoId;
            existing.Source = model.Source;
            existing.PublishedDate = model.PublishedDate;
            existing.IsFeatured = model.IsFeatured;
            existing.Order = model.Order;
            model = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(model.Id == 0 ? "Create" : "Update", nameof(Video), model.Id, new { model.Title }, cancellationToken);
        return Result<Video>.Ok(model);
    }

    public Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        AdminSoftDelete.DeleteAsync<Video>(db, auditLogger, id, "Kayıt bulunamadı.", cancellationToken);

    public static string? NormalizeYouTubeId(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length == 11 && !trimmed.Contains('/'))
        {
            return trimmed;
        }

        try
        {
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                if (uri.Host.Contains("youtu", StringComparison.OrdinalIgnoreCase))
                {
                    if (uri.AbsolutePath.StartsWith("/embed/", StringComparison.OrdinalIgnoreCase))
                    {
                        return uri.AbsolutePath["/embed/".Length..].Split('?')[0];
                    }

                    if (uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase)
                        && uri.AbsolutePath.Length > 1)
                    {
                        return uri.AbsolutePath.TrimStart('/').Split('?')[0];
                    }

                    if (!string.IsNullOrEmpty(uri.Query))
                    {
                        foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
                        {
                            var kv = part.Split('=', 2);
                            if (kv.Length == 2 && kv[0] == "v" && !string.IsNullOrWhiteSpace(kv[1]))
                            {
                                return Uri.UnescapeDataString(kv[1]);
                            }
                        }
                    }

                    if (uri.AbsolutePath.StartsWith("/shorts/", StringComparison.OrdinalIgnoreCase))
                    {
                        return uri.AbsolutePath["/shorts/".Length..].Split('?')[0];
                    }
                }
            }
        }
        catch
        {
            // fall through
        }

        return trimmed;
    }
}