namespace GrantOS.Sentinel.Application.Options;

/// <summary>Controls how enabled project standards are added to model context.</summary>
public sealed class StandardsOptions
{
    public const string SectionName = "Standards";

    public bool Enabled { get; set; } = true;
    public int MaxStandards { get; set; } = 12;
    public int MaxContextCharacters { get; set; } = 6_000;
    public int MaxStandardContentCharacters { get; set; } = 2_000;
}
