using GrantOS.Sentinel.Application.Options;
using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Domain.Enums;
using GrantOS.Sentinel.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace GrantOS.Sentinel.Tests;

public sealed class ProjectStandardServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ProjectStandardService _service;

    public ProjectStandardServiceTests()
    {
        _service = new ProjectStandardService(
            _factory,
            Options.Create(new StandardsOptions
            {
                MaxStandards = 5,
                MaxContextCharacters = 2_000,
                MaxStandardContentCharacters = 500
            }));
    }

    [Fact]
    public async Task Crud_normalizes_fields_and_preserves_created_timestamp()
    {
        var created = await _service.CreateAsync(new ProjectStandard
        {
            Name = "  API errors  ",
            Category = " ",
            Content = "  Return ProblemDetails.  ",
            Priority = 150,
            Scope = ProjectScope.Work
        });
        var createdAt = created.CreatedAt;

        Assert.Equal("API errors", created.Name);
        Assert.Equal("General", created.Category);
        Assert.Equal("Return ProblemDetails.", created.Content);
        Assert.Equal(100, created.Priority);

        created.Content = "Return typed ProblemDetails.";
        await _service.UpdateAsync(created);
        var updated = Assert.Single(await _service.ListAsync(ProjectScope.Work));
        Assert.Equal(createdAt, updated.CreatedAt);
        Assert.Equal("Return typed ProblemDetails.", updated.Content);

        await _service.DeleteAsync(created.Id);
        Assert.Empty(await _service.ListAsync(ProjectScope.Work));
    }

    [Fact]
    public async Task Context_contains_enabled_same_scope_standards_in_priority_order()
    {
        await _service.CreateAsync(Standard("Lower priority", "Use integration tests.", 20, ProjectScope.Work));
        await _service.CreateAsync(Standard("Critical security", "Never commit secrets.", 90, ProjectScope.Work));
        await _service.CreateAsync(Standard("Disabled rule", "Do not include this.", 100, ProjectScope.Work, enabled: false));
        await _service.CreateAsync(Standard("Personal rule", "Personal only.", 100, ProjectScope.Personal));

        var result = await _service.GetContextAsync(ProjectScope.Work);

        Assert.Equal(2, result.Standards.Count);
        Assert.Equal("Critical security", result.Standards[0].Name);
        Assert.Contains("USER-AUTHORED PROJECT STANDARDS", result.PromptText);
        Assert.True(result.PromptText.IndexOf("Critical security", StringComparison.Ordinal) < result.PromptText.IndexOf("Lower priority", StringComparison.Ordinal));
        Assert.DoesNotContain("Disabled rule", result.PromptText);
        Assert.DoesNotContain("Personal rule", result.PromptText);
    }

    [Fact]
    public async Task Context_is_empty_when_feature_is_disabled()
    {
        var disabledService = new ProjectStandardService(
            _factory,
            Options.Create(new StandardsOptions { Enabled = false }));
        await _service.CreateAsync(Standard("Rule", "Do the thing.", 50, ProjectScope.Work));

        var result = await disabledService.GetContextAsync(ProjectScope.Work);

        Assert.Empty(result.Standards);
        Assert.Empty(result.PromptText);
    }

    [Fact]
    public async Task Context_reports_only_standards_that_fit_the_budget()
    {
        await _service.CreateAsync(Standard("Highest", new string('A', 200), 90, ProjectScope.Work));
        await _service.CreateAsync(Standard("Lower", new string('B', 200), 10, ProjectScope.Work));
        var limitedService = new ProjectStandardService(
            _factory,
            Options.Create(new StandardsOptions
            {
                MaxStandards = 5,
                MaxContextCharacters = 500,
                MaxStandardContentCharacters = 100
            }));

        var result = await limitedService.GetContextAsync(ProjectScope.Work);

        var included = Assert.Single(result.Standards);
        Assert.Equal("Highest", included.Name);
        Assert.DoesNotContain("Lower", result.PromptText);
    }

    private static ProjectStandard Standard(
        string name,
        string content,
        int priority,
        ProjectScope scope,
        bool enabled = true) =>
        new()
        {
            Name = name,
            Content = content,
            Priority = priority,
            Scope = scope,
            Enabled = enabled
        };

    public void Dispose() => _factory.Dispose();
}
