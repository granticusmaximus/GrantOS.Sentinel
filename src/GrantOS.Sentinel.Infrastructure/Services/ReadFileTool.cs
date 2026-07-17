using System.Text;
using System.Text.Json;
using GrantOS.Sentinel.Application.Abstractions;
using GrantOS.Sentinel.Application.Options;
using Microsoft.Extensions.Options;

namespace GrantOS.Sentinel.Infrastructure.Services;

public sealed class ReadFileTool(FileSystemPathPolicy pathPolicy, IOptions<AgentOptions> options) : IAgentTool
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

    public string Name => "read_file";
    public string Description => "Reads a UTF-8 text file inside an allowed directory.";
    public JsonElement ParametersSchema => Schema;

    public string DescribeInvocation(JsonElement arguments) =>
        TryGetPath(arguments, out var path) ? $"Read file: `{path}`" : "Read a file (no path provided).";

    public AgentToolValidationResult ValidateInvocation(JsonElement arguments)
    {
        if (!TryGetPath(arguments, out var path))
            return AgentToolValidationResult.Invalid("A file path is required.");
        if (!pathPolicy.TryResolve(path, out var resolved, out var error))
            return AgentToolValidationResult.Invalid(error);
        if (!File.Exists(resolved))
            return AgentToolValidationResult.Invalid($"File '{resolved}' does not exist.");
        return AgentToolValidationResult.Valid;
    }

    public async Task<AgentToolResult> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var validation = ValidateInvocation(arguments);
        if (!validation.IsValid)
            return new AgentToolResult(false, validation.Error!);

        TryGetPath(arguments, out var path);
        pathPolicy.TryResolve(path, out var resolved, out _);

        try
        {
            var limit = Math.Max(1, options.Value.MaxReadBytes);
            await using var stream = new FileStream(resolved, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
            var buffer = new byte[limit + 1];
            var total = 0;
            while (total < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), ct);
                if (read == 0) break;
                total += read;
            }

            if (total > limit)
                return new AgentToolResult(false, $"File exceeds the configured {limit:N0}-byte read limit.");

            return new AgentToolResult(true, total == 0 ? "(empty file)" : Encoding.UTF8.GetString(buffer, 0, total));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new AgentToolResult(false, $"Could not read file: {ex.Message}");
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
