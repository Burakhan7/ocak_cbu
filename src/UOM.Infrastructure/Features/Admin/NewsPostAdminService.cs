using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Admin;

public sealed class NewsPostAdminService(
    AppDbContext db,
    ISlugService slugService,
    IClock clock,
    IAuditLogger auditLogger)
{
    public Task<List<NewsPost>> ListAsync(CancellationToken cancellationToken = default) =>
        db.NewsPosts
            .OrderByDescending(n => n.UpdatedAtUtc ?? n.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<NewsPost?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.NewsPosts.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<Result<NewsPost>> SaveAsync(NewsPost model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            return Result<NewsPost>.Fail("Başlık zorunludur.");
        }

        model.BodyHtml = AdminContentFieldNormalization.NormalizeRequiredHtml(model.BodyHtml);
        var isCreate = model.Id == 0;

        if (model.Id == 0)
        {
            model.Slug = await slugService.GenerateSlugAsync(
                model.Title,
                s => db.NewsPosts.AnyAsync(n => n.Slug == s, cancellationToken),
                cancellationToken);
            model.Status = PublishStatus.Draft;
            db.NewsPosts.Add(model);
        }
        else
        {
            var existing = await db.NewsPosts.FirstOrDefaultAsync(n => n.Id == model.Id, cancellationToken);
            if (existing is null)
            {
                return Result<NewsPost>.Fail("Haber bulunamadı.");
            }

            existing.Title = model.Title;
            existing.Summary = model.Summary;
            existing.BodyHtml = AdminContentFieldNormalization.NormalizeRequiredHtml(model.BodyHtml);
            existing.Scope = model.Scope;
            existing.CategoryId = model.CategoryId;
            existing.IsFeatured = model.IsFeatured;
            existing.IsPinned = model.IsPinned;
            existing.CoverImageId = model.CoverImageId;
            existing.MetaTitle = model.MetaTitle;
            existing.MetaDescription = model.MetaDescription;
            existing.OgImageId = model.OgImageId;
            model = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(isCreate ? "Create" : "Update", nameof(NewsPost), model.Id, new { model.Title, model.Status }, cancellationToken);
        return Result<NewsPost>.Ok(model);
    }

    public async Task<Result> SubmitForApprovalAsync(int id, CancellationToken cancellationToken = default)
    {
        var post = await db.NewsPosts.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (post is null)
        {
            return Result.Fail("Haber bulunamadı.");
        }

        if (post.Status is not (PublishStatus.Draft or PublishStatus.PendingApproval))
        {
            return Result.Fail("Bu haber onaya gönderilemez.");
        }

        post.Status = PublishStatus.PendingApproval;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("SubmitForApproval", nameof(NewsPost), id, null, cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> PublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var post = await db.NewsPosts.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (post is null)
        {
            return Result.Fail("Haber bulunamadı.");
        }

        if (post.Status is not (PublishStatus.Draft or PublishStatus.PendingApproval))
        {
            return Result.Fail("Yalnızca taslak veya onay bekleyen haber yayınlanabilir.");
        }

        post.Status = PublishStatus.Published;
        post.PublishedAtUtc ??= clock.UtcNow;
        post.LastRejectionNote = null;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("Publish", nameof(NewsPost), id, null, cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> RejectAsync(int id, string note, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return Result.Fail("Red notu zorunludur.");
        }

        var post = await db.NewsPosts.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (post is null || post.Status != PublishStatus.PendingApproval)
        {
            return Result.Fail("Onay bekleyen haber bulunamadı.");
        }

        post.Status = PublishStatus.Draft;
        post.LastRejectionNote = note.Trim();
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("Reject", nameof(NewsPost), id, new { note }, cancellationToken);
        return Result.Ok();
    }

    public Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        AdminSoftDelete.DeleteAsync<NewsPost>(db, auditLogger, id, "Haber bulunamadı.", cancellationToken);
}