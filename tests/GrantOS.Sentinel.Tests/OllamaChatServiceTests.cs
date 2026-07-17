using System.Net;
using System.Text;
using GrantOS.Sentinel.Application.Models;
using GrantOS.Sentinel.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GrantOS.Sentinel.Tests;

public sealed class OllamaChatServiceTests
{
    [Fact]
    public async Task StreamChat_yields_completion_metrics_from_final_chunk()
    {
        const string stream = """
            {"message":{"role":"assistant","content":"Hello"},"done":false}
            {"message":{"role":"assistant","content":""},"done":true,"prompt_eval_count":10,"eval_count":5,"total_duration":2500000000,"load_duration":100000000,"prompt_eval_duration":400000000,"eval_duration":2000000000}
            """;
        using var http = new HttpClient(new StaticResponseHandler(stream))
        {
            BaseAddress = new Uri("http://localhost:11434")
        };
        var service = new OllamaChatService(http, NullLogger<OllamaChatService>.Instance);
        var request = new OllamaChatRequest
        {
            Model = "qwen3",
            Messages = [new OllamaMessage("user", "Hi")],
            Stream = true
        };
        var events = new List<OllamaStreamEvent>();

        await foreach (var evt in service.StreamChatAsync(request))
            events.Add(evt);

        Assert.Equal("Hello", Assert.IsType<OllamaContentDelta>(events[0]).Text);
        var completed = Assert.IsType<OllamaGenerationCompleted>(events[1]);
        Assert.Equal(10, completed.PromptTokenCount);
        Assert.Equal(5, completed.ResponseTokenCount);
        Assert.Equal(2_500_000_000, completed.TotalDurationNanoseconds);
        Assert.Equal(100_000_000, completed.LoadDurationNanoseconds);
        Assert.Equal(400_000_000, completed.PromptEvalDurationNanoseconds);
        Assert.Equal(2_000_000_000, completed.EvalDurationNanoseconds);
    }

    private sealed class StaticResponseHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/x-ndjson")
            });
    }
}
