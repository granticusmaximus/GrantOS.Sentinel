using GrantOS.Sentinel.Domain.Entities;

namespace GrantOS.Sentinel.Application.Abstractions;

public interface IMemoryService
{
    Task<IReadOnlyList<MemoryEntry>> ListAsync(string? search = null, CancellationToken ct = default);
    Task<MemoryEntry?> GetAsync(int id, CancellationToken ct = default);
    Task<MemoryEntry> CreateAsync(MemoryEntry entry, CancellationToken ct = default);
    Task UpdateAsync(MemoryEntry entry, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
