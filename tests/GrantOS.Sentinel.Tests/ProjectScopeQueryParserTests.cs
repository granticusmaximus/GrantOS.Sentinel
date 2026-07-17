using GrantOS.Sentinel.Application.Models;
using GrantOS.Sentinel.Domain.Enums;
using Xunit;

namespace GrantOS.Sentinel.Tests;

public sealed class ProjectScopeQueryParserTests
{
    [Theory]
    [InlineData("Personal", ProjectScope.Personal)]
    [InlineData("work", ProjectScope.Work)]
    [InlineData("WORK", ProjectScope.Work)]
    public void ParseOrDefault_accepts_defined_scope_names(string value, ProjectScope expected)
    {
        var result = ProjectScopeQueryParser.ParseOrDefault(value, ProjectScope.Personal);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-scope")]
    [InlineData("999")]
    public void ParseOrDefault_uses_fallback_for_missing_invalid_or_undefined_values(string? value)
    {
        var result = ProjectScopeQueryParser.ParseOrDefault(value, ProjectScope.Work);

        Assert.Equal(ProjectScope.Work, result);
    }
}
