using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UOM.Application.Abstractions;
using UOM.Application.Common;
using UOM.Application.Features.Public;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Entities;
using UOM.Infrastructure.Persistence;

namespace UOM.Infrastructure.Features.Public;

public sealed class ContactFormService : IContactFormService
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly IIpHasher _ipHasher;
    private readonly ITurnstileVerifier _turnstileVerifier;
    private readonly IConfiguration _configuration;

    public ContactFormService(
        AppDbContext db,
        IClock clock,
        IIpHasher ipHasher,
        ITurnstileVerifier turnstileVerifier,
        IConfiguration configuration)
    {
        _db = db;
        _clock = clock;
        _ipHasher = ipHasher;
        _turnstileVerifier = turnstileVerifier;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<FacultyOptionDto>> GetFacultyOptionsAsync(CancellationToken cancellationToken = default) =>
        await _db.Faculties
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .Select(f => new FacultyOptionDto(f.Id, f.Name))
            .ToListAsync(cancellationToken);

    public async Task<Result> SubmitAsync(ContactFormRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            return Result.Fail("Spam");
        }

        if (!request.KvkkConsent)
        {
            return Result.Fail("KVKK onayı zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(request.FullName)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Message))
        {
            return Result.Fail("Zorunlu alanları doldurun.");
        }

        if (!await _turnstileVerifier.VerifyAsync(request.TurnstileToken, cancellationToken))
        {
            return Result.Fail("Bot doğrulaması başarısız.");
        }

        if (request.IsStudentAtCbu && request.FacultyId is { } facultyId)
        {
            var facultyExists = await _db.Faculties.AnyAsync(f => f.Id == facultyId, cancellationToken);
            if (!facultyExists)
            {
                return Result.Fail("Geçerli bir fakülte seçin.");
            }
        }

        var retentionDays = _configuration.GetValue("Kvkk:ContactRetentionDays", 180);
        var entity = new ContactMessage
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Subject = request.Subject,
            Message = request.Message.Trim(),
            IsStudentAtCbu = request.IsStudentAtCbu,
            FacultyId = request.IsStudentAtCbu ? request.FacultyId : null,
            KvkkConsentAtUtc = _clock.UtcNow,
            IpHash = _ipHasher.Hash(ipAddress),
            DeleteAfterUtc = _clock.UtcNow.AddDays(retentionDays),
        };

        _db.ContactMessages.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}