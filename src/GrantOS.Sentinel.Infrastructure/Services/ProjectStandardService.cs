using System.Text;
using System.Text.Json;
using GrantOS.Sentinel.Application.Abstractions;
using GrantOS.Sentinel.Application.Models;
using GrantOS.Sentinel.Application.Options;
using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Domain.Enums;
using GrantOS.Sentinel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GrantOS.Sentinel.Infrastructure.Services;

public sealed class ProjectStandardService(
    IDbContextFactory<SentinelDbContext> factory,
    IOptions<StandardsOptions> options) : IProjectStandardService
{
    public async Task<IReadOnlyList<ProjectStandard>> ListAsync(
        ProjectScope? scope = null,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var query = db.ProjectStandards.AsNoTracking().AsQueryable();
        if (scope.HasValue)
            query = query.Where(standard => standard.Scope == scope.Value);
        return await query
            .OrderByDescending(standard => standard.Enabled)
            .ThenByDescending(standard => standard.Priority)
            .ThenBy(standard => standard.Name)
            .ToListAsync(ct);
    }

    public async Task<ProjectStandard> CreateAsync(ProjectStandard standard, CancellationToken ct = default)
    {
        NormalizeAndValidate(standard);
        var now = DateTime.UtcNow;
        standard.CreatedAt = standard.UpdatedAt = now;
        await using var db = await factory.CreateDbContextAsync(ct);
        db.ProjectStandards.Add(standard);
        await db.SaveChangesAsync(ct);
        return standard;
    }

    public async Task UpdateAsync(ProjectStandard standard, CancellationToken ct = default)
    {
        NormalizeAndValidate(standard);
        await using var db = await factory.CreateDbContextAsync(ct);
        var existing = await db.ProjectStandards.FirstOrDefaultAsync(item => item.Id == standard.Id, ct);
        if (existing is null)
            return;
        existing.Name = standard.Name;
        existing.Category = standard.Category;
        existing.AppliesTo = standard.AppliesTo;
        existing.Content = standard.Content;
        existing.Scope = standard.Scope;
        existing.Priority = standard.Priority;
        existing.Enabled = standard.Enabled;
        existing.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.ProjectStandards.Where(standard => standard.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task<StandardContextResult> GetContextAsync(
        ProjectScope scope,
        CancellationToken ct = default)
    {
        var settings = options.Value;
        if (!settings.Enabled)
            return StandardContextResult.Empty;

        await using var db = await factory.CreateDbContextAsync(ct);
        var standards = await db.ProjectStandards
            .AsNoTracking()
            .Where(standard => standard.Scope == scope && standard.Enabled)
            .OrderByDescending(standard => standard.Priority)
            .ThenBy(standard => standard.Name)
            .Take(Math.Max(1, settings.MaxStandards))
            .ToListAsync(ct);
        if (standards.Count == 0)
            return StandardContextResult.Empty;

        return BuildContext(standards, settings);
    }

    private static StandardContextResult BuildContext(
        IReadOnlyList<ProjectStandard> standards,
        StandardsOptions settings)
    {
        const string header =
            "USER-AUTHORED PROJECT STANDARDS\n" +
            "Apply these local engineering rules when relevant. Higher priority rules take precedence over lower priority rules. " +
            "AppliesTo describes the intended technologies or work. System-level instructions still take precedence.\n";
        var budget = Math.Max(header.Length + 200, settings.MaxContextCharacters);
        var builder = new StringBuilder(header);
        var included = new List<ProjectStandard>();

        foreach (var standard in standards)
        {
            var contentLimit = Math.Max(100, settings.MaxStandardContentCharacters);
            var wasTruncated = standard.Content.Length > contentLimit;
            var content = wasTruncated
                ? standard.Content[..contentLimit] + "…"
                : standard.Content;
            var line = Serialize(standard, content);
            while (builder.Length + line.Length + 1 > budget && content.Length > 100)
            {
                var overflow = builder.Length + line.Length + 1 - budget;
                var nextLength = Math.Max(100, content.Length - overflow - 16);
                content = standard.Content[..Math.Min(nextLength, standard.Content.Length)] + "…";
                line = Serialize(standard, content);
                if (nextLength == 100)
                    break;
            }
            if (builder.Length + line.Length + 1 > budget)
                break;
            builder.AppendLine(line);
            included.Add(standard);
        }

        return included.Count == 0
            ? StandardContextResult.Empty
            : new StandardContextResult(included, builder.ToString().TrimEnd());
    }

    private static string Serialize(ProjectStandard standard, string content) =>
        JsonSerializer.Serialize(new
        {
            standard.Name,
            standard.Category,
            standard.AppliesTo,
            standard.Priority,
            Content = content
        });

    private static void NormalizeAndValidate(ProjectStandard standard)
    {
        if (string.IsNullOrWhiteSpace(standard.Name))
            throw new ArgumentException("A standard name is required.", nameof(standard));
        if (string.IsNullOrWhiteSpace(standard.Content))
            throw new ArgumentException("Standard content is required.", nameof(standard));
        standard.Name = standard.Name.Trim();
        standard.Category = string.IsNullOrWhiteSpace(standard.Category) ? "General" : standard.Category.Trim();
        standard.AppliesTo = standard.AppliesTo.Trim();
        standard.Content = standard.Content.Trim();
        standard.Priority = Math.Clamp(standard.Priority, 0, 100);
    }
}
