using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Domain.Entities;

/// <summary>A local source workspace whose text files can be indexed for chat retrieval.</summary>
public class ProjectWorkspace
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public ProjectScope Scope { get; set; } = ProjectScope.Work;
    public int IndexedFileCount { get; set; }
    public long IndexedByteCount { get; set; }
    public DateTime? LastIndexedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ProjectDocument> Documents { get; set; } = [];
}
