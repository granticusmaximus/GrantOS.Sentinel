using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Domain.Enums;
using GrantOS.Sentinel.Infrastructure.Services;
using Xunit;

namespace GrantOS.Sentinel.Tests;

public sealed class MemoryServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly MemoryService _service;

    public MemoryServiceTests() => _service = new MemoryService(_factory);

    [Fact]
    public async Task CreateAsync_then_ListAsync_returns_the_entry()
    {
        await _service.CreateAsync(new MemoryEntry { Title = "Clean Architecture", Content = "Dependencies point inward." });

        var all = await _service.ListAsync();

        Assert.Contains(all, m => m.Title == "Clean Architecture");
    }

    [Fact]
    public async Task ListAsync_search_matches_title_content_tags_and_category()
    {
        await _service.CreateAsync(new MemoryEntry { Title = "SOLID", Content = "Five principles", Tags = "design", Category = "Standards" });
        await _service.CreateAsync(new MemoryEntry { Title = "Docker note", Content = "compose up", Tags = "infra", Category = "Ops" });

        Assert.Single(await _service.ListAsync("SOLID"));     // title
        Assert.Single(await _service.ListAsync("compose"));   // content
        Assert.Single(await _service.ListAsync("infra"));     // tags
        Assert.Single(await _service.ListAsync("Standards")); // category
        Assert.Equal(2, (await _service.ListAsync()).Count);  // no filter
    }

    [Fact]
    public async Task UpdateAsync_changes_fields_but_preserves_CreatedAt()
    {
        var created = await _service.CreateAsync(new MemoryEntry { Title = "Draft", Content = "old" });
        var originalCreatedAt = created.CreatedAt;

        created.Title = "Final";
        created.Content = "new";
        await _service.UpdateAsync(created);

        var reloaded = await _service.GetAsync(created.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("Final", reloaded!.Title);
        Assert.Equal("new", reloaded.Content);
        Assert.Equal(originalCreatedAt, reloaded.CreatedAt);
    }

    [Fact]
    public async Task DeleteAsync_removes_the_entry()
    {
        var created = await _service.CreateAsync(new MemoryEntry { Title = "Temp" });

        await _service.DeleteAsync(created.Id);

        Assert.Null(await _service.GetAsync(created.Id));
    }

    [Fact]
    public async Task ListAsync_orders_pinned_entries_first()
    {
        await _service.CreateAsync(new MemoryEntry { Title = "Unpinned", Pinned = false });
        await _service.CreateAsync(new MemoryEntry { Title = "Pinned", Pinned = true });

        var all = await _service.ListAsync();

        Assert.True(all[0].Pinned, "The first entry should be the pinned one.");
    }

    public void Dispose() => _factory.Dispose();
}
