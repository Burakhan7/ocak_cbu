using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Domain.Entities;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Admin;

public sealed class VenueAdminService(AppDbContext db, IAuditLogger auditLogger)
{
    public Task<List<Venue>> ListAsync(CancellationToken cancellationToken = default) =>
        db.Venues.OrderBy(v => v.Name).ToListAsync(cancellationToken);

    public Task<Venue?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.Venues.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<Result<Venue>> SaveAsync(Venue model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return Result<Venue>.Fail("Mekan adı zorunludur.");
        }

        if (model.Id == 0)
        {
            db.Venues.Add(model);
        }
        else
        {
            var existing = await db.Venues.FirstOrDefaultAsync(v => v.Id == model.Id, cancellationToken);
            if (existing is null)
            {
                return Result<Venue>.Fail("Mekan bulunamadı.");
            }

            existing.Name = model.Name;
            existing.Address = model.Address;
            existing.Latitude = model.Latitude;
            existing.Longitude = model.Longitude;
            existing.CampusId = model.CampusId;
            existing.CoverImageId = model.CoverImageId;
            model = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(model.Id == 0 ? "Create" : "Update", nameof(Venue), model.Id, new { model.Name }, cancellationToken);
        return Result<Venue>.Ok(model);
    }

    public Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        AdminSoftDelete.DeleteAsync<Venue>(db, auditLogger, id, "Mekan bulunamadı.", cancellationToken);
}