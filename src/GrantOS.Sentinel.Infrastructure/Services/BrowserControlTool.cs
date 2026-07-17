using System.Text.Json;
using GrantOS.Sentinel.Application.Abstractions;
using GrantOS.Sentinel.Application.Options;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace GrantOS.Sentinel.Infrastructure.Services;

/// <summary>
/// Controls one persistent, visible Chromium page for the lifetime of a Blazor circuit.
/// Browser startup is lazy and every action is still gated by the chat approval flow.
/// </summary>
public sealed class BrowserControlTool(IOptions<AgentOptions> options) : IAgentTool, IAsyncDisposable
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>("""
        {
          "type": "object",
          "required": ["action"],
          "properties": {
            "action": {
              "type": "string",
              "enum": ["navigate", "read_page_text", "click", "fill"],
              "description": "Browser action to perform."
            },
            "url": {
              "type": "string",
              "description": "HTTP or HTTPS URL for navigate."
            },
            "selector": {
              "type": "string",
              "description": "CSS selector for click or fill."
            },
            "value": {
              "type": "string",
              "description": "Text to enter for fill."
            }
          }
        }
        """);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

    public string Name => "control_browser";
    public string Description =>
        "Controls a visible Chromium window. Can navigate to HTTP/HTTPS URLs, read page text, click an element, or fill a field.";
    public JsonElement ParametersSchema => Schema;

    public string DescribeInvocation(JsonElement arguments)
    {
        var action = GetString(arguments, "action");
        return action switch
        {
            "navigate" => $"Open URL in the controlled browser: `{GetString(arguments, "url")}`",
            "read_page_text" => "Read the visible browser page's text.",
            "click" => $"Click browser element matching: `{GetString(arguments, "selector")}`",
            "fill" => $"Fill browser element `{GetString(arguments, "selector")}` with `{GetString(arguments, "value")}`.",
            _ => "Perform an unknown browser action."
        };
    }

    public AgentToolValidationResult ValidateInvocation(JsonElement arguments)
    {
        var action = GetString(arguments, "action");
        return action switch
        {
            "navigate" => ValidateUrl(GetString(arguments, "url")),
            "read_page_text" => AgentToolValidationResult.Valid,
            "click" => RequireValue(arguments, "selector", "A CSS selector is required for click."),
            "fill" => ValidateFill(arguments),
            _ => AgentToolValidationResult.Invalid("Action must be navigate, read_page_text, click, or fill.")
        };
    }

    public async Task<AgentToolResult> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var validation = ValidateInvocation(arguments);
        if (!validation.IsValid)
            return new AgentToolResult(false, validation.Error!);

        await _gate.WaitAsync(ct);
        try
        {
            var page = await GetPageAsync(ct);
            var timeout = Math.Max(1, options.Value.BrowserTimeoutSeconds) * 1000f;
            page.SetDefaultTimeout(timeout);
            page.SetDefaultNavigationTimeout(timeout);

            return GetString(arguments, "action") switch
            {
                "navigate" => await NavigateAsync(page, GetString(arguments, "url")!, ct),
                "read_page_text" => await ReadPageTextAsync(page),
                "click" => await ClickAsync(page, GetString(arguments, "selector")!),
                "fill" => await FillAsync(page, GetString(arguments, "selector")!, GetString(arguments, "value")!),
                _ => new AgentToolResult(false, "Unknown browser action.")
            };
        }
        catch (PlaywrightException ex)
        {
            return new AgentToolResult(false, BrowserError(ex));
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            return new AgentToolResult(false, BrowserError(ex));
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IPage> GetPageAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (_page is { IsClosed: false })
            return _page;

        _playwright ??= await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = options.Value.BrowserHeadless });
        _context = await _browser.NewContextAsync();
        _page = await _context.NewPageAsync();
        return _page;
    }

    private static async Task<AgentToolResult> NavigateAsync(IPage page, string url, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var response = await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        var title = await page.TitleAsync();
        return new AgentToolResult(response?.Ok ?? true, $"Opened '{title}' at {page.Url} (HTTP {response?.Status.ToString() ?? "unknown"}).");
    }

    private async Task<AgentToolResult> ReadPageTextAsync(IPage page)
    {
        if (page.Url == "about:blank")
            return new AgentToolResult(false, "The controlled browser has not navigated to a page yet.");

        var text = await page.Locator("body").InnerTextAsync();
        var limit = Math.Max(1, options.Value.MaxBrowserTextCharacters);
        if (text.Length > limit)
            text = text[..limit] + $"\n… page text truncated at {limit:N0} characters";
        return new AgentToolResult(true, $"Page: {page.Url}\n\n{text}");
    }

    private static async Task<AgentToolResult> ClickAsync(IPage page, string selector)
    {
        if (page.Url == "about:blank")
            return new AgentToolResult(false, "The controlled browser has not navigated to a page yet.");
        await page.Locator(selector).First.ClickAsync();
        return new AgentToolResult(true, $"Clicked '{selector}'. Current page: {page.Url}");
    }

    private static async Task<AgentToolResult> FillAsync(IPage page, string selector, string value)
    {
        if (page.Url == "about:blank")
            return new AgentToolResult(false, "The controlled browser has not navigated to a page yet.");
        await page.Locator(selector).First.FillAsync(value);
        return new AgentToolResult(true, $"Filled '{selector}' with {value.Length:N0} characters.");
    }

    private static AgentToolValidationResult ValidateUrl(string? rawUrl)
    {
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return AgentToolValidationResult.Invalid("Navigate requires an absolute HTTP or HTTPS URL.");
        return AgentToolValidationResult.Valid;
    }

    private static AgentToolValidationResult ValidateFill(JsonElement arguments)
    {
        var selector = RequireValue(arguments, "selector", "A CSS selector is required for fill.");
        if (!selector.IsValid) return selector;
        return arguments.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.String
            ? AgentToolValidationResult.Valid
            : AgentToolValidationResult.Invalid("A string value is required for fill.");
    }

    private static AgentToolValidationResult RequireValue(JsonElement arguments, string name, string error) =>
        string.IsNullOrWhiteSpace(GetString(arguments, name))
            ? AgentToolValidationResult.Invalid(error)
            : AgentToolValidationResult.Valid;

    private static string? GetString(JsonElement arguments, string name) =>
        arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string BrowserError(Exception ex) =>
        ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase)
            ? "Chromium is not installed for this Playwright version. Run the documented playwright.ps1 install chromium command, then retry."
            : $"Browser action failed: {ex.Message}";

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_context is not null) await _context.CloseAsync();
            if (_browser is not null) await _browser.CloseAsync();
            _playwright?.Dispose();
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }
}
