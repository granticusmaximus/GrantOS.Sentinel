using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Application.Models;

/// <summary>Safely converts the human-readable scope query value used by Blazor routes.</summary>
public static class ProjectScopeQueryParser
{
    public static ProjectScope ParseOrDefault(string? value, ProjectScope fallback) =>
        Enum.TryParse<ProjectScope>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : fallback;
}
