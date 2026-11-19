using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Admin;

public sealed class EventAdminService(
    AppDbContext db,
    ISlugService slugService,
    IClock clock,
    IAuditLogger auditLogger)
{
    public Task<List<Event>> ListAsync(CancellationToken cancellationToken = default) =>
        db.Events.OrderByDescending(e => e.StartsAtUtc).ToListAsync(cancellationToken);

    public Task<Event?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<Result<Event>> SaveAsync(Event model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            return Result<Event>.Fail("Başlık zorunludur.");
        }

        model.BodyHtml = AdminContentFieldNormalization.NormalizeRequiredHtml(model.BodyHtml);
        var isCreate = model.Id == 0;

        if (model.Id == 0)
        {
            model.Slug = await slugService.GenerateSlugAsync(
                model.Title,
                s => db.Events.AnyAsync(e => e.Slug == s, cancellationToken),
                cancellationToken);
            model.Status = PublishStatus.Draft;
            db.Events.Add(model);
        }
        else
        {
            var existing = await db.Events.FirstOrDefaultAsync(e => e.Id == model.Id, cancellationToken);
            if (existing is null)
            {
                return Result<Event>.Fail("Etkinlik bulunamadı.");
            }

            existing.Title = model.Title;
            existing.Summary = model.Summary;
            existing.BodyHtml = AdminContentFieldNormalization.NormalizeRequiredHtml(model.BodyHtml);
            existing.StartsAtUtc = model.StartsAtUtc;
            existing.EndsAtUtc = model.EndsAtUtc;
            existing.VenueId = model.VenueId;
            existing.Audience = model.Audience;
            existing.Type = model.Type;
            existing.CallToActionType = model.CallToActionType;
            existing.CallToActionUrl = model.CallToActionUrl;
            existing.Capacity = model.Capacity;
            existing.CoverImageId = model.CoverImageId;
            existing.MetaTitle = model.MetaTitle;
            existing.MetaDescription = model.MetaDescription;
            existing.OgImageId = model.OgImageId;
            model = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(isCreate ? "Create" : "Update", nameof(Event), model.Id, new { model.Title, model.Status }, cancellationToken);
        return Result<Event>.Ok(model);
    }

    public async Task<Result> SubmitForApprovalAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail("Etkinlik bulunamadı.");
        }

        entity.Status = PublishStatus.PendingApproval;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("SubmitForApproval", nameof(Event), id, null, cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> PublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail("Etkinlik bulunamadı.");
        }

        if (entity.Status is not (PublishStatus.Draft or PublishStatus.PendingApproval))
        {
            return Result.Fail("Yalnızca taslak veya onay bekleyen etkinlik yayınlanabilir.");
        }

        entity.Status = PublishStatus.Published;
        entity.PublishedAtUtc ??= clock.UtcNow;
        entity.LastRejectionNote = null;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("Publish", nameof(Event), id, null, cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> RejectAsync(int id, string note, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return Result.Fail("Red notu zorunludur.");
        }

        var entity = await db.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null || entity.Status != PublishStatus.PendingApproval)
        {
            return Result.Fail("Onay bekleyen etkinlik bulunamadı.");
        }

        entity.Status = PublishStatus.Draft;
        entity.LastRejectionNote = note.Trim();
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("Reject", nameof(Event), id, new { note }, cancellationToken);
        return Result.Ok();
    }

    public Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        AdminSoftDelete.DeleteAsync<Event>(db, auditLogger, id, "Etkinlik bulunamadı.", cancellationToken);
}