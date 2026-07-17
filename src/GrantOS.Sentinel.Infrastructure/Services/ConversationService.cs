using GrantOS.Sentinel.Application.Abstractions;
using GrantOS.Sentinel.Application.Models;
using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Domain.Enums;
using GrantOS.Sentinel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GrantOS.Sentinel.Infrastructure.Services;

public sealed class ConversationService(IDbContextFactory<SentinelDbContext> factory) : IConversationService
{
    public async Task<IReadOnlyList<ConversationSummary>> ListAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Conversations
            .AsNoTracking()
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ConversationSummary(
                c.Id,
                c.Title,
                c.ModelName,
                c.Scope,
                c.UpdatedAt,
                c.Messages.Count,
                c.Messages.Sum(message => message.TokenCount ?? 0)))
            .ToListAsync(ct);
    }

    public async Task<Conversation?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Conversations
            .AsNoTracking()
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Conversation> CreateAsync(string title, string modelName, int? systemPromptId, ProjectScope scope, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        var conversation = new Conversation
        {
            Title = string.IsNullOrWhiteSpace(title) ? "New conversation" : title.Trim(),
            ModelName = modelName,
            SystemPromptId = systemPromptId,
            Scope = scope,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(ct);
        return conversation;
    }

    public async Task<ChatMessage> AddMessageAsync(int conversationId, ChatRole role, string content, MessageGenerationMetrics? metrics = null, string? toolName = null, string? toolArguments = null, string? toolCallsJson = null, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var message = new ChatMessage
        {
            ConversationId = conversationId,
            Role = role,
            Content = content,
            TokenCount = metrics?.ResponseTokenCount,
            PromptTokenCount = metrics?.PromptTokenCount,
            TotalDurationNanoseconds = metrics?.TotalDurationNanoseconds,
            LoadDurationNanoseconds = metrics?.LoadDurationNanoseconds,
            PromptEvalDurationNanoseconds = metrics?.PromptEvalDurationNanoseconds,
            EvalDurationNanoseconds = metrics?.EvalDurationNanoseconds,
            ToolName = toolName,
            ToolArguments = toolArguments,
            ToolCallsJson = toolCallsJson,
            CreatedAt = DateTime.UtcNow
        };
        db.ChatMessages.Add(message);

        var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct);
        if (conversation is not null)
            conversation.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return message;
    }

    public async Task RenameAsync(int id, string title, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (conversation is null) return;
        conversation.Title = title.Trim();
        conversation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.Conversations.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
    }
}
