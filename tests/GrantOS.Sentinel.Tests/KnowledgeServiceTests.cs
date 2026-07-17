using GrantOS.Sentinel.Application.Models;
using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Domain.Enums;
using GrantOS.Sentinel.Infrastructure.Services;
using Xunit;

namespace GrantOS.Sentinel.Tests;

public sealed class KnowledgeServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly KnowledgeService _service;

    public KnowledgeServiceTests()
    {
        _service = new KnowledgeService(_factory);
        using var db = _factory.CreateDbContext();
        db.MemoryEntries.AddRange(
            new MemoryEntry
            {
                Title = "Runtime state standard",
                Content = "Persist the runtime endpoint atomically.",
                Category = "Standards",
                Tags = "runtime,electron",
                Scope = ProjectScope.Work,
                Pinned = true
            },
            new MemoryEntry
            {
                Title = "Private runtime note",
                Content = "Personal-only runtime details.",
                Scope = ProjectScope.Personal
            });
        db.ProjectWorkspaces.AddRange(
            new ProjectWorkspace
            {
                Name = "Sentinel",
                RootPath = "/work/sentinel",
                Scope = ProjectScope.Work,
                Documents =
                [
                    new ProjectDocument
                    {
                        RelativePath = "src/RuntimeStateFile.cs",
                        Content = "class RuntimeStateFile { string Endpoint; }",
                        ContentHash = "WORK",
                        IndexedAt = DateTime.UtcNow
                    }
                ]
            },
            new ProjectWorkspace
            {
                Name = "Private",
                RootPath = "/personal/private",
                Scope = ProjectScope.Personal,
                Documents =
                [
                    new ProjectDocument
                    {
                        RelativePath = "SecretRuntime.cs",
                        Content = "personal runtime secret",
                        ContentHash = "PERSONAL",
                        IndexedAt = DateTime.UtcNow
                    }
                ]
            });
        db.SaveChanges();
    }

    [Fact]
    public async Task Search_combines_memory_and_projects_without_crossing_scope()
    {
        var result = await _service.SearchAsync(new KnowledgeSearchRequest("runtime endpoint", ProjectScope.Work));

        Assert.Equal(2, result.TotalMatches);
        Assert.Equal(1, result.MemoryMatches);
        Assert.Equal(1, result.ProjectMatches);
        Assert.Contains(result.Items, item => item.Source == KnowledgeSourceKind.Memory && item.Title == "Runtime state standard");
        Assert.Contains(result.Items, item => item.Source == KnowledgeSourceKind.Project && item.Location == "src/RuntimeStateFile.cs");
        Assert.DoesNotContain(result.Items, item => item.Scope == ProjectScope.Personal);
    }

    [Fact]
    public async Task Search_honors_source_filter_and_ranks_title_matches()
    {
        var memoryOnly = await _service.SearchAsync(new KnowledgeSearchRequest(
            "runtime",
            ProjectScope.Work,
            KnowledgeSourceKind.Memory));

        var item = Assert.Single(memoryOnly.Items);
        Assert.Equal(KnowledgeSourceKind.Memory, item.Source);
        Assert.Equal(0, memoryOnly.ProjectMatches);
        Assert.True(item.Score >= 8);
    }

    [Fact]
    public async Task Empty_search_returns_scope_counts_and_respects_result_limit()
    {
        var result = await _service.SearchAsync(new KnowledgeSearchRequest(
            null,
            ProjectScope.Work,
            MaxResults: 1));

        Assert.Equal(2, result.TotalMatches);
        Assert.Single(result.Items);
        Assert.True(result.Items[0].Pinned);
    }

    [Fact]
    public async Task Search_includes_enabled_and_disabled_project_standards()
    {
        using (var db = _factory.CreateDbContext())
        {
            db.ProjectStandards.Add(new ProjectStandard
            {
                Name = "API testing standard",
                Category = "Testing",
                AppliesTo = "ASP.NET APIs",
                Content = "Use integration tests for every API endpoint.",
                Scope = ProjectScope.Work,
                Enabled = false
            });
            await db.SaveChangesAsync();
        }

        var result = await _service.SearchAsync(new KnowledgeSearchRequest(
            "integration API",
            ProjectScope.Work,
            KnowledgeSourceKind.Standard));

        var standard = Assert.Single(result.Items);
        Assert.Equal(KnowledgeSourceKind.Standard, standard.Source);
        Assert.False(standard.Active);
        Assert.Equal(1, result.StandardMatches);
        Assert.Equal(0, result.MemoryMatches);
        Assert.Equal(0, result.ProjectMatches);
    }

    public void Dispose() => _factory.Dispose();
}
