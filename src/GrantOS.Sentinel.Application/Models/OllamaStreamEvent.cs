namespace GrantOS.Sentinel.Application.Models;

/// <summary>One item yielded by <see cref="Abstractions.IOllamaChatService.StreamChatAsync"/>.</summary>
public abstract record OllamaStreamEvent;

/// <summary>An incremental piece of assistant reply text.</summary>
public sealed record OllamaContentDelta(string Text) : OllamaStreamEvent;

/// <summary>The model finished its turn by requesting one or more tool calls instead of (or after) text.</summary>
public sealed record OllamaToolCallsReady(IReadOnlyList<OllamaToolCall> ToolCalls) : OllamaStreamEvent;

/// <summary>Token usage and timing values reported by Ollama in the final stream chunk.</summary>
public sealed record OllamaGenerationCompleted(
    int? PromptTokenCount,
    int? ResponseTokenCount,
    long? TotalDurationNanoseconds,
    long? LoadDurationNanoseconds,
    long? PromptEvalDurationNanoseconds,
    long? EvalDurationNanoseconds) : OllamaStreamEvent;

/// <summary>Generation metadata persisted with an assistant message.</summary>
public sealed record MessageGenerationMetrics(
    int? PromptTokenCount,
    int? ResponseTokenCount,
    long? TotalDurationNanoseconds,
    long? LoadDurationNanoseconds,
    long? PromptEvalDurationNanoseconds,
    long? EvalDurationNanoseconds);
