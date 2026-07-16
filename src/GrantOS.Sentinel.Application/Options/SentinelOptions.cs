using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Application.Options;

/// <summary>General application settings, bound from the "Sentinel" config section.</summary>
public sealed class SentinelOptions
{
    public const string SectionName = "Sentinel";

    /// <summary>Default scope applied to new conversations, memory, and audit rows.</summary>
    public ProjectScope DefaultScope { get; set; } = ProjectScope.Personal;
}
