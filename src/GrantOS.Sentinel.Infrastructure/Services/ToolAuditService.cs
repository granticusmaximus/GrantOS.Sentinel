using GrantOS.Sentinel.Application.Abstractions;
using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Domain.Enums;
using GrantOS.Sentinel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GrantOS.Sentinel.Infrastructure.Services;

public sealed class ToolAuditService(IDbContextFactory<SentinelDbContext> factory) : IToolAuditService
{
    public async Task LogAsync(string toolName, string action, string? parameters, string? result, bool success, ProjectScope scope, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        db.ToolAuditLogs.Add(new ToolAuditLog
        {
            ToolName = toolName,
            Action = action,
            Parameters = parameters,
            Result = result,
            Success = success,
            Scope = scope,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ToolAuditLog>> ListAsync(int take = 200, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.ToolAuditLogs.AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
    }
}
