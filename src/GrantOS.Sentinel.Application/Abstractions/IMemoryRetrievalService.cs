using GrantOS.Sentinel.Application.Models;
using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Application.Abstractions;

public interface IMemoryRetrievalService
{
    Task<MemoryRetrievalResult> RetrieveAsync(
        string query,
        ProjectScope scope,
        CancellationToken ct = default);
}
