using Microsoft.AspNetCore.DataProtection;
using UOM.Application.Abstractions;

namespace UOM.Infrastructure.Security;

public sealed class PreviewTokenService(IDataProtectionProvider dataProtectionProvider) : IPreviewTokenService
{
    private const string Purpose = "UOM.Preview";

    public string CreateToken(string contentType, int id, TimeSpan lifetime)
    {
        var protector = dataProtectionProvider.CreateProtector(Purpose);
        var payload = $"{contentType}|{id}|{DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds()}";
        return protector.Protect(payload);
    }

    public bool TryValidate(string contentType, int id, string token)
    {
        try
        {
            var protector = dataProtectionProvider.CreateProtector(Purpose);
            var payload = protector.Unprotect(token);
            var parts = payload.Split('|');
            if (parts.Length != 3)
            {
                return false;
            }

            if (!string.Equals(parts[0], contentType, StringComparison.Ordinal) || !int.TryParse(parts[1], out var tokenId) || tokenId != id)
            {
                return false;
            }

            if (!long.TryParse(parts[2], out var expUnix))
            {
                return false;
            }

            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() <= expUnix;
        }
        catch
        {
            return false;
        }
    }
}