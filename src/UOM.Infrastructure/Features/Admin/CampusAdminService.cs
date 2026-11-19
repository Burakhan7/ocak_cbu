using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Domain.Entities;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Admin;

public sealed class CampusAdminService(AppDbContext db, IAuditLogger auditLogger)
{
    public Task<List<Campus>> ListCampusesAsync(CancellationToken cancellationToken = default) =>
        db.Campuses.OrderBy(c => c.Order).ThenBy(c => c.Name).ToListAsync(cancellationToken);

    public Task<Campus?> GetCampusAsync(int id, CancellationToken cancellationToken = default) =>
        db.Campuses.Include(c => c.Faculties).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Result<Campus>> SaveCampusAsync(Campus model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return Result<Campus>.Fail("Kampüs adı zorunludur.");
        }

        if (model.Id == 0)
        {
            db.Campuses.Add(model);
        }
        else
        {
            var existing = await db.Campuses.FirstOrDefaultAsync(c => c.Id == model.Id, cancellationToken);
            if (existing is null)
            {
                return Result<Campus>.Fail("Kampüs bulunamadı.");
            }

            existing.Name = model.Name;
            existing.ShortName = model.ShortName;
            existing.Address = model.Address;
            existing.Latitude = model.Latitude;
            existing.Longitude = model.Longitude;
            existing.Description = model.Description;
            existing.Order = model.Order;
            existing.CoverImageId = model.CoverImageId;
            model = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(model.Id == 0 ? "Create" : "Update", nameof(Campus), model.Id, new { model.Name }, cancellationToken);
        return Result<Campus>.Ok(model);
    }

    public Task<List<Faculty>> ListFacultiesAsync(CancellationToken cancellationToken = default) =>
        db.Faculties.Include(f => f.Campus).OrderBy(f => f.Campus.Name).ThenBy(f => f.Order).ToListAsync(cancellationToken);

    public Task<Faculty?> GetFacultyAsync(int id, CancellationToken cancellationToken = default) =>
        db.Faculties.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<Result<Faculty>> SaveFacultyAsync(Faculty model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return Result<Faculty>.Fail("Fakülte adı zorunludur.");
        }

        if (model.CampusId <= 0)
        {
            return Result<Faculty>.Fail("Kampüs seçin.");
        }

        if (model.Id == 0)
        {
            db.Faculties.Add(model);
        }
        else
        {
            var existing = await db.Faculties.FirstOrDefaultAsync(f => f.Id == model.Id, cancellationToken);
            if (existing is null)
            {
                return Result<Faculty>.Fail("Fakülte bulunamadı.");
            }

            existing.Name = model.Name;
            existing.CampusId = model.CampusId;
            existing.Order = model.Order;
            existing.CoverImageId = model.CoverImageId;
            model = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(model.Id == 0 ? "Create" : "Update", nameof(Faculty), model.Id, new { model.Name }, cancellationToken);
        return Result<Faculty>.Ok(model);
    }
}