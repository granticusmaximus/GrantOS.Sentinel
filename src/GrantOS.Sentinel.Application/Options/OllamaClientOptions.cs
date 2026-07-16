namespace GrantOS.Sentinel.Application.Options;

/// <summary>Strongly-typed Ollama connection settings, bound from the "Ollama" config section.</summary>
public sealed class OllamaClientOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string DefaultModel { get; set; } = "qwen2.5-coder";

    /// <summary>Request timeout. Generation on modest local hardware can be slow, so this is generous.</summary>
    public int TimeoutSeconds { get; set; } = 300;
}
