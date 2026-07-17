namespace GrantOS.Sentinel.Application.Models;

public sealed record RetrievedMemory(
    int Id,
    string Title,
    string Content,
    string Category,
    string Tags,
    bool Pinned,
    int Score);

public sealed record MemoryRetrievalResult(
    IReadOnlyList<RetrievedMemory> Entries,
    string PromptText)
{
    public static MemoryRetrievalResult Empty { get; } = new([], string.Empty);
}
