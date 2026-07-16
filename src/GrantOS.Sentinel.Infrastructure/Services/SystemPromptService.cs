using GrantOS.Sentinel.Application.Abstractions;
using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GrantOS.Sentinel.Infrastructure.Services;

public sealed class SystemPromptService(IDbContextFactory<SentinelDbContext> factory) : ISystemPromptService
{
    public async Task<IReadOnlyList<SystemPrompt>> ListAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.SystemPrompts.AsNoTracking()
            .OrderByDescending(p => p.IsDefault).ThenBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task<SystemPrompt?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.SystemPrompts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<SystemPrompt?> GetDefaultAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.SystemPrompts.AsNoTracking().FirstOrDefaultAsync(p => p.IsDefault, ct);
    }

    public async Task<SystemPrompt> CreateAsync(SystemPrompt prompt, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        prompt.CreatedAt = prompt.UpdatedAt = DateTime.UtcNow;
        db.SystemPrompts.Add(prompt);
        await db.SaveChangesAsync(ct);
        return prompt;
    }

    public async Task UpdateAsync(SystemPrompt prompt, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var existing = await db.SystemPrompts.FirstOrDefaultAsync(p => p.Id == prompt.Id, ct);
        if (existing is null) return;
        existing.Name = prompt.Name;
        existing.Content = prompt.Content;
        existing.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task SetDefaultAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.SystemPrompts.Where(p => p.IsDefault).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDefault, false), ct);
        await db.SystemPrompts.Where(p => p.Id == id).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDefault, true), ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.SystemPrompts.Where(p => p.Id == id && !p.IsDefault).ExecuteDeleteAsync(ct);
    }
}
