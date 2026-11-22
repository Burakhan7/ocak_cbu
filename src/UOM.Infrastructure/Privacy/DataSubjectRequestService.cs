using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Application.Formatting;
using UOM.Domain.Enums;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Privacy;

public sealed record DataSubjectContactRow(
    int Id,
    string FullName,
    string Email,
    string Subject,
    string ReceivedDisplay);

public sealed record DataSubjectInterestRow(
    int Id,
    string FullName,
    string Email,
    string EventTitle,
    string ReceivedDisplay);

public sealed record DataSubjectSearchResult(
    string Email,
    IReadOnlyList<DataSubjectContactRow> Contacts,
    IReadOnlyList<DataSubjectInterestRow> Interests)
{
    public int Total => Contacts.Count + Interests.Count;
}

public sealed record DataSubjectEraseResult(int ContactMessages, int EventInterests)
{
    public int Total => ContactMessages + EventInterests;
}

public sealed class DataSubjectRequestService(AppDbContext db, IAuditLogger auditLogger)
{
    public async Task<Result<DataSubjectSearchResult>> SearchAsync(string? email, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        if (normalized is null)
        {
            return Result<DataSubjectSearchResult>.Fail("Geçerli bir e-posta adresi girin.");
        }

        var contacts = await db.ContactMessages
            .IgnoreQueryFilters()
            .Where(m => m.Email.ToLower() == normalized)
            .OrderByDescending(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var interests = await db.EventInterests
            .IgnoreQueryFilters()
            .Include(i => i.Event)
            .Where(i => i.Email != null && i.Email.ToLower() == normalized)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var result = new DataSubjectSearchResult(
            normalized,
            contacts.Select(m => new DataSubjectContactRow(
                m.Id,
                m.FullName,
                m.Email,
                SubjectLabel(m.Subject),
                m.CreatedAtUtc.ToLocalDisplay())).ToList(),
            interests.Select(i => new DataSubjectInterestRow(
                i.Id,
                i.FullName,
                i.Email ?? normalized,
                i.Event?.Title ?? "Etkinlik",
                i.CreatedAtUtc.ToLocalDisplay())).ToList());

        return Result<DataSubjectSearchResult>.Ok(result);
    }

    public async Task<Result<DataSubjectEraseResult>> EraseAsync(string? email, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        if (normalized is null)
        {
            return Result<DataSubjectEraseResult>.Fail("Geçerli bir e-posta adresi girin.");
        }

        var contacts = await db.ContactMessages
            .IgnoreQueryFilters()
            .Where(m => m.Email.ToLower() == normalized)
            .ToListAsync(cancellationToken);
        var interests = await db.EventInterests
            .IgnoreQueryFilters()
            .Where(i => i.Email != null && i.Email.ToLower() == normalized)
            .ToListAsync(cancellationToken);

        db.ContactMessages.RemoveRange(contacts);
        db.EventInterests.RemoveRange(interests);

        var erased = new DataSubjectEraseResult(contacts.Count, interests.Count);
        await auditLogger.LogAsync(
            "KvkkDataSubjectErase",
            "DataSubjectRequest",
            null,
            new
            {
                contactMessages = erased.ContactMessages,
                eventInterests = erased.EventInterests,
            },
            cancellationToken);

        return Result<DataSubjectEraseResult>.Ok(erased);
    }

    internal static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmed = email.Trim();
        if (trimmed.Length > 256 || trimmed.Contains(' ') || trimmed.Contains('\t'))
        {
            return null;
        }

        var at = trimmed.IndexOf('@');
        if (at <= 0 || at != trimmed.LastIndexOf('@') || at >= trimmed.Length - 1)
        {
            return null;
        }

        var domain = trimmed[(at + 1)..];
        if (!domain.Contains('.'))
        {
            return null;
        }

        return trimmed.ToLowerInvariant();
    }

    private static string SubjectLabel(ContactSubject subject) => subject switch
    {
        ContactSubject.General => "Genel",
        ContactSubject.Event => "Etkinlik",
        ContactSubject.Volunteering => "Gönüllülük",
        ContactSubject.Press => "Basın",
        ContactSubject.Other => "Diğer",
        _ => "Diğer",
    };
}
