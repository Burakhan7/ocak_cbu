using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Admin;

public sealed class GuideArticleAdminService(
    AppDbContext db,
    ISlugService slugService,
    IClock clock,
    IAuditLogger auditLogger)
{
    public Task<List<GuideArticle>> ListAsync(CancellationToken cancellationToken = default) =>
        db.GuideArticles.OrderBy(g => g.Category).ThenBy(g => g.Order).ToListAsync(cancellationToken);

    public Task<GuideArticle?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.GuideArticles.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<Result<GuideArticle>> SaveAsync(GuideArticle model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            return Result<GuideArticle>.Fail("Başlık zorunludur.");
        }

        if (model.Id == 0)
        {
            model.Slug = await slugService.GenerateSlugAsync(
                model.Title,
                s => db.GuideArticles.AnyAsync(g => g.Slug == s, cancellationToken),
                cancellationToken);
            model.Status = PublishStatus.Draft;
            db.GuideArticles.Add(model);
        }
        else
        {
            var existing = await db.GuideArticles.FirstOrDefaultAsync(g => g.Id == model.Id, cancellationToken);
            if (existing is null)
            {
                return Result<GuideArticle>.Fail("Makale bulunamadı.");
            }

            existing.Title = model.Title;
            existing.Summary = model.Summary;
            existing.BodyHtml = model.BodyHtml;
            existing.Category = model.Category;
            existing.Order = model.Order;
            existing.CoverImageId = model.CoverImageId;
            existing.MetaTitle = model.MetaTitle;
            existing.MetaDescription = model.MetaDescription;
            existing.OgImageId = model.OgImageId;
            if (!string.IsNullOrWhiteSpace(model.Slug))
            {
                existing.Slug = model.Slug.Trim();
            }

            model = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(model.Id == 0 ? "Create" : "Update", nameof(GuideArticle), model.Id, new { model.Title }, cancellationToken);
        return Result<GuideArticle>.Ok(model);
    }

    public Task<Result> SubmitForApprovalAsync(int id, CancellationToken cancellationToken = default) =>
        PublishableContentAdmin.SubmitForApprovalAsync<GuideArticle>(db, auditLogger, id, cancellationToken);

    public async Task<Result> PublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await PublishableContentAdmin.PublishAsync<GuideArticle>(db, auditLogger, clock, id, cancellationToken);
        if (!result.Succeeded)
        {
            return result;
        }

        var article = await db.GuideArticles.FirstAsync(g => g.Id == id, cancellationToken);
        article.LastReviewedAtUtc = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}