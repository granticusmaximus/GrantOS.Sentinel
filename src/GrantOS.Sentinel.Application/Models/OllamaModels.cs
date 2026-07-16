using System.Text.Json.Serialization;

namespace GrantOS.Sentinel.Application.Models;

// These records map 1:1 to Ollama's local HTTP API (http://localhost:11434).
// The services use JsonSerializerDefaults.Web (camelCase, case-insensitive), so
// single-word lowercase fields bind automatically. Ollama's snake_case fields
// (num_ctx, eval_count, ...) are pinned explicitly with [JsonPropertyName].

/// <summary>A single message in the Ollama chat format.</summary>
public sealed record OllamaMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

/// <summary>Inference knobs sent under the request's "options" object.</summary>
public sealed record OllamaOptions
{
    [JsonPropertyName("num_ctx")]
    public int? NumCtx { get; init; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; init; }
}

/// <summary>Body of POST /api/chat.</summary>
public sealed record OllamaChatRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("messages")]
    public required IReadOnlyList<OllamaMessage> Messages { get; init; }

    [JsonPropertyName("stream")]
    public bool Stream { get; init; }

    [JsonPropertyName("options")]
    public OllamaOptions? Options { get; init; }
}

/// <summary>
/// Response from /api/chat. Used for both the single non-streaming object and each
/// newline-delimited chunk when streaming (the shapes are compatible).
/// </summary>
public sealed record OllamaChatResponse
{
    [JsonPropertyName("message")]
    public OllamaMessage? Message { get; init; }

    [JsonPropertyName("done")]
    public bool Done { get; init; }

    [JsonPropertyName("eval_count")]
    public int? EvalCount { get; init; }

    [JsonPropertyName("prompt_eval_count")]
    public int? PromptEvalCount { get; init; }
}

/// <summary>Response from GET /api/tags (locally installed models).</summary>
public sealed record OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public IReadOnlyList<OllamaModelInfo> Models { get; init; } = [];
}

public sealed record OllamaModelInfo
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("details")]
    public OllamaModelDetails? Details { get; init; }
}

public sealed record OllamaModelDetails
{
    [JsonPropertyName("family")]
    public string? Family { get; init; }

    [JsonPropertyName("parameter_size")]
    public string? ParameterSize { get; init; }

    [JsonPropertyName("quantization_level")]
    public string? QuantizationLevel { get; init; }
}
