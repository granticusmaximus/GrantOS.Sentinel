using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Domain.Entities;

/// <summary>A persistent note in the Memory Vault (standards, decisions, lessons, facts).</summary>
public class MemoryEntry
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "General";

    /// <summary>Comma-separated tags. Kept simple for Phase 1; a join table can come later.</summary>
    public string Tags { get; set; } = string.Empty;
    public bool Pinned { get; set; }
    public ProjectScope Scope { get; set; } = ProjectScope.Personal;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
