using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Application.Abstractions;

public interface IConversationService
{
    Task<IReadOnlyList<Conversation>> ListAsync(CancellationToken ct = default);
    Task<Conversation?> GetAsync(int id, CancellationToken ct = default);
    Task<Conversation> CreateAsync(string title, string modelName, int? systemPromptId, ProjectScope scope, CancellationToken ct = default);
    Task<ChatMessage> AddMessageAsync(int conversationId, ChatRole role, string content, int? tokenCount, CancellationToken ct = default);
    Task RenameAsync(int id, string title, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
