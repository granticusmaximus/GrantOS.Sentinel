using GrantOS.Sentinel.Domain.Entities;

namespace GrantOS.Sentinel.Application.Abstractions;

public interface IModelProfileService
{
    Task<IReadOnlyList<ModelProfile>> ListAsync(CancellationToken ct = default);
    Task<ModelProfile?> GetDefaultAsync(CancellationToken ct = default);
    Task<ModelProfile> CreateAsync(ModelProfile profile, CancellationToken ct = default);
    Task UpdateAsync(ModelProfile profile, CancellationToken ct = default);
    Task SetDefaultAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
