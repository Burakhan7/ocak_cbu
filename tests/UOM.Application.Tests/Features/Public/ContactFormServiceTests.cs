using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UOM.Application.Abstractions;
using UOM.Application.Features.Public.Models;
using UOM.Domain.Enums;
using UOM.Infrastructure.Features.Public;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Security;
using UOM.Infrastructure.Time;

namespace UOM.Application.Tests.Features.Public;

public class ContactFormServiceTests
{
    [Fact]
    public async Task Submit_rejects_without_kvkk_consent()
    {
        var service = CreateService();
        var result = await service.SubmitAsync(new ContactFormRequest
        {
            FullName = "Ali",
            Email = "ali@test.com",
            Message = "Test",
            Subject = ContactSubject.General,
            KvkkConsent = false,
        }, "127.0.0.1");

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("KVKK");
    }

    private static ContactFormService CreateService()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var clock = new SystemClock();
        var db = new AppDbContext(options, clock, new NullAuditUserProvider());
        var config = new ConfigurationBuilder().Build();
        return new ContactFormService(db, clock, new IpHasher(config), new NoOpTurnstile(), config);
    }

    private sealed class NoOpTurnstile : ITurnstileVerifier
    {
        public Task<bool> VerifyAsync(string? token, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class NullAuditUserProvider : IAuditUserProvider
    {
        public string? GetCurrentUserId() => null;
    }
}