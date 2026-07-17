namespace GrantOS.Sentinel.Application.Options;

/// <summary>Security and behavior settings for local agent tools.</summary>
public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    /// <summary>
    /// Directories filesystem tools may access. Relative paths are resolved from the app's
    /// working directory at startup. An empty list disables all filesystem access.
    /// </summary>
    public List<string> AllowedDirectories { get; set; } = [];

    /// <summary>Maximum file size returned by the read-file tool.</summary>
    public int MaxReadBytes { get; set; } = 262_144;

    /// <summary>Maximum UTF-8 payload accepted by the write-file tool.</summary>
    public int MaxWriteBytes { get; set; } = 1_048_576;

    /// <summary>Maximum number of entries returned by a directory listing.</summary>
    public int MaxDirectoryEntries { get; set; } = 200;

    /// <summary>Maximum page-text characters returned to the model by the browser tool.</summary>
    public int MaxBrowserTextCharacters { get; set; } = 50_000;

    /// <summary>Timeout applied to individual browser actions.</summary>
    public int BrowserTimeoutSeconds { get; set; } = 30;

    /// <summary>Whether the controlled browser is hidden. Keep false for normal use.</summary>
    public bool BrowserHeadless { get; set; }
}
