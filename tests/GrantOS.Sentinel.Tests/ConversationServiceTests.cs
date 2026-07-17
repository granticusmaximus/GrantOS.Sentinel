using GrantOS.Sentinel.Application.Models;
using GrantOS.Sentinel.Domain.Enums;
using GrantOS.Sentinel.Infrastructure.Services;
using Xunit;

namespace GrantOS.Sentinel.Tests;

public sealed class ConversationServiceTests
{
    [Fact]
    public async Task AddMessage_persists_generation_metrics_and_list_includes_messages()
    {
        using var factory = new TestDbContextFactory();
        var service = new ConversationService(factory);
        var conversation = await service.CreateAsync("Metrics", "qwen3", null, ProjectScope.Work);
        var metrics = new MessageGenerationMetrics(
            PromptTokenCount: 120,
            ResponseTokenCount: 30,
            TotalDurationNanoseconds: 4_000_000_000,
            LoadDurationNanoseconds: 100_000_000,
            PromptEvalDurationNanoseconds: 900_000_000,
            EvalDurationNanoseconds: 3_000_000_000);

        await service.AddMessageAsync(conversation.Id, ChatRole.Assistant, "Done", metrics);

        var loaded = await service.GetAsync(conversation.Id);
        var message = Assert.Single(loaded!.Messages);
        Assert.Equal(120, message.PromptTokenCount);
        Assert.Equal(30, message.TokenCount);
        Assert.Equal(4_000_000_000, message.TotalDurationNanoseconds);
        Assert.Equal(100_000_000, message.LoadDurationNanoseconds);
        Assert.Equal(900_000_000, message.PromptEvalDurationNanoseconds);
        Assert.Equal(3_000_000_000, message.EvalDurationNanoseconds);

        var summary = Assert.Single(await service.ListAsync());
        Assert.Single(summary.Messages);
    }
}
