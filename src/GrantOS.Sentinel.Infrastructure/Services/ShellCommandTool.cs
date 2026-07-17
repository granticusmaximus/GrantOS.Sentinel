using System.Diagnostics;
using System.Text;
using System.Text.Json;
using GrantOS.Sentinel.Application.Abstractions;

namespace GrantOS.Sentinel.Infrastructure.Services;

/// <summary>Runs a shell command as the current user (no elevation) and returns its output.</summary>
public sealed class ShellCommandTool : IAgentTool
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private static readonly JsonElement Schema = JsonSerializer.Deserialize<JsonElement>("""
        {
          "type": "object",
          "required": ["command"],
          "properties": {
            "command": {
              "type": "string",
              "description": "The full shell command to execute, e.g. \"ls -la\"."
            }
          }
        }
        """);

    public string Name => "run_shell_command";

    public string Description =>
        "Runs a shell command on the user's machine and returns its stdout/stderr. " +
        "Runs as the current user with no elevated privileges and a 30 second timeout.";

    public JsonElement ParametersSchema => Schema;

    public string DescribeInvocation(JsonElement arguments)
    {
        var command = GetCommand(arguments);
        return string.IsNullOrWhiteSpace(command)
            ? "Run a shell command (no command text provided)."
            : $"Run shell command: `{command}`";
    }

    public async Task<AgentToolResult> ExecuteAsync(JsonElement arguments, CancellationToken ct = default)
    {
        var command = GetCommand(arguments);
        if (string.IsNullOrWhiteSpace(command))
            return new AgentToolResult(false, "No command was provided.");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/zsh",
                ArgumentList = { "-c", command },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(Timeout);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            return new AgentToolResult(false, $"Command timed out after {Timeout.TotalSeconds:0} seconds.");
        }

        var output = stdout.ToString().TrimEnd();
        var error = stderr.ToString().TrimEnd();
        var combined = error.Length == 0 ? output : $"{output}\n[stderr]\n{error}";
        return new AgentToolResult(process.ExitCode == 0, combined.Length == 0 ? "(no output)" : combined);
    }

    private static string? GetCommand(JsonElement arguments) =>
        arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty("command", out var c)
            ? c.GetString()
            : null;
}
