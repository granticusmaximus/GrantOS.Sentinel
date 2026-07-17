using System.Text.Json;
using GrantOS.Sentinel.Application.Options;
using GrantOS.Sentinel.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace GrantOS.Sentinel.Tests;

public sealed class BrowserControlToolTests
{
    [Fact]
    public void Validation_rejects_non_http_navigation_and_incomplete_actions()
    {
        var tool = CreateTool();

        Assert.False(tool.ValidateInvocation(Args(new { action = "navigate", url = "file:///etc/passwd" })).IsValid);
        Assert.False(tool.ValidateInvocation(Args(new { action = "click" })).IsValid);
        Assert.False(tool.ValidateInvocation(Args(new { action = "fill", selector = "#name" })).IsValid);
        Assert.True(tool.ValidateInvocation(Args(new { action = "navigate", url = "https://example.com" })).IsValid);
    }

    [Fact]
    public async Task Read_before_navigation_launches_browser_and_returns_clear_state()
    {
        await using var tool = CreateTool();

        var result = await tool.ExecuteAsync(Args(new { action = "read_page_text" }));

        Assert.False(result.Success);
        Assert.Contains("not navigated", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    private static BrowserControlTool CreateTool() => new(Options.Create(new AgentOptions
    {
        BrowserHeadless = true,
        BrowserTimeoutSeconds = 10
    }));

    private static JsonElement Args<T>(T value) => JsonSerializer.SerializeToElement(value);
}
