using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Domain.Entities;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Slugs;

namespace UOM.Infrastructure.Features.Admin;

public sealed class DistrictAdminService(AppDbContext db, IAuditLogger auditLogger, ISlugService slugService)
{
    public Task<List<District>> ListAsync(CancellationToken cancellationToken = default) =>
        db.Districts.OrderBy(d => d.Order).ThenBy(d => d.Name).ToListAsync(cancellationToken);

    public Task<District?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.Districts.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<Result<District>> SaveAsync(District model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return Result<District>.Fail("İlçe adı zorunludur.");
        }

        if (model.Id == 0)
        {
            model.Slug = await GenerateDistrictSlugAsync(model.Name, excludeId: null, cancellationToken);
            db.Districts.Add(model);
        }
        else
        {
            var existing = await db.Districts.FirstOrDefaultAsync(d => d.Id == model.Id, cancellationToken);
            if (existing is null)
            {
                return Result<District>.Fail("İlçe bulunamadı.");
            }

            if (!string.Equals(existing.Name, model.Name, StringComparison.Ordinal))
            {
                existing.Slug = await GenerateDistrictSlugAsync(model.Name, excludeId: existing.Id, cancellationToken);
            }

            existing.Name = model.Name.Trim();
            existing.PresidentName = model.PresidentName;
            existing.Address = model.Address;
            existing.Phone = model.Phone;
            existing.Latitude = model.Latitude;
            existing.Longitude = model.Longitude;
            existing.IsActive = model.IsActive;
            existing.Order = model.Order;
            model = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(model.Id == 0 ? "Create" : "Update", nameof(District), model.Id, new { model.Name }, cancellationToken);
        return Result<District>.Ok(model);
    }

    private async Task<string> GenerateDistrictSlugAsync(string name, int? excludeId, CancellationToken cancellationToken) =>
        await slugService.GenerateSlugAsync(
            name,
            slug => db.Districts.AnyAsync(
                d => d.Slug == slug && (excludeId == null || d.Id != excludeId),
                cancellationToken),
            cancellationToken);
}

public sealed class BoardMemberAdminService(AppDbContext db, IAuditLogger auditLogger)
{
    public Task<List<BoardMember>> ListAsync(CancellationToken cancellationToken = default) =>
        db.BoardMembers.OrderBy(b => b.Order).ThenBy(b => b.FullName).ToListAsync(cancellationToken);

    public Task<BoardMember?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.BoardMembers.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<Result<BoardMember>> SaveAsync(BoardMember model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.FullName))
        {
            return Result<BoardMember>.Fail("Ad soyad zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            return Result<BoardMember>.Fail("Unvan zorunludur.");
        }

        if (!model.ConsentOnFile)
        {
            model.PhotoImageId = null;
        }

        if (model.Id == 0)
        {
            db.BoardMembers.Add(model);
        }
        else
        {
            var existing = await db.BoardMembers.FirstOrDefaultAsync(b => b.Id == model.Id, cancellationToken);
            if (existing is null)
            {
                return Result<BoardMember>.Fail("Kayıt bulunamadı.");
            }

            existing.FullName = model.FullName.Trim();
            existing.Title = model.Title.Trim();
            existing.PhotoImageId = model.PhotoImageId;
            existing.Order = model.Order;
            existing.IsActive = model.IsActive;
            existing.ConsentOnFile = model.ConsentOnFile;
            model = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(model.Id == 0 ? "Create" : "Update", nameof(BoardMember), model.Id, new { model.FullName }, cancellationToken);
        return Result<BoardMember>.Ok(model);
    }
}