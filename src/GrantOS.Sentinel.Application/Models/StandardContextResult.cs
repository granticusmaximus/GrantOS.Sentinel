using GrantOS.Sentinel.Domain.Entities;

namespace GrantOS.Sentinel.Application.Models;

public sealed record StandardContextResult(
    IReadOnlyList<ProjectStandard> Standards,
    string PromptText)
{
    public static StandardContextResult Empty { get; } = new([], string.Empty);
}
