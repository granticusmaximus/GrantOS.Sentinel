using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace GrantOS.Sentinel.Application.Services;

/// <summary>Pure validation seam for the loopback API's network boundary and process secret.</summary>
public static class LocalApiRequestValidator
{
    public static bool IsAuthorized(IPAddress? remoteAddress, string? suppliedToken, string expectedToken)
    {
        if (remoteAddress is null || !IPAddress.IsLoopback(remoteAddress))
            return false;
        if (string.IsNullOrEmpty(suppliedToken))
            return false;

        var expected = Encoding.UTF8.GetBytes(expectedToken);
        var actual = Encoding.UTF8.GetBytes(suppliedToken);
        return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
