using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Application.Models;

public enum KnowledgeSourceKind
{
    All = 0,
    Memory = 1,
    Project = 2,
    Standard = 3
}

public sealed record KnowledgeSearchRequest(
    string? Query,
    ProjectScope Scope,
    KnowledgeSourceKind Source = KnowledgeSourceKind.All,
    int MaxResults = 100);

public sealed record KnowledgeItem(
    KnowledgeSourceKind Source,
    int SourceId,
    string Title,
    string SourceName,
    string Location,
    string ContentPreview,
    ProjectScope Scope,
    DateTime UpdatedAt,
    bool Pinned,
    bool Active,
    int Score);

public sealed record KnowledgeSearchResult(
    IReadOnlyList<KnowledgeItem> Items,
    int TotalMatches,
    int MemoryMatches,
    int ProjectMatches,
    int StandardMatches);
