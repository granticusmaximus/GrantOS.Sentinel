namespace GrantOS.Sentinel.Domain.Entities;

/// <summary>A single chat thread with the local model.</summary>
public class Conversation
{
    public int Id { get; set; }
    public string Title { get; set; } = "New conversation";
    public string ModelName { get; set; } = string.Empty;
    public int? SystemPromptId { get; set; }
    public ProjectScope Scope { get; set; } = ProjectScope.Personal;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public SystemPrompt? SystemPrompt { get; set; }
    public List<ChatMessage> Messages { get; set; } = [];
}
