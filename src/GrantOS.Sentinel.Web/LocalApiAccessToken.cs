using System.Security.Cryptography;

namespace GrantOS.Sentinel.Web;

/// <summary>Per-process bearer secret for the loopback automation API.</summary>
public sealed class LocalApiAccessToken
{
    public const string HeaderName = "X-Sentinel-Token";
    public string Value { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}
