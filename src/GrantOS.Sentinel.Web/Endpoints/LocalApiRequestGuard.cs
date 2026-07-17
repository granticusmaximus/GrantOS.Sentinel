using GrantOS.Sentinel.Application.Services;

namespace GrantOS.Sentinel.Web.Endpoints;

public static class LocalApiRequestGuard
{
    public const long MaximumRequestBytes = 1_048_576;

    public static bool IsAuthorized(HttpContext context, string expectedToken)
    {
        var supplied = context.Request.Headers[LocalApiAccessToken.HeaderName].ToString();
        return LocalApiRequestValidator.IsAuthorized(
            context.Connection.RemoteIpAddress, supplied, expectedToken);
    }
}
