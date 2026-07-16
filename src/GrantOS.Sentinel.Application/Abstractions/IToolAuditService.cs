using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Application.Abstractions;

public interface IToolAuditService
{
    Task LogAsync(string toolName, string action, string? parameters, string? result, bool success, ProjectScope scope, CancellationToken ct = default);
    Task<IReadOnlyList<ToolAuditLog>> ListAsync(int take = 200, CancellationToken ct = default);
}
