using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace UOM.Web.Security;

public static class HealthCheckAccess
{
    public const string HeaderName = "X-Health-Key";

    public static bool IsAllowed(HttpContext context, IHostEnvironment environment, IConfiguration configuration)
    {
        if (HasValidKey(context, configuration))
        {
            return true;
        }

        var ip = context.Connection.RemoteIpAddress;
        if (ip is not null && IPAddress.IsLoopback(ip))
        {
            return true;
        }

        return ip is null && (environment.IsDevelopment() || environment.IsEnvironment("Testing"));
    }

    private static bool HasValidKey(HttpContext context, IConfiguration configuration)
    {
        var expected = configuration["Health:AccessKey"];
        if (string.IsNullOrEmpty(expected))
        {
            return false;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var providedValues))
        {
            return false;
        }

        var provided = providedValues.ToString();
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        if (expectedBytes.Length != providedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
