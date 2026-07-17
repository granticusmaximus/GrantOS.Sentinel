using System.Net;
using GrantOS.Sentinel.Application.Services;
using Xunit;

namespace GrantOS.Sentinel.Tests;

public sealed class LocalApiRequestGuardTests
{
    [Fact]
    public void Authorizes_matching_token_from_loopback()
    {
        Assert.True(LocalApiRequestValidator.IsAuthorized(IPAddress.Loopback, "secret", "secret"));
    }

    [Theory]
    [InlineData("203.0.113.4", "secret")]
    [InlineData("127.0.0.1", "wrong")]
    [InlineData("127.0.0.1", "")]
    public void Rejects_remote_or_invalid_requests(string address, string token)
    {
        Assert.False(LocalApiRequestValidator.IsAuthorized(IPAddress.Parse(address), token, "secret"));
    }

    [Fact]
    public void Rejects_request_without_a_resolved_remote_address() =>
        Assert.False(LocalApiRequestValidator.IsAuthorized(null, "secret", "secret"));
}
