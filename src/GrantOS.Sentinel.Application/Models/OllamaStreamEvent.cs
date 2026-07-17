namespace GrantOS.Sentinel.Application.Models;

/// <summary>One item yielded by <see cref="Abstractions.IOllamaChatService.StreamChatAsync"/>.</summary>
public abstract record OllamaStreamEvent;

/// <summary>An incremental piece of assistant reply text.</summary>
public sealed record OllamaContentDelta(string Text) : OllamaStreamEvent;

/// <summary>The model finished its turn by requesting one or more tool calls instead of (or after) text.</summary>
public sealed record OllamaToolCallsReady(IReadOnlyList<OllamaToolCall> ToolCalls) : OllamaStreamEvent;
