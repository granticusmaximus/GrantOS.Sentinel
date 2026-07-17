using System.Text.Json;
using GrantOS.Sentinel.Application.Models;
using GrantOS.Sentinel.Application.Options;
using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Domain.Enums;

namespace GrantOS.Sentinel.Application.Services;

/// <summary>Builds bounded Ollama requests while preserving complete tool-call/result groups.</summary>
public sealed class ChatRequestComposer(ChatContextOptions settings)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ChatRequestComposition Compose(
        string model,
        int contextLength,
        IReadOnlyList<OllamaMessage> systemMessages,
        IReadOnlyList<ChatMessage> history,
        OllamaOptions? ollamaOptions,
        IReadOnlyList<OllamaToolDefinition>? tools)
    {
        var charsPerToken = Math.Clamp(settings.EstimatedCharactersPerToken, 1, 12);
        var availableTokens = Math.Max(256, contextLength - Math.Max(0, settings.ReserveOutputTokens));
        var characterBudget = (int)Math.Min(int.MaxValue, availableTokens * charsPerToken);
        var wire = new List<OllamaMessage>();
        var usedCharacters = 0;

        foreach (var message in systemMessages)
        {
            if (string.IsNullOrWhiteSpace(message.Content))
                continue;
            var remaining = characterBudget - usedCharacters;
            if (remaining <= 100)
                break;
            var content = Truncate(message.Content, remaining);
            wire.Add(message with { Content = content });
            usedCharacters += EstimateCharacters(wire[^1]);
        }

        var eligibleHistory = history
            .Where(message => message.Role != ChatRole.System &&
                (!string.IsNullOrEmpty(message.Content) || !string.IsNullOrWhiteSpace(message.ToolCallsJson)))
            .ToList();
        var groups = GroupHistory(eligibleHistory);
        var selectedGroups = new List<IReadOnlyList<ChatMessage>>();
        var minimumGroups = Math.Max(1, settings.MinimumRecentMessageGroups);

        for (var index = groups.Count - 1; index >= 0; index--)
        {
            var group = groups[index];
            var groupCharacters = group.Sum(message => EstimateCharacters(ToWireMessage(message, settings.MaxToolResultCharacters)));
            if (selectedGroups.Count >= minimumGroups && usedCharacters + groupCharacters > characterBudget)
                break;
            selectedGroups.Add(group);
            usedCharacters += groupCharacters;
        }
        selectedGroups.Reverse();

        var includedMessages = selectedGroups.Sum(group => group.Count);
        var omittedMessages = eligibleHistory.Count - includedMessages;
        if (omittedMessages > 0)
            wire.Add(new OllamaMessage("system", $"{omittedMessages} older conversation messages were omitted to fit the model context window."));

        foreach (var group in selectedGroups)
            foreach (var message in group)
                wire.Add(ToWireMessage(message, settings.MaxToolResultCharacters));

        var estimatedCharacters = wire.Sum(EstimateCharacters);
        return new ChatRequestComposition(
            new OllamaChatRequest
            {
                Model = model,
                Messages = wire,
                Stream = true,
                Options = ollamaOptions,
                Tools = tools is { Count: > 0 } ? tools : null
            },
            includedMessages,
            omittedMessages,
            (int)Math.Ceiling(estimatedCharacters / charsPerToken));
    }

    private static List<IReadOnlyList<ChatMessage>> GroupHistory(IReadOnlyList<ChatMessage> messages)
    {
        var groups = new List<IReadOnlyList<ChatMessage>>();
        for (var index = 0; index < messages.Count; index++)
        {
            var message = messages[index];
            if (message.Role == ChatRole.Assistant && !string.IsNullOrWhiteSpace(message.ToolCallsJson))
            {
                var toolGroup = new List<ChatMessage> { message };
                while (index + 1 < messages.Count && messages[index + 1].Role == ChatRole.Tool)
                    toolGroup.Add(messages[++index]);
                groups.Add(toolGroup);
            }
            else
            {
                groups.Add([message]);
            }
        }
        return groups;
    }

    private static OllamaMessage ToWireMessage(ChatMessage message, int maxToolResultCharacters)
    {
        var role = message.Role switch
        {
            ChatRole.User => "user",
            ChatRole.Tool => "tool",
            _ => "assistant"
        };
        var content = message.Role == ChatRole.Tool
            ? Truncate(message.Content, Math.Max(200, maxToolResultCharacters))
            : message.Content;
        IReadOnlyList<OllamaToolCall>? calls = null;
        if (!string.IsNullOrWhiteSpace(message.ToolCallsJson))
        {
            try
            {
                calls = JsonSerializer.Deserialize<IReadOnlyList<OllamaToolCall>>(message.ToolCallsJson, Json);
            }
            catch (JsonException)
            {
                // Historical malformed metadata should not make the conversation unloadable.
            }
        }
        return new OllamaMessage(role, content)
        {
            ToolName = message.Role == ChatRole.Tool ? message.ToolName : null,
            ToolCalls = calls
        };
    }

    private static int EstimateCharacters(OllamaMessage message) =>
        message.Content.Length + (message.ToolName?.Length ?? 0) +
        (message.ToolCalls is null ? 0 : JsonSerializer.Serialize(message.ToolCalls, Json).Length) + 24;

    private static string Truncate(string content, int maxCharacters)
    {
        if (content.Length <= maxCharacters)
            return content;
        if (maxCharacters <= 1)
            return "…";
        return content[..(maxCharacters - 1)] + "…";
    }
}
