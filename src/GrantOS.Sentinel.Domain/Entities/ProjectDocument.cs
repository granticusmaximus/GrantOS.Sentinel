namespace GrantOS.Sentinel.Domain.Entities;

/// <summary>One indexed text file belonging to a <see cref="ProjectWorkspace"/>.</summary>
public class ProjectDocument
{
    public int Id { get; set; }
    public int ProjectWorkspaceId { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime LastWriteTimeUtc { get; set; }
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;

    public ProjectWorkspace? ProjectWorkspace { get; set; }
}
