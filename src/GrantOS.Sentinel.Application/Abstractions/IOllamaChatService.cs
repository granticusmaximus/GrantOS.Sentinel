using GrantOS.Sentinel.Application.Models;

namespace GrantOS.Sentinel.Application.Abstractions;

/// <summary>Talks to the local Ollama server. The only component that knows Ollama's HTTP shape.</summary>
public interface IOllamaChatService
{
    /// <summary>Returns true if the Ollama server responds; never throws.</summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>Lists locally installed models. Returns an empty list if Ollama is unreachable.</summary>
    Task<IReadOnlyList<OllamaModelInfo>> ListModelsAsync(CancellationToken ct = default);

    /// <summary>Sends a chat request and returns the full assistant reply (non-streaming).</summary>
    Task<OllamaChatResponse> ChatAsync(OllamaChatRequest request, CancellationToken ct = default);

    /// <summary>Streams the assistant reply as incremental content deltas.</summary>
    IAsyncEnumerable<string> StreamChatAsync(OllamaChatRequest request, CancellationToken ct = default);
}
