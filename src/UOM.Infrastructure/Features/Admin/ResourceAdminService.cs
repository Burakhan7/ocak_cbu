using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Domain.Entities;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Admin;

public sealed class ResourceAdminService(
    AppDbContext db,
    ISlugService slugService,
    IClock clock,
    IAuditLogger auditLogger)
{
    public Task<List<Resource>> ListAsync(CancellationToken cancellationToken = default) =>
        db.Resources.Include(r => r.Faculty).OrderByDescending(r => r.UpdatedAtUtc ?? r.CreatedAtUtc).ToListAsync(cancellationToken);

    public Task<Resource?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.Resources.Include(r => r.Faculty).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<Result<Resource>> SaveAsync(Resource model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            return Result<Resource>.Fail("Başlık zorunludur.");
        }

        if (model.Type == ResourceType.File && model.FileId is null)
        {
            return Result<Resource>.Fail("Dosya kaynağı için medya dosyası seçin.");
        }

        if (model.Type == ResourceType.Link && string.IsNullOrWhiteSpace(model.ExternalUrl))
        {
            return Result<Resource>.Fail("Bağlantı kaynağı için URL girin.");
        }

        if (model.Id == 0)
        {
            model.Slug = await slugService.GenerateSlugAsync(
                model.Title,
                s => db.Resources.AnyAsync(r => r.Slug == s, cancellationToken),
                cancellationToken);
            model.Status = PublishStatus.Draft;
            db.Resources.Add(model);
        }
        else
        {
            var existing = await db.Resources.FirstOrDefaultAsync(r => r.Id == model.Id, cancellationToken);
            if (existing is null)
            {
                return Result<Resource>.Fail("Kaynak bulunamadı.");
            }

            existing.Title = model.Title;
            existing.Description = model.Description;
            existing.Type = model.Type;
            existing.FileId = model.Type == ResourceType.File ? model.FileId : null;
            existing.ExternalUrl = model.Type == ResourceType.Link ? model.ExternalUrl : null;
            existing.FacultyId = model.FacultyId;
            existing.CourseCode = model.CourseCode;
            existing.Semester = model.Semester;
            model = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(model.Id == 0 ? "Create" : "Update", nameof(Resource), model.Id, new { model.Title }, cancellationToken);
        return Result<Resource>.Ok(model);
    }

    public Task<Result> SubmitForApprovalAsync(int id, CancellationToken cancellationToken = default) =>
        PublishableContentAdmin.SubmitForApprovalAsync<Resource>(db, auditLogger, id, cancellationToken);

    public Task<Result> PublishAsync(int id, CancellationToken cancellationToken = default) =>
        PublishableContentAdmin.PublishAsync<Resource>(db, auditLogger, clock, id, cancellationToken);
}