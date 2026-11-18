using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using UOM.Application.Abstractions;

namespace UOM.Infrastructure.Security;

public sealed class IpHasher : IIpHasher
{
    private readonly string _salt;

    public IpHasher(IConfiguration configuration)
    {
        _salt = configuration["Site:Environment"] ?? "default";
    }

    public string Hash(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return string.Empty;
        }

        var day = DateTime.UtcNow.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        var payload = Encoding.UTF8.GetBytes($"{ipAddress}:{day}:{_salt}");
        return Convert.ToHexString(SHA256.HashData(payload));
    }
}