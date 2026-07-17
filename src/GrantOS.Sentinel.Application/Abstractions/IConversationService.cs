using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Domain.Enums;
using GrantOS.Sentinel.Application.Models;

namespace GrantOS.Sentinel.Application.Abstractions;

public interface IConversationService
{
    Task<IReadOnlyList<ConversationSummary>> ListAsync(CancellationToken ct = default);
    Task<Conversation?> GetAsync(int id, CancellationToken ct = default);
    Task<Conversation> CreateAsync(string title, string modelName, int? systemPromptId, ProjectScope scope, CancellationToken ct = default);
    Task<ChatMessage> AddMessageAsync(int conversationId, ChatRole role, string content, MessageGenerationMetrics? metrics = null, string? toolName = null, string? toolArguments = null, string? toolCallsJson = null, CancellationToken ct = default);
    Task RenameAsync(int id, string title, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
