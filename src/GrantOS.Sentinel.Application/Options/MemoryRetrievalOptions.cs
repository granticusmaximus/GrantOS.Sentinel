namespace GrantOS.Sentinel.Application.Options;

/// <summary>Controls automatic retrieval from the local Memory vault.</summary>
public sealed class MemoryRetrievalOptions
{
    public const string SectionName = "MemoryRetrieval";

    public bool Enabled { get; set; } = true;
    public int MaxEntries { get; set; } = 5;
    public int MinimumScore { get; set; } = 2;
    public int MaxContextCharacters { get; set; } = 6_000;
    public int MaxEntryContentCharacters { get; set; } = 1_500;
}
