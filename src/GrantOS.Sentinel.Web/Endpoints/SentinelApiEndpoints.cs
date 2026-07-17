using GrantOS.Sentinel.Application.Abstractions;
using GrantOS.Sentinel.Application.Models;
using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Web;

namespace GrantOS.Sentinel.Web.Endpoints;

/// <summary>
/// A small HTTP surface the future VS Code extension (and scripts) can call.
///
/// Phase 1 scope and caveats:
///  - Localhost-only. There is no authentication yet, so this must never be
///    exposed beyond the loopback interface. Kestrel binds to localhost by default.
///  - Antiforgery is disabled on this group because these are machine-to-machine
///    JSON calls, not browser form posts.
///  - /chat is a thin, non-streaming proxy to Ollama and does not persist history.
///    The Blazor UI is the path that saves conversations. This keeps the API contract
///    simple until a real client needs more.
/// </summary>
public static class SentinelApiEndpoints
{
    public static IEndpointRouteBuilder MapSentinelApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sentinel").DisableAntiforgery();

        group.MapGet("/health", async (IOllamaChatService ollama, CancellationToken ct) =>
        {
            var ollamaUp = await ollama.IsAvailableAsync(ct);
            return Results.Ok(new { status = "ok", ollama = ollamaUp });
        });

        group.MapGet("/models", async (IOllamaChatService ollama, CancellationToken ct) =>
        {
            var models = await ollama.ListModelsAsync(ct);
            return Results.Ok(models.Select(m => new
            {
                m.Name,
                m.Size,
                parameterSize = m.Details?.ParameterSize,
                family = m.Details?.Family
            }));
        });

        group.MapPost("/chat", async (ChatApiRequest req, IOllamaChatService ollama, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Model) || string.IsNullOrWhiteSpace(req.Message))
                return Results.BadRequest(new { error = "Both 'model' and 'message' are required." });

            var messages = new List<OllamaMessage>();
            if (!string.IsNullOrWhiteSpace(req.SystemPrompt))
                messages.Add(new OllamaMessage("system", req.SystemPrompt));
            messages.Add(new OllamaMessage("user", req.Message));

            var options = (req.NumCtx is null && req.Temperature is null)
                ? null
                : new OllamaOptions { NumCtx = req.NumCtx, Temperature = req.Temperature };

            var request = new OllamaChatRequest
            {
                Model = req.Model,
                Messages = messages,
                Stream = false,
                Options = options
            };

            try
            {
                var response = await ollama.ChatAsync(request, ct);
                return Results.Ok(new
                {
                    reply = response.Message?.Content ?? "",
                    evalCount = response.EvalCount,
                    promptEvalCount = response.PromptEvalCount
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(title: "Ollama call failed", detail: ex.Message, statusCode: 502);
            }
        });

        group.MapGet("/memory", async (string? search, IMemoryService memory, CancellationToken ct) =>
        {
            var entries = await memory.ListAsync(search, ct);
            return Results.Ok(entries.Select(e => new
            {
                e.Id,
                e.Title,
                e.Content,
                e.Category,
                e.Tags,
                e.Pinned,
                scope = e.Scope.ToString(),
                e.UpdatedAt
            }));
        });

        group.MapPost("/memory", async (MemoryCreateRequest req, IMemoryService memory, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Title))
                return Results.BadRequest(new { error = "'title' is required." });

            var entry = new MemoryEntry
            {
                Title = req.Title,
                Content = req.Content ?? "",
                Category = string.IsNullOrWhiteSpace(req.Category) ? "General" : req.Category,
                Tags = req.Tags ?? "",
                Pinned = req.Pinned
            };
            var created = await memory.CreateAsync(entry, ct);
            return Results.Created($"/api/sentinel/memory/{created.Id}", new { created.Id });
        });

        group.MapPost("/focus", async (string? prompt, ElectronWindowController window) =>
        {
            var focused = await window.FocusAsync(prompt);
            return focused
                ? Results.Ok(new { focused = true, promptSupplied = !string.IsNullOrWhiteSpace(prompt) })
                : Results.Problem(title: "Electron window is not ready", statusCode: 503);
        });

        return app;
    }
}

/// <summary>Request body for POST /api/sentinel/chat.</summary>
public sealed record ChatApiRequest(
    string Model,
    string Message,
    string? SystemPrompt = null,
    double? Temperature = null,
    int? NumCtx = null);

/// <summary>Request body for POST /api/sentinel/memory.</summary>
public sealed record MemoryCreateRequest(
    string Title,
    string? Content = null,
    string? Category = null,
    string? Tags = null,
    bool Pinned = false);
