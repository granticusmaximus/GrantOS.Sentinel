namespace GrantOS.Sentinel.Domain.Entities;

/// <summary>
/// A saved configuration for an Ollama model: which model to call and the
/// inference knobs to send with it. Decouples "the model" from "how we use it".
/// </summary>
public class ModelProfile
{
    public int Id { get; set; }

    /// <summary>Ollama model tag, e.g. "qwen2.5-coder" or "qwen2.5-coder:7b".</summary>
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ContextLength { get; set; } = 8192;
    public double Temperature { get; set; } = 0.7;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
