using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Application.Models;

public sealed record ConversationSummary(
    int Id,
    string Title,
    string ModelName,
    ProjectScope Scope,
    DateTime UpdatedAt,
    int MessageCount,
    int GeneratedTokenCount);
