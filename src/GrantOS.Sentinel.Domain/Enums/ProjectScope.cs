namespace GrantOS.Sentinel.Domain.Enums;

/// <summary>
/// Security boundary for data and tool actions. Kept from day one so that
/// personal and work contexts never blur together as the system grows.
/// </summary>
public enum ProjectScope
{
    Personal = 0,
    Work = 1
}
