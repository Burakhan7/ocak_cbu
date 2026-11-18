using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using UOM.Application.Abstractions;

namespace UOM.Infrastructure.Identity;

public sealed class HttpContextAuditUserProvider(IHttpContextAccessor httpContextAccessor) : IAuditUserProvider
{
    public string? GetCurrentUserId() =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}