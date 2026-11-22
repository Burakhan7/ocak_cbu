using System.Security.Cryptography;

namespace UOM.Web.Security;

public static class CspNonce
{
    public const string HttpContextItemKey = "UOM.CspNonce";
    public const string Placeholder = "__UOM_CSP_NONCE__";

    public static string Create() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
}
