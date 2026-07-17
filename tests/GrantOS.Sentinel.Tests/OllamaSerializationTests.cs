using System.Text.Json;
using GrantOS.Sentinel.Application.Models;
using Xunit;

namespace GrantOS.Sentinel.Tests;

/// <summary>
/// Locks down the JSON contract with Ollama. These are the fields most likely to
/// break silently: Ollama expects snake_case (num_ctx, eval_count) while the rest
/// of the payload is single-word lowercase, and the service uses Web defaults.
/// </summary>
public sealed class OllamaSerializationTests
{
    // Mirror the exact options the OllamaChatService uses.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ChatRequest_serializes_options_as_snake_case()
    {
        var request = new OllamaChatRequest
        {
            Model = "qwen2.5-coder",
            Messages = [new OllamaMessage("user", "hi")],
            Stream = true,
            Options = new OllamaOptions { NumCtx = 8192, Temperature = 0.3 }
        };

        var json = JsonSerializer.Serialize(request, Json);

        Assert.Contains("\"num_ctx\":8192", json);
        Assert.Contains("\"temperature\":0.3", json);
        Assert.DoesNotContain("numCtx", json);
        Assert.Contains("\"model\":\"qwen2.5-coder\"", json);
        Assert.Contains("\"role\":\"user\"", json);
        Assert.Contains("\"content\":\"hi\"", json);
        Assert.Contains("\"stream\":true", json);
    }

    [Fact]
    public void StreamChunk_deserializes_message_content_and_done()
    {
        const string line = """{"model":"qwen2.5-coder","message":{"role":"assistant","content":"Hello"},"done":false}""";

        var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line, Json);

        Assert.NotNull(chunk);
        Assert.Equal("Hello", chunk!.Message?.Content);
        Assert.False(chunk.Done);
    }

    [Fact]
    public void FinalChunk_reads_generation_metrics_from_snake_case()
    {
        const string line = """{"message":{"role":"assistant","content":""},"done":true,"eval_count":42,"prompt_eval_count":7,"total_duration":3500000000,"load_duration":100000000,"prompt_eval_duration":400000000,"eval_duration":3000000000}""";

        var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line, Json);

        Assert.NotNull(chunk);
        Assert.True(chunk!.Done);
        Assert.Equal(42, chunk.EvalCount);
        Assert.Equal(7, chunk.PromptEvalCount);
        Assert.Equal(3_500_000_000, chunk.TotalDurationNanoseconds);
        Assert.Equal(100_000_000, chunk.LoadDurationNanoseconds);
        Assert.Equal(400_000_000, chunk.PromptEvalDurationNanoseconds);
        Assert.Equal(3_000_000_000, chunk.EvalDurationNanoseconds);
    }

    [Fact]
    public void TagsResponse_maps_parameter_size_and_family()
    {
        const string body = """
        {"models":[{"name":"qwen2.5-coder:7b","size":4700000000,"details":{"family":"qwen2","parameter_size":"7.6B","quantization_level":"Q4_K_M"}}]}
        """;

        var tags = JsonSerializer.Deserialize<OllamaTagsResponse>(body, Json);

        Assert.NotNull(tags);
        var model = Assert.Single(tags!.Models);
        Assert.Equal("qwen2.5-coder:7b", model.Name);
        Assert.Equal(4_700_000_000, model.Size);
        Assert.Equal("7.6B", model.Details?.ParameterSize);
        Assert.Equal("qwen2", model.Details?.Family);
    }
}
