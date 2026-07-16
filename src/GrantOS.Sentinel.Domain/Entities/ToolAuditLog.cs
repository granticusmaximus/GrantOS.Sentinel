using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Domain.Entities;

/// <summary>
/// Immutable record of a tool action. Every side-effecting or model-invoking
/// action writes one of these so the system always has an audit trail.
/// </summary>
public class ToolAuditLog
{
    public int Id { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;

    /// <summary>Human-readable summary of inputs. Never store secrets here.</summary>
    public string? Parameters { get; set; }
    public string? Result { get; set; }
    public bool Success { get; set; }
    public ProjectScope Scope { get; set; } = ProjectScope.Personal;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
