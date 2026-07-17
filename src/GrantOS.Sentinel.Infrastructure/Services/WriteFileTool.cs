using System.Text;
using System.Text.Json;
using GrantOS.Sentinel.Application.Abstractions;
using GrantOS.Sentinel.Application.Options;
using Microsoft.Extensions.Options;

namespace GrantOS.Sentinel.Infrastructure.Services;

public sealed class WriteFileTool(FileSystemPathPolicy pathPolicy, IOptions<AgentOptions> options) : IAgentTool
{
    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>("""
        {
          "type": "object",
          "required": ["path", "content"],
          "properties": {
            "path": {
              "type": "string",
              "description": "Absolute path, or a path relative to the Sentinel working directory."
            },
            "content": {
              "type": "string",
              "description": "Complete UTF-8 text to write."
            },
            "overwrite": {
              "type": "boolean",
              "description": "Must be true to replace an existing file. Defaults to false."
            }
          }
        }
        """);

    public string Name => "write_file";
    public string Description =>
        "Writes a UTF-8 text file inside an allowed directory. Existing files are protected unless overwrite is explicitly true.";
    public JsonElement ParametersSchema => Schema;

    public string DescribeInvocation(JsonElement arguments)
    {
        if (!TryGetArguments(arguments, out var path, out var content, out var overwrite))
            return "Write a file (invalid arguments).";
        return $"{(overwrite ? "Write or replace" : "Create")} file `{path}` ({Encoding.UTF8.GetByteCount(content!):N0} bytes).";
    }

    public AgentToolValidationResult ValidateInvocation(JsonElement arguments)
    {
        if (!TryGetArguments(arguments, out var path, out var content, out var overwrite))
            return AgentToolValidationResult.Invalid("A path and string content are required.");
        if (!pathPolicy.TryResolve(path, out var resolved, out var error))
            return AgentToolValidationResult.Invalid(error);
        if (Directory.Exists(resolved))
            return AgentToolValidationResult.Invalid($"Path '{resolved}' is a directory, not a file.");
        if (!Directory.Exists(Path.GetDirectoryName(resolved)))
            return AgentToolValidationResult.Invalid("The destination directory does not exist.");
        if (File.Exists(resolved) && !overwrite)
            return AgentToolValidationResult.Invalid("The file already exists; overwrite must be explicitly true to replace it.");
        if (Encoding.UTF8.GetByteCount(content!) > Math.Max(1, options.Value.MaxWriteBytes))
            return AgentToolValidationResult.Invalid($"Content exceeds the configured {options.Value.MaxWriteBytes:N0}-byte write limit.");
        return AgentToolValidationResult.Valid;
    }

    public async Task<AgentToolResult> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var validation = ValidateInvocation(arguments);
        if (!validation.IsValid)
            return new AgentToolResult(false, validation.Error!);

        TryGetArguments(arguments, out var path, out var content, out var overwrite);
        pathPolicy.TryResolve(path, out var resolved, out _);

        try
        {
            var mode = overwrite ? FileMode.Create : FileMode.CreateNew;
            await using var stream = new FileStream(resolved, mode, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            var bytes = Encoding.UTF8.GetBytes(content!);
            await stream.WriteAsync(bytes, ct);
            return new AgentToolResult(true, $"Wrote {bytes.Length:N0} bytes to '{resolved}'.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new AgentToolResult(false, $"Could not write file: {ex.Message}");
        }
    }

    private static bool TryGetArguments(JsonElement arguments, out string? path, out string? content, out bool overwrite)
    {
        path = null;
        content = null;
        overwrite = false;
        if (arguments.ValueKind != JsonValueKind.Object ||
            !arguments.TryGetProperty("path", out var pathValue) || pathValue.ValueKind != JsonValueKind.String ||
            !arguments.TryGetProperty("content", out var contentValue) || contentValue.ValueKind != JsonValueKind.String)
            return false;

        path = pathValue.GetString();
        content = contentValue.GetString();
        if (arguments.TryGetProperty("overwrite", out var overwriteValue) &&
            overwriteValue.ValueKind is JsonValueKind.True or JsonValueKind.False)
            overwrite = overwriteValue.GetBoolean();
        return !string.IsNullOrWhiteSpace(path) && content is not null;
    }
}
