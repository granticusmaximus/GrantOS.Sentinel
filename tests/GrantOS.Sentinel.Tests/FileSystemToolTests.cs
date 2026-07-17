using System.Text.Json;
using GrantOS.Sentinel.Application.Options;
using GrantOS.Sentinel.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace GrantOS.Sentinel.Tests;

public sealed class FileSystemToolTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), "grantos-sentinel-tests", Guid.NewGuid().ToString("N"));
    private readonly string _allowedRoot;
    private readonly string _outsideRoot;
    private readonly IOptions<AgentOptions> _options;
    private readonly FileSystemPathPolicy _policy;

    public FileSystemToolTests()
    {
        _allowedRoot = Path.Combine(_testRoot, "allowed");
        _outsideRoot = Path.Combine(_testRoot, "outside");
        Directory.CreateDirectory(_allowedRoot);
        Directory.CreateDirectory(_outsideRoot);
        _options = Options.Create(new AgentOptions
        {
            AllowedDirectories = [_allowedRoot],
            MaxReadBytes = 32,
            MaxWriteBytes = 64,
            MaxDirectoryEntries = 2
        });
        _policy = new FileSystemPathPolicy(_options);
    }

    [Fact]
    public void Path_policy_allows_descendants_and_rejects_outside_paths()
    {
        Assert.True(_policy.TryResolve(Path.Combine(_allowedRoot, "note.txt"), out _, out _));
        Assert.False(_policy.TryResolve(Path.Combine(_outsideRoot, "secret.txt"), out _, out var error));
        Assert.Contains("outside", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Path_policy_rejects_a_symlink_that_escapes_the_allowed_root()
    {
        var link = Path.Combine(_allowedRoot, "escape");
        Directory.CreateSymbolicLink(link, _outsideRoot);

        Assert.False(_policy.TryResolve(Path.Combine(link, "secret.txt"), out _, out var error));
        Assert.Contains("outside", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Read_file_returns_content_and_enforces_size_limit()
    {
        var tool = new ReadFileTool(_policy, _options);
        var smallPath = Path.Combine(_allowedRoot, "small.txt");
        var largePath = Path.Combine(_allowedRoot, "large.txt");
        await File.WriteAllTextAsync(smallPath, "hello Sentinel");
        await File.WriteAllTextAsync(largePath, new string('x', 33));

        var small = await tool.ExecuteAsync(Args(new { path = smallPath }));
        var large = await tool.ExecuteAsync(Args(new { path = largePath }));

        Assert.True(small.Success);
        Assert.Equal("hello Sentinel", small.Output);
        Assert.False(large.Success);
        Assert.Contains("read limit", large.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Write_file_requires_explicit_overwrite_and_rejects_outside_path_before_approval()
    {
        var tool = new WriteFileTool(_policy, _options);
        var path = Path.Combine(_allowedRoot, "created.txt");

        var created = await tool.ExecuteAsync(Args(new { path, content = "first" }));
        var overwriteDenied = tool.ValidateInvocation(Args(new { path, content = "second" }));
        var outside = tool.ValidateInvocation(Args(new { path = Path.Combine(_outsideRoot, "nope.txt"), content = "blocked" }));
        var replaced = await tool.ExecuteAsync(Args(new { path, content = "second", overwrite = true }));

        Assert.True(created.Success);
        Assert.False(overwriteDenied.IsValid);
        Assert.Contains("overwrite", overwriteDenied.Error!, StringComparison.OrdinalIgnoreCase);
        Assert.False(outside.IsValid);
        Assert.True(replaced.Success);
        Assert.Equal("second", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task List_directory_orders_directories_first_and_truncates_at_the_limit()
    {
        Directory.CreateDirectory(Path.Combine(_allowedRoot, "folder"));
        await File.WriteAllTextAsync(Path.Combine(_allowedRoot, "a.txt"), "a");
        await File.WriteAllTextAsync(Path.Combine(_allowedRoot, "b.txt"), "bb");
        var tool = new ListDirectoryTool(_policy, _options);

        var result = await tool.ExecuteAsync(Args(new { path = _allowedRoot }));

        Assert.True(result.Success);
        Assert.StartsWith("[dir]  folder/", result.Output);
        Assert.Contains("truncated", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    private static JsonElement Args<T>(T value) => JsonSerializer.SerializeToElement(value);

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
            Directory.Delete(_testRoot, recursive: true);
    }
}
