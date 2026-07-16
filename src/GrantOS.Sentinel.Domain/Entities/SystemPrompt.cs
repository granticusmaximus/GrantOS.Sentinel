namespace GrantOS.Sentinel.Domain.Entities;

/// <summary>A reusable system prompt that shapes the model's behaviour.</summary>
public class SystemPrompt
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
