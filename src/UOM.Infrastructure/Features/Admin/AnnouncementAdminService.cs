using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Admin;

public sealed class AnnouncementAdminService(
    AppDbContext db,
    ISlugService slugService,
    IClock clock,
    IAuditLogger auditLogger)
{
    public Task<List<Announcement>> ListAsync(CancellationToken cancellationToken = default) =>
        db.Announcements.OrderByDescending(a => a.IsPinned).ThenBy(a => a.ExpiresAtUtc).ToListAsync(cancellationToken);

    public Task<Announcement?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.Announcements.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<Result<Announcement>> SaveAsync(Announcement model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            return Result<Announcement>.Fail("Başlık zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(model.Body))
        {
            return Result<Announcement>.Fail("Duyuru metni zorunludur.");
        }

        if (model.ExpiresAtUtc <= clock.UtcNow)
        {
            return Result<Announcement>.Fail("Bitiş tarihi gelecekte olmalıdır.");
        }

        if (model.Id == 0)
        {
            model.Slug = await slugService.GenerateSlugAsync(
                model.Title,
                s => db.Announcements.AnyAsync(a => a.Slug == s, cancellationToken),
                cancellationToken);
            model.Status = PublishStatus.Draft;
            db.Announcements.Add(model);
        }
        else
        {
            var existing = await db.Announcements.FirstOrDefaultAsync(a => a.Id == model.Id, cancellationToken);
            if (existing is null)
            {
                return Result<Announcement>.Fail("Duyuru bulunamadı.");
            }

            existing.Title = model.Title;
            existing.Body = model.Body;
            existing.Type = model.Type;
            existing.ExternalUrl = model.ExternalUrl;
            existing.DeadlineUtc = model.DeadlineUtc;
            existing.ExpiresAtUtc = model.ExpiresAtUtc;
            existing.IsPinned = model.IsPinned;
            existing.CoverImageId = model.CoverImageId;
            model = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(model.Id == 0 ? "Create" : "Update", nameof(Announcement), model.Id, new { model.Title }, cancellationToken);
        return Result<Announcement>.Ok(model);
    }

    public Task<Result> SubmitForApprovalAsync(int id, CancellationToken cancellationToken = default) =>
        PublishableContentAdmin.SubmitForApprovalAsync<Announcement>(db, auditLogger, id, cancellationToken);

    public Task<Result> PublishAsync(int id, CancellationToken cancellationToken = default) =>
        PublishableContentAdmin.PublishAsync<Announcement>(db, auditLogger, clock, id, cancellationToken);

    public Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        AdminSoftDelete.DeleteAsync<Announcement>(db, auditLogger, id, "Duyuru bulunamadı.", cancellationToken);
}