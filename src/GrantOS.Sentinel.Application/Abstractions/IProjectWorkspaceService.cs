using GrantOS.Sentinel.Application.Models;
using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Application.Abstractions;

public interface IProjectWorkspaceService
{
    Task<IReadOnlyList<ProjectWorkspace>> ListAsync(CancellationToken ct = default);
    Task<ProjectWorkspace> CreateAsync(string name, string rootPath, ProjectScope scope, CancellationToken ct = default);
    Task<WorkspaceIndexResult> IndexAsync(
        int workspaceId,
        IProgress<WorkspaceIndexProgress>? progress = null,
        CancellationToken ct = default);
    Task<ProjectRetrievalResult> RetrieveAsync(string query, ProjectScope scope, CancellationToken ct = default);
    Task DeleteAsync(int workspaceId, CancellationToken ct = default);
}
