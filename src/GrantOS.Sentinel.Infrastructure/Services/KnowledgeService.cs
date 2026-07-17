using System.Linq.Expressions;
using System.Text.RegularExpressions;
using GrantOS.Sentinel.Application.Abstractions;
using GrantOS.Sentinel.Application.Models;
using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GrantOS.Sentinel.Infrastructure.Services;

/// <summary>Unified, read-only search over durable Memory notes and indexed Project files.</summary>
public sealed partial class KnowledgeService(IDbContextFactory<SentinelDbContext> factory) : IKnowledgeService
{
    private const int CandidateLimit = 500;
    private const int PreviewCharacters = 1_500;

    public async Task<KnowledgeSearchResult> SearchAsync(
        KnowledgeSearchRequest request,
        CancellationToken ct = default)
    {
        var terms = Tokenize(request.Query).Take(8).ToArray();
        var maxResults = Math.Clamp(request.MaxResults, 1, 200);
        await using var db = await factory.CreateDbContextAsync(ct);

        var memoryItems = new List<KnowledgeItem>();
        var projectItems = new List<KnowledgeItem>();
        var standardItems = new List<KnowledgeItem>();
        var memoryMatches = 0;
        var projectMatches = 0;
        var standardMatches = 0;

        if (request.Source is KnowledgeSourceKind.All or KnowledgeSourceKind.Memory)
        {
            var query = terms.Length == 0
                ? db.MemoryEntries.AsNoTracking()
                : db.MemoryEntries.FromSqlInterpolated($"SELECT m.* FROM MemoryEntries m JOIN MemoryEntriesFts f ON f.rowid = m.Id WHERE MemoryEntriesFts MATCH {BuildFtsQuery(terms)}").AsNoTracking();
            query = query.Where(memory => memory.Scope == request.Scope);

            memoryMatches = await query.CountAsync(ct);
            var candidates = await query
                .OrderByDescending(memory => memory.Pinned)
                .ThenByDescending(memory => memory.UpdatedAt)
                .Take(CandidateLimit)
                .ToListAsync(ct);
            memoryItems.AddRange(candidates.Select(memory => ToKnowledgeItem(memory, terms)));
        }

        if (request.Source is KnowledgeSourceKind.All or KnowledgeSourceKind.Project)
        {
            var query = (terms.Length == 0
                    ? db.ProjectDocuments.AsNoTracking()
                    : db.ProjectDocuments.FromSqlInterpolated($"SELECT d.* FROM ProjectDocuments d JOIN ProjectDocumentsFts f ON f.rowid = d.Id WHERE ProjectDocumentsFts MATCH {BuildFtsQuery(terms)}").AsNoTracking())
                .Include(document => document.ProjectWorkspace)
                .Where(document => document.ProjectWorkspace!.Scope == request.Scope);

            projectMatches = await query.CountAsync(ct);
            var candidates = await query
                .OrderByDescending(document => document.IndexedAt)
                .Take(CandidateLimit)
                .ToListAsync(ct);
            projectItems.AddRange(candidates.Select(document => ToKnowledgeItem(document, terms)));
        }

        if (request.Source is KnowledgeSourceKind.All or KnowledgeSourceKind.Standard)
        {
            var query = terms.Length == 0
                ? db.ProjectStandards.AsNoTracking()
                : db.ProjectStandards.FromSqlInterpolated($"SELECT s.* FROM ProjectStandards s JOIN ProjectStandardsFts f ON f.rowid = s.Id WHERE ProjectStandardsFts MATCH {BuildFtsQuery(terms)}").AsNoTracking();
            query = query.Where(standard => standard.Scope == request.Scope);

            standardMatches = await query.CountAsync(ct);
            var candidates = await query
                .OrderByDescending(standard => standard.Enabled)
                .ThenByDescending(standard => standard.Priority)
                .ThenByDescending(standard => standard.UpdatedAt)
                .Take(CandidateLimit)
                .ToListAsync(ct);
            standardItems.AddRange(candidates.Select(standard => ToKnowledgeItem(standard, terms)));
        }

        var selected = memoryItems
            .Concat(projectItems)
            .Concat(standardItems)
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Pinned)
            .ThenByDescending(item => item.Active)
            .ThenByDescending(item => item.UpdatedAt)
            .ThenBy(item => item.Title)
            .Take(maxResults)
            .ToList();

