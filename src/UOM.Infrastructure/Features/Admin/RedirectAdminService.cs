using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Domain.Entities;
using UOM.Infrastructure.Features.Public;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Admin;

public sealed class RedirectAdminService(AppDbContext db, IAuditLogger auditLogger, IMemoryCache cache)
{
    public Task<List<Redirect>> ListAsync(CancellationToken cancellationToken = default) =>
        db.Redirects.OrderByDescending(r => r.UpdatedAtUtc ?? r.CreatedAtUtc).ToListAsync(cancellationToken);

    public Task<Redirect?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.Redirects.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<Result<Redirect>> SaveAsync(Redirect model, CancellationToken cancellationToken = default)
    {
        var from = RedirectPath.Normalize(model.FromPath);
        var to = RedirectPath.Normalize(model.ToPath);
        if (string.IsNullOrWhiteSpace(model.FromPath) || from == "/")
        {
            return Result<Redirect>.Fail("Kaynak adres zorunludur.");
        }

        if (!RedirectPath.IsLocalPath(from) || !RedirectPath.IsLocalPath(to))
        {
            return Result<Redirect>.Fail("Adresler site içi olmalı ve / ile başlamalıdır.");
        }

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            return Result<Redirect>.Fail("Kaynak ve hedef aynı olamaz.");
        }

        if (model.StatusCode is not (301 or 302))
        {
            return Result<Redirect>.Fail("Durum kodu 301 veya 302 olmalıdır.");
        }

        var duplicate = await db.Redirects.AnyAsync(r => r.FromPath == from && r.Id != model.Id, cancellationToken);
        if (duplicate)
        {
            return Result<Redirect>.Fail("Bu kaynak adres için yönlendirme zaten var.");
        }

        var isCreate = model.Id == 0;
        if (isCreate)
        {
            model.FromPath = from;
            model.ToPath = to;
            db.Redirects.Add(model);
        }
        else
        {
            var existing = await db.Redirects.FirstOrDefaultAsync(r => r.Id == model.Id, cancellationToken);
            if (existing is null)
            {
                return Result<Redirect>.Fail("Yönlendirme bulunamadı.");
            }

            existing.FromPath = from;
            existing.ToPath = to;
            existing.StatusCode = model.StatusCode;
            model = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        cache.Remove(PublicCacheKeys.Redirects);
        await auditLogger.LogAsync(isCreate ? "Create" : "Update", nameof(Redirect), model.Id, new { model.FromPath, model.ToPath }, cancellationToken);
        return Result<Redirect>.Ok(model);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await AdminSoftDelete.DeleteAsync<Redirect>(db, auditLogger, id, "Yönlendirme bulunamadı.", cancellationToken);
        if (result.Succeeded)
        {
            cache.Remove(PublicCacheKeys.Redirects);
        }

        return result;
    }
}
