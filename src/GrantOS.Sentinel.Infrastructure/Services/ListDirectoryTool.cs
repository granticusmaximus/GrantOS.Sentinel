using System.Text.Json;
using GrantOS.Sentinel.Application.Abstractions;
using GrantOS.Sentinel.Application.Options;
using Microsoft.Extensions.Options;

namespace GrantOS.Sentinel.Infrastructure.Services;

public sealed class ListDirectoryTool(FileSystemPathPolicy pathPolicy, IOptions<AgentOptions> options) : IAgentTool
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>("""
        {
          "type": "object",
          "required": ["path"],
          "properties": {
            "path": {
              "type": "string",
              "description": "Absolute path, or a path relative to the Sentinel working directory."
            }
          }
        }
        """);

    public string Name => "list_directory";
    public string Description => "Lists files and subdirectories inside an allowed directory.";
    public JsonElement ParametersSchema => Schema;

    public string DescribeInvocation(JsonElement arguments) =>
        TryGetPath(arguments, out var path) ? $"List directory: `{path}`" : "List a directory (no path provided).";

    public AgentToolValidationResult ValidateInvocation(JsonElement arguments)
    {
        if (!TryGetPath(arguments, out var path))
            return AgentToolValidationResult.Invalid("A directory path is required.");
        if (!pathPolicy.TryResolve(path, out var resolved, out var error))
            return AgentToolValidationResult.Invalid(error);
        if (!Directory.Exists(resolved))
            return AgentToolValidationResult.Invalid($"Directory '{resolved}' does not exist.");
        return AgentToolValidationResult.Valid;
    }

    public Task<AgentToolResult> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var validation = ValidateInvocation(arguments);
        if (!validation.IsValid)
            return Task.FromResult(new AgentToolResult(false, validation.Error!));

        TryGetPath(arguments, out var path);
        pathPolicy.TryResolve(path, out var resolved, out _);

        try
        {
            ct.ThrowIfCancellationRequested();
            var limit = Math.Max(1, options.Value.MaxDirectoryEntries);
            var entries = new DirectoryInfo(resolved)
                .EnumerateFileSystemInfos()
                .OrderBy(item => item is FileInfo)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Take(limit + 1)
                .ToList();
            var truncated = entries.Count > limit;
            var lines = entries.Take(limit).Select(item => item switch
            {
                DirectoryInfo => $"[dir]  {item.Name}/",
                FileInfo file => $"[file] {item.Name} ({file.Length:N0} bytes)",
                _ => item.Name
            }).ToList();
            if (truncated)
                lines.Add($"… listing truncated at {limit:N0} entries");
            return Task.FromResult(new AgentToolResult(true, lines.Count == 0 ? "(empty directory)" : string.Join('\n', lines)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(new AgentToolResult(false, $"Could not list directory: {ex.Message}"));
        }
    }

    private static bool TryGetPath(JsonElement arguments, out string? path)
    {
        path = arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty("path", out var value)
            ? value.GetString()
            : null;
        return !string.IsNullOrWhiteSpace(path);
    }
}
