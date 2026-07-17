namespace GrantOS.Sentinel.Application.Options;

/// <summary>Safety, capacity, and retrieval limits for local project indexing.</summary>
public sealed class WorkspaceIndexOptions
{
    public const string SectionName = "WorkspaceIndex";

    public bool Enabled { get; set; } = true;
    public int MaxFilesPerWorkspace { get; set; } = 5_000;
    public int MaxFileBytes { get; set; } = 262_144;
    public int MaxRetrievedDocuments { get; set; } = 5;
    public int MaxContextCharacters { get; set; } = 8_000;
    public int MaxDocumentContextCharacters { get; set; } = 2_000;
    public List<string> IncludedExtensions { get; set; } =
    [
        ".cs", ".cshtml", ".razor", ".fs", ".vb", ".js", ".jsx", ".ts", ".tsx",
        ".json", ".jsonc", ".md", ".txt", ".xml", ".yml", ".yaml", ".toml",
        ".props", ".targets", ".sln", ".slnx", ".csproj", ".fsproj", ".vbproj",
        ".html", ".css", ".scss", ".sql", ".sh", ".zsh", ".ps1", ".py", ".go",
        ".rs", ".java", ".kt", ".swift", ".dockerfile"
    ];
    public List<string> IncludedFileNames { get; set; } =
    [
        "Dockerfile", ".editorconfig", ".gitignore", ".gitattributes"
    ];
    public List<string> IgnoredDirectories { get; set; } =
    [
        ".git", ".svn", ".hg", ".idea", ".vs", ".vscode", "bin", "obj",
        "node_modules", "dist", "build", "coverage", ".next", ".nuxt", "vendor"
    ];
}
