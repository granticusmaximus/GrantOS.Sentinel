using GrantOS.Sentinel.Application.Models;
using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Application.Abstractions;

public interface IProjectStandardService
{
    Task<IReadOnlyList<ProjectStandard>> ListAsync(ProjectScope? scope = null, CancellationToken ct = default);
    Task<ProjectStandard> CreateAsync(ProjectStandard standard, CancellationToken ct = default);
    Task UpdateAsync(ProjectStandard standard, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<StandardContextResult> GetContextAsync(ProjectScope scope, CancellationToken ct = default);
}
