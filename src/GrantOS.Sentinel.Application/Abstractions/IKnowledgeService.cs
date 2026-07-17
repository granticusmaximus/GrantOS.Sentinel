using GrantOS.Sentinel.Application.Models;

namespace GrantOS.Sentinel.Application.Abstractions;

public interface IKnowledgeService
{
    Task<KnowledgeSearchResult> SearchAsync(KnowledgeSearchRequest request, CancellationToken ct = default);
}
