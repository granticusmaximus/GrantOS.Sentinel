using System.Text.Json;
using GrantOS.Sentinel.Application.Models;
using GrantOS.Sentinel.Application.Options;
using GrantOS.Sentinel.Application.Services;
using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Domain.Enums;
using Xunit;

namespace GrantOS.Sentinel.Tests;

public sealed class ChatRequestComposerTests
{
    [Fact]
    public void Compose_preserves_assistant_tool_call_before_its_result()
    {
        var calls = new[]
        {
            new OllamaToolCall
            {
                Function = new OllamaToolCallFunction
                {
                    Name = "read_file",
                    Arguments = JsonSerializer.SerializeToElement(new { path = "README.md" })
                }
            }
        };
        var history = new[]
        {
            Message(ChatRole.User, "Read the readme"),
            Message(ChatRole.Assistant, "", JsonSerializer.Serialize(calls)),
            Message(ChatRole.Tool, "contents", toolName: "read_file"),
            Message(ChatRole.User, "Summarize it")
        };

        var result = Composer().Compose("model", 8_192, [], history, null, []);

        Assert.Equal(["user", "assistant", "tool", "user"], result.Request.Messages.Select(message => message.Role));
        Assert.Equal("read_file", Assert.Single(result.Request.Messages[1].ToolCalls!).Function.Name);
        Assert.Equal("read_file", result.Request.Messages[2].ToolName);
    }

    [Fact]
    public void Compose_omits_old_history_and_truncates_large_tool_results()
    {
        var history = Enumerable.Range(1, 12)
            .Select(index => Message(index % 2 == 0 ? ChatRole.Assistant : ChatRole.User, new string((char)('a' + index), 300)))
            .Append(Message(ChatRole.Tool, new string('x', 2_000), toolName: "read_file"))
            .ToArray();

        var result = Composer(maxToolCharacters: 240).Compose(
            "model", 500, [new OllamaMessage("system", "system")], history, null, []);

        Assert.True(result.OmittedHistoryMessages > 0);
        Assert.Contains(result.Request.Messages, message => message.Role == "system" && message.Content.Contains("omitted"));
        Assert.True(result.Request.Messages.Last().Content.Length <= 240);
    }

    private static ChatRequestComposer Composer(int maxToolCharacters = 8_000) => new(new ChatContextOptions
    {
        ReserveOutputTokens = 100,
        EstimatedCharactersPerToken = 1,
        MaxToolResultCharacters = maxToolCharacters,
        MinimumRecentMessageGroups = 2
    });

    private static ChatMessage Message(
        ChatRole role,
        string content,
        string? toolCallsJson = null,
        string? toolName = null) => new()
        {
            Role = role,
            Content = content,
            ToolCallsJson = toolCallsJson,
            ToolName = toolName
        };
}
