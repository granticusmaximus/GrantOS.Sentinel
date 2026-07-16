using GrantOS.Sentinel.Domain.Entities;

namespace GrantOS.Sentinel.Application.Abstractions;

public interface ISystemPromptService
{
    Task<IReadOnlyList<SystemPrompt>> ListAsync(CancellationToken ct = default);
    Task<SystemPrompt?> GetAsync(int id, CancellationToken ct = default);
    Task<SystemPrompt?> GetDefaultAsync(CancellationToken ct = default);
    Task<SystemPrompt> CreateAsync(SystemPrompt prompt, CancellationToken ct = default);
    Task UpdateAsync(SystemPrompt prompt, CancellationToken ct = default);
    Task SetDefaultAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
