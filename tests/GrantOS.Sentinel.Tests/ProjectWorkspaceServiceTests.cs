using GrantOS.Sentinel.Application.Options;
using GrantOS.Sentinel.Application.Models;
using GrantOS.Sentinel.Domain.Enums;
using GrantOS.Sentinel.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace GrantOS.Sentinel.Tests;

public sealed class ProjectWorkspaceServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "grantos-sentinel-workspace-tests", Guid.NewGuid().ToString("N"));
    private readonly TestDbContextFactory _factory = new();
    private readonly ProjectWorkspaceService _service;

    public ProjectWorkspaceServiceTests()
    {
        Directory.CreateDirectory(_root);
        var agentOptions = Options.Create(new AgentOptions { AllowedDirectories = [_root] });
        var indexOptions = Options.Create(new WorkspaceIndexOptions
        {
            MaxFilesPerWorkspace = 20,
            MaxFileBytes = 1_024,
            MaxRetrievedDocuments = 3,
            MaxContextCharacters = 2_000,
            MaxDocumentContextCharacters = 500
        });
        _service = new ProjectWorkspaceService(
            _factory,
            new FileSystemPathPolicy(agentOptions),
            indexOptions);
    }

    [Fact]
    public async Task Index_adds_updates_and_removes_supported_files_only()
    {
        var source = Path.Combine(_root, "source");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(Path.Combine(source, "bin"));
        await File.WriteAllTextAsync(Path.Combine(source, "Widget.cs"), "public class Widget { }");
        await File.WriteAllTextAsync(Path.Combine(source, "README.md"), "initial docs");
        await File.WriteAllTextAsync(Path.Combine(source, ".editorconfig"), "root = true");
        await File.WriteAllTextAsync(Path.Combine(source, "image.png"), "not indexed");
        await File.WriteAllTextAsync(Path.Combine(source, "bin", "Generated.cs"), "ignored");
        var workspace = await _service.CreateAsync("Widget", source, ProjectScope.Work);

        var first = await _service.IndexAsync(workspace.Id);
        await File.WriteAllTextAsync(Path.Combine(source, "README.md"), "updated docs");
        File.Delete(Path.Combine(source, "Widget.cs"));
        await File.WriteAllTextAsync(Path.Combine(source, "NewFile.ts"), "export const value = 1;");
        var second = await _service.IndexAsync(workspace.Id);

        Assert.Equal(3, first.IndexedFiles);
        Assert.Equal(3, first.AddedFiles);
        Assert.Equal(3, second.IndexedFiles);
        Assert.Equal(1, second.AddedFiles);
        Assert.Equal(1, second.UpdatedFiles);
        Assert.Equal(1, second.RemovedFiles);
        var listed = Assert.Single(await _service.ListAsync());
        Assert.Equal(3, listed.IndexedFileCount);
    }

    [Fact]
    public async Task Retrieve_returns_ranked_same_scope_context_with_source_path()
    {
        var workRoot = Path.Combine(_root, "work");
        var personalRoot = Path.Combine(_root, "personal");
        Directory.CreateDirectory(workRoot);
        Directory.CreateDirectory(personalRoot);
        await File.WriteAllTextAsync(Path.Combine(workRoot, "RuntimeStateFile.cs"), "class RuntimeStateFile { string Endpoint; }");
        await File.WriteAllTextAsync(Path.Combine(personalRoot, "Private.md"), "RuntimeStateFile secret personal note");
        var work = await _service.CreateAsync("Sentinel", workRoot, ProjectScope.Work);
        var personal = await _service.CreateAsync("Private", personalRoot, ProjectScope.Personal);
        await _service.IndexAsync(work.Id);
        await _service.IndexAsync(personal.Id);

        var result = await _service.RetrieveAsync("How does RuntimeStateFile expose the endpoint?", ProjectScope.Work);

        var document = Assert.Single(result.Documents);
        Assert.Equal("Sentinel", document.WorkspaceName);
        Assert.Equal("RuntimeStateFile.cs", document.RelativePath);
        Assert.Contains("RuntimeStateFile", document.Content);
        Assert.Contains("untrusted excerpts", result.PromptText);
        Assert.DoesNotContain("secret personal", result.PromptText);
    }

    [Fact]
    public async Task Create_rejects_workspace_outside_allowlist()
    {
        var outside = Path.Combine(Path.GetTempPath(), "outside-sentinel-workspace", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outside);
        try
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateAsync("Outside", outside, ProjectScope.Work));
            Assert.Contains("outside", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(outside);
        }
    }

    [Fact]
    public async Task Index_excludes_sensitive_files_and_reports_progress()
    {
        var source = Path.Combine(_root, "sensitive");
        Directory.CreateDirectory(source);
        await File.WriteAllTextAsync(Path.Combine(source, ".env.local"), "API_KEY=secret");
        await File.WriteAllTextAsync(Path.Combine(source, "safe.cs"), "class Safe { }");
        var workspace = await _service.CreateAsync("Safe", source, ProjectScope.Work);
        var reports = new List<WorkspaceIndexProgress>();

        var result = await _service.IndexAsync(workspace.Id, new InlineProgress<WorkspaceIndexProgress>(reports.Add));

        Assert.Equal(1, result.IndexedFiles);
        Assert.Single(reports);
        Assert.Equal("safe.cs", reports[0].CurrentPath);
    }

    public void Dispose()
    {
        _factory.Dispose();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
