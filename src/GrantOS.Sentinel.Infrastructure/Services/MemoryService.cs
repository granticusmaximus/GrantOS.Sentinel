using GrantOS.Sentinel.Application.Abstractions;
using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GrantOS.Sentinel.Infrastructure.Services;

public sealed class MemoryService(IDbContextFactory<SentinelDbContext> factory) : IMemoryService
{
    public async Task<IReadOnlyList<MemoryEntry>> ListAsync(string? search = null, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var query = db.MemoryEntries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(m =>
                EF.Functions.Like(m.Title, $"%{term}%") ||
                EF.Functions.Like(m.Content, $"%{term}%") ||
                EF.Functions.Like(m.Tags, $"%{term}%") ||
                EF.Functions.Like(m.Category, $"%{term}%"));
        }

        return await query
            .OrderByDescending(m => m.Pinned)
            .ThenByDescending(m => m.UpdatedAt)
            .ToListAsync(ct);
    }

    public async Task<MemoryEntry?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.MemoryEntries.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<MemoryEntry> CreateAsync(MemoryEntry entry, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        entry.CreatedAt = entry.UpdatedAt = DateTime.UtcNow;
        db.MemoryEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task UpdateAsync(MemoryEntry entry, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var existing = await db.MemoryEntries.FirstOrDefaultAsync(m => m.Id == entry.Id, ct);
        if (existing is null) return;
        existing.Title = entry.Title;
        existing.Content = entry.Content;
        existing.Category = entry.Category;
        existing.Tags = entry.Tags;
        existing.Pinned = entry.Pinned;
        existing.Scope = entry.Scope;
        existing.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.MemoryEntries.Where(m => m.Id == id).ExecuteDeleteAsync(ct);
    }
}
