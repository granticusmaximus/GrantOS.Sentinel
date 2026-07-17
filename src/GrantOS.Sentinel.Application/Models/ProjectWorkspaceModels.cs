namespace GrantOS.Sentinel.Application.Models;

public sealed record WorkspaceIndexResult(int IndexedFiles, int AddedFiles, int UpdatedFiles, int RemovedFiles, long IndexedBytes);

public sealed record RetrievedProjectDocument(
    int WorkspaceId,
    string WorkspaceName,
    string RelativePath,
    string Content,
    int Score);

public sealed record ProjectRetrievalResult(
    IReadOnlyList<RetrievedProjectDocument> Documents,
    string PromptText)
{
    public static ProjectRetrievalResult Empty { get; } = new([], string.Empty);
}
