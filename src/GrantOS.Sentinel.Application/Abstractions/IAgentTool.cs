using System.Text.Json;

namespace GrantOS.Sentinel.Application.Abstractions;

/// <summary>
/// A single agentic capability the model can invoke (shell, file system, browser, ...).
/// Every invocation is proposed to the user for approval before <see cref="ExecuteAsync"/>
/// ever runs - implementations should assume nothing they do is silent.
/// </summary>
public interface IAgentTool
{
    /// <summary>Wire name sent to Ollama and matched back against tool_calls.function.name.</summary>
    string Name { get; }

    /// <summary>Sent to the model so it knows when to call this tool.</summary>
    string Description { get; }

    /// <summary>JSON Schema for the tool's arguments (Ollama's "parameters" object).</summary>
    JsonElement ParametersSchema { get; }

    /// <summary>A short, human-readable summary of what this specific call would do, for the approval prompt.</summary>
    string DescribeInvocation(JsonElement arguments);

    /// <summary>
    /// Validates an invocation before it is offered to the user for approval. This is the
    /// enforcement point for rules such as filesystem allowlists; implementations must also
    /// repeat security-sensitive validation in <see cref="ExecuteAsync"/> to avoid TOCTOU gaps.
    /// </summary>
    AgentToolValidationResult ValidateInvocation(JsonElement arguments);

    Task<AgentToolResult> ExecuteAsync(JsonElement arguments, CancellationToken ct = default);
}

public sealed record AgentToolValidationResult(bool IsValid, string? Error = null)
{
    public static AgentToolValidationResult Valid { get; } = new(true);

    public static AgentToolValidationResult Invalid(string error) => new(false, error);
}

public sealed record AgentToolResult(bool Success, string Output);
