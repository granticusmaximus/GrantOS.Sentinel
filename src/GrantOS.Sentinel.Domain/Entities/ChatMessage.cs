using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Domain.Entities;

/// <summary>One message inside a <see cref="Conversation"/>.</summary>
public class ChatMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;

    /// <summary>Output token count reported by Ollama (assistant messages), if known.</summary>
    public int? TokenCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Conversation? Conversation { get; set; }
}
