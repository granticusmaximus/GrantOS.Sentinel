using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Domain.Entities;

/// <summary>User-authored engineering guidance automatically available to same-scope chats.</summary>
public class ProjectStandard
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string AppliesTo { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public ProjectScope Scope { get; set; } = ProjectScope.Work;
    public int Priority { get; set; } = 50;
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
