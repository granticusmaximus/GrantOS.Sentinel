using GrantOS.Sentinel.Application.Abstractions;
using GrantOS.Sentinel.Domain.Entities;
using GrantOS.Sentinel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GrantOS.Sentinel.Infrastructure.Services;

public sealed class ModelProfileService(IDbContextFactory<SentinelDbContext> factory) : IModelProfileService
{
    public async Task<IReadOnlyList<ModelProfile>> ListAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.ModelProfiles.AsNoTracking()
            .OrderByDescending(p => p.IsDefault).ThenBy(p => p.DisplayName)
            .ToListAsync(ct);
    }

    public async Task<ModelProfile?> GetDefaultAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.ModelProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.IsDefault, ct);
    }

    public async Task<ModelProfile> CreateAsync(ModelProfile profile, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        profile.CreatedAt = DateTime.UtcNow;
        db.ModelProfiles.Add(profile);
        await db.SaveChangesAsync(ct);
        return profile;
    }

    public async Task UpdateAsync(ModelProfile profile, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var existing = await db.ModelProfiles.FirstOrDefaultAsync(p => p.Id == profile.Id, ct);
        if (existing is null) return;
        existing.Name = profile.Name;
        existing.DisplayName = profile.DisplayName;
        existing.Description = profile.Description;
        existing.ContextLength = profile.ContextLength;
        existing.Temperature = profile.Temperature;
        await db.SaveChangesAsync(ct);
    }

    public async Task SetDefaultAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.ModelProfiles.Where(p => p.IsDefault).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDefault, false), ct);
        await db.ModelProfiles.Where(p => p.Id == id).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDefault, true), ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.ModelProfiles.Where(p => p.Id == id && !p.IsDefault).ExecuteDeleteAsync(ct);
    }
}
