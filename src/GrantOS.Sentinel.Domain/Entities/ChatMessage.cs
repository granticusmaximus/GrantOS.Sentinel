using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Domain.Entities;

/// <summary>One message inside a <see cref="Conversation"/>.</summary>
public class ChatMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;

    /// <summary>Tool name, set only for <see cref="ChatRole.Tool"/> result messages.</summary>
    public string? ToolName { get; set; }

    /// <summary>JSON-serialized arguments the model called <see cref="ToolName"/> with.</summary>
    public string? ToolArguments { get; set; }

    /// <summary>JSON-serialized assistant tool calls, retained for Ollama multi-turn continuity.</summary>
    public string? ToolCallsJson { get; set; }

    /// <summary>Output token count reported by Ollama (assistant messages), if known.</summary>
    public int? TokenCount { get; set; }

    /// <summary>Input token count reported by Ollama, including the assembled conversation context.</summary>
    public int? PromptTokenCount { get; set; }

    /// <summary>Ollama timings are persisted in nanoseconds to preserve the API's original precision.</summary>
    public long? TotalDurationNanoseconds { get; set; }
    public long? LoadDurationNanoseconds { get; set; }
    public long? PromptEvalDurationNanoseconds { get; set; }
    public long? EvalDurationNanoseconds { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Conversation? Conversation { get; set; }
}
