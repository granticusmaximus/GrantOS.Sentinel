namespace GrantOS.Sentinel.Application.Options;

/// <summary>Controls model context budgeting and oversized tool-result handling.</summary>
public sealed class ChatContextOptions
{
    public const string SectionName = "ChatContext";

    public int ReserveOutputTokens { get; set; } = 1_024;
    public double EstimatedCharactersPerToken { get; set; } = 4;
    public int MaxToolResultCharacters { get; set; } = 8_000;
    public int MinimumRecentMessageGroups { get; set; } = 2;
}
