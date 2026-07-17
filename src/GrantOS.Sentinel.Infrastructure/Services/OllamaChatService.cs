using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using GrantOS.Sentinel.Application.Abstractions;
using GrantOS.Sentinel.Application.Models;
using Microsoft.Extensions.Logging;

namespace GrantOS.Sentinel.Infrastructure.Services;

/// <summary>
/// The only class that knows Ollama's HTTP contract. Uses a typed <see cref="HttpClient"/>
/// whose BaseAddress and timeout are configured in DI from OllamaClientOptions.
/// </summary>
public sealed class OllamaChatService(HttpClient http, ILogger<OllamaChatService> logger) : IOllamaChatService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await http.GetAsync("/api/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ollama availability check failed at {BaseAddress}", http.BaseAddress);
            return false;
        }
    }

    public async Task<IReadOnlyList<OllamaModelInfo>> ListModelsAsync(CancellationToken ct = default)
    {
        try
        {
            var tags = await http.GetFromJsonAsync<OllamaTagsResponse>("/api/tags", Json, ct);
            return tags?.Models ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not list Ollama models");
            return [];
        }
    }

    public async Task<bool> SupportsToolCallingAsync(string model, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.PostAsJsonAsync("/api/show", new OllamaShowRequest { Model = model }, Json, ct);
            if (!response.IsSuccessStatusCode)
                return false;

            var show = await response.Content.ReadFromJsonAsync<OllamaShowResponse>(Json, ct);
            return show?.Capabilities.Contains("tools") ?? false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not determine tool-calling support for {Model}", model);
            return false;
        }
    }

    public async Task<OllamaChatResponse> ChatAsync(OllamaChatRequest request, CancellationToken ct = default)
    {
        var body = request with { Stream = false };
        using var response = await http.PostAsJsonAsync("/api/chat", body, Json, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(Json, ct);
        return result ?? new OllamaChatResponse { Done = true };
    }

    public async IAsyncEnumerable<OllamaStreamEvent> StreamChatAsync(
        OllamaChatRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var body = request with { Stream = true };
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(body, options: Json)
        };

        using var response = await http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        // Ollama's own guidance is to gather tool_calls across every chunk before acting on
        // them, even though observed behavior delivers them atomically in one chunk - accumulate
        // defensively rather than assume.
        List<OllamaToolCall>? toolCalls = null;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(ct);
            if (line is null)
                break;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            OllamaChatResponse? chunk = null;
            try
            {
                chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line, Json);
            }
            catch (JsonException ex)
            {
                // A malformed line should not kill the whole stream.
                logger.LogDebug(ex, "Skipped an unparseable stream chunk");
            }

            if (chunk is null)
                continue;

            if (chunk.Message?.Content is { Length: > 0 } delta)
                yield return new OllamaContentDelta(delta);

            if (chunk.Message?.ToolCalls is { Count: > 0 } calls)
            {
                toolCalls ??= [];
                toolCalls.AddRange(calls);
            }

            if (chunk.Done)
            {
                if (toolCalls is { Count: > 0 })
                    yield return new OllamaToolCallsReady(toolCalls);
                yield return new OllamaGenerationCompleted(
                    chunk.PromptEvalCount,
                    chunk.EvalCount,
                    chunk.TotalDurationNanoseconds,
                    chunk.LoadDurationNanoseconds,
                    chunk.PromptEvalDurationNanoseconds,
                    chunk.EvalDurationNanoseconds);
                yield break;
            }
        }
    }
}
