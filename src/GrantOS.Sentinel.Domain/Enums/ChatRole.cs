namespace GrantOS.Sentinel.Domain.Enums;

/// <summary>Author of a chat message, mirroring Ollama's role vocabulary.</summary>
public enum ChatRole
{
    System = 0,
    User = 1,
    Assistant = 2,
    Tool = 3
}
