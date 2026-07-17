namespace GrantOS.Sentinel.Application.Models;

public sealed record ChatRequestComposition(
    OllamaChatRequest Request,
    int IncludedHistoryMessages,
    int OmittedHistoryMessages,
    int EstimatedInputTokens);
