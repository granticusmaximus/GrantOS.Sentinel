using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Application.Models;

/// <summary>A minimal role+content pair used to build a request to the model.</summary>
public readonly record struct ChatTurn(ChatRole Role, string Content);