        return new KnowledgeSearchResult(
            selected,
            memoryMatches + projectMatches + standardMatches,
            memoryMatches,
            projectMatches,
            standardMatches);
    }

    private static KnowledgeItem ToKnowledgeItem(MemoryEntry memory, IReadOnlyList<string> terms) =>
        new(
            KnowledgeSourceKind.Memory,
            memory.Id,
            memory.Title,
            string.IsNullOrWhiteSpace(memory.Category) ? "Memory" : memory.Category,
            memory.Tags,
            CreatePreview(memory.Content, terms),
            memory.Scope,
            memory.UpdatedAt,
            memory.Pinned,
            true,
            ScoreMemory(memory, terms));

    private static string BuildFtsQuery(IReadOnlyList<string> terms) =>
        string.Join(" OR ", terms.Select(term => $"\"{term.Replace("\"", "\"\"")}\"*"));

    private static KnowledgeItem ToKnowledgeItem(ProjectDocument document, IReadOnlyList<string> terms) =>
        new(
            KnowledgeSourceKind.Project,
            document.Id,
            Path.GetFileName(document.RelativePath),
            document.ProjectWorkspace!.Name,
            document.RelativePath,
            CreatePreview(document.Content, terms),
            document.ProjectWorkspace.Scope,
            document.IndexedAt,
            false,
            true,
            ScoreProject(document, terms));

    private static KnowledgeItem ToKnowledgeItem(ProjectStandard standard, IReadOnlyList<string> terms) =>
        new(
            KnowledgeSourceKind.Standard,
            standard.Id,
            standard.Name,
            standard.Category,
            standard.AppliesTo,
            CreatePreview(standard.Content, terms),
            standard.Scope,
            standard.UpdatedAt,
            false,
            standard.Enabled,
            ScoreStandard(standard, terms));

    private static int ScoreMemory(MemoryEntry memory, IReadOnlyList<string> terms)
    {
        var score = memory.Pinned ? 2 : 0;
        var title = memory.Title.ToLowerInvariant();
        var category = memory.Category.ToLowerInvariant();
        var tags = memory.Tags.ToLowerInvariant();
        var content = memory.Content.ToLowerInvariant();
        foreach (var term in terms)
        {
            if (title.Contains(term, StringComparison.Ordinal)) score += 8;
            if (tags.Contains(term, StringComparison.Ordinal)) score += 5;
            if (category.Contains(term, StringComparison.Ordinal)) score += 4;
            if (content.Contains(term, StringComparison.Ordinal)) score += 1;
        }
        return score;
    }

    private static int ScoreProject(ProjectDocument document, IReadOnlyList<string> terms)
    {
        var score = 0;
        var path = document.RelativePath.ToLowerInvariant();
        var workspace = document.ProjectWorkspace!.Name.ToLowerInvariant();
        var content = document.Content.ToLowerInvariant();
        foreach (var term in terms)
        {
            if (path.Contains(term, StringComparison.Ordinal)) score += 6;
            if (workspace.Contains(term, StringComparison.Ordinal)) score += 4;
            if (content.Contains(term, StringComparison.Ordinal)) score += 1;
        }
        return score;
    }

    private static int ScoreStandard(ProjectStandard standard, IReadOnlyList<string> terms)
    {
        var score = standard.Enabled ? 2 : 0;
        var name = standard.Name.ToLowerInvariant();
        var category = standard.Category.ToLowerInvariant();
        var appliesTo = standard.AppliesTo.ToLowerInvariant();
        var content = standard.Content.ToLowerInvariant();
        foreach (var term in terms)
        {
            if (name.Contains(term, StringComparison.Ordinal)) score += 8;
            if (appliesTo.Contains(term, StringComparison.Ordinal)) score += 5;
            if (category.Contains(term, StringComparison.Ordinal)) score += 4;
            if (content.Contains(term, StringComparison.Ordinal)) score += 1;
        }
        return score;
    }

    private static string CreatePreview(string content, IReadOnlyList<string> terms)
    {
        if (content.Length <= PreviewCharacters)
            return content;

        var firstMatch = terms
            .Select(term => content.IndexOf(term, StringComparison.OrdinalIgnoreCase))
            .Where(index => index >= 0)
            .DefaultIfEmpty(0)
            .Min();
        var start = Math.Max(0, firstMatch - PreviewCharacters / 4);
        if (start + PreviewCharacters > content.Length)
            start = content.Length - PreviewCharacters;
        return $"{(start > 0 ? "…" : string.Empty)}{content.Substring(start, PreviewCharacters)}{(start + PreviewCharacters < content.Length ? "…" : string.Empty)}";
    }

    private static Expression<Func<MemoryEntry, bool>> BuildMemoryPredicate(IReadOnlyList<string> terms)
    {
        var memory = Expression.Parameter(typeof(MemoryEntry), "memory");
        return Expression.Lambda<Func<MemoryEntry, bool>>(
            BuildContainsAny(memory, terms, nameof(MemoryEntry.Title), nameof(MemoryEntry.Category), nameof(MemoryEntry.Tags), nameof(MemoryEntry.Content)),
            memory);
    }

    private static Expression<Func<ProjectDocument, bool>> BuildProjectPredicate(IReadOnlyList<string> terms)
    {
        var document = Expression.Parameter(typeof(ProjectDocument), "document");
        var workspace = Expression.Property(document, nameof(ProjectDocument.ProjectWorkspace));
        var fields = new Expression[]
        {
            Expression.Property(document, nameof(ProjectDocument.RelativePath)),
            Expression.Property(document, nameof(ProjectDocument.Content)),
            Expression.Property(workspace, nameof(ProjectWorkspace.Name))
        };
        return Expression.Lambda<Func<ProjectDocument, bool>>(BuildContainsAny(fields, terms), document);
    }

    private static Expression<Func<ProjectStandard, bool>> BuildStandardPredicate(IReadOnlyList<string> terms)
    {
        var standard = Expression.Parameter(typeof(ProjectStandard), "standard");
        return Expression.Lambda<Func<ProjectStandard, bool>>(
            BuildContainsAny(
                standard,
                terms,
                nameof(ProjectStandard.Name),
                nameof(ProjectStandard.Category),
                nameof(ProjectStandard.AppliesTo),
                nameof(ProjectStandard.Content)),
            standard);
    }

    private static Expression BuildContainsAny(
        ParameterExpression parameter,
        IReadOnlyList<string> terms,
        params string[] propertyNames) =>
        BuildContainsAny(propertyNames.Select(name => (Expression)Expression.Property(parameter, name)), terms);

    private static Expression BuildContainsAny(IEnumerable<Expression> fields, IReadOnlyList<string> terms)
    {
        Expression? body = null;
        foreach (var field in fields)
        {
            var loweredField = Expression.Call(field, nameof(string.ToLower), Type.EmptyTypes);
            foreach (var term in terms)
            {
                var contains = Expression.Call(
                    loweredField,
                    nameof(string.Contains),
                    Type.EmptyTypes,
                    Expression.Constant(term));
                body = body is null ? contains : Expression.OrElse(body, contains);
            }
        }
        return body ?? Expression.Constant(true);
    }

    private static IEnumerable<string> Tokenize(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];
        return WordPattern().Matches(query)
            .Cast<Match>()
            .Select(match => match.Value.ToLowerInvariant())
            .Where(term => term.Length >= 2 && !StopWords.Contains(term))
            .Distinct(StringComparer.Ordinal);
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "about", "after", "also", "and", "are", "can", "could", "for", "from", "have",
        "how", "into", "its", "our", "should", "that", "the", "their", "this", "using",
        "was", "what", "when", "where", "which", "with", "would", "you", "your"
    };

    [GeneratedRegex(@"[\p{L}\p{N}][\p{L}\p{N}_.+#-]*", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}
