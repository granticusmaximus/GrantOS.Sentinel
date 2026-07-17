using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GrantOS.Sentinel.Infrastructure.Persistence;

/// <summary>Applies local SQLite durability settings and creates bounded pre-migration backups.</summary>
public static class LocalDatabaseStartup
{
    public static async Task PrepareAndMigrateAsync(SentinelDbContext db, CancellationToken ct = default)
    {
        var pending = await db.Database.GetPendingMigrationsAsync(ct);
        if (pending.Any())
            BackupDatabase(db.Database.GetConnectionString());

        await db.Database.MigrateAsync(ct);
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;", ct);
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", ct);
    }

    private static void BackupDatabase(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource == ":memory:")
            return;
        var fullPath = Path.GetFullPath(dataSource);
        if (!File.Exists(fullPath))
            return;

        var backupPath = $"{fullPath}.{DateTime.UtcNow:yyyyMMddHHmmss}.bak";
        using (var source = new SqliteConnection(connectionString))
        using (var destination = new SqliteConnection($"Data Source={backupPath}"))
        {
            source.Open();
            destination.Open();
            source.BackupDatabase(destination);
        }
        foreach (var stale in Directory.GetFiles(Path.GetDirectoryName(fullPath)!, $"{Path.GetFileName(fullPath)}.*.bak")
                     .OrderByDescending(File.GetCreationTimeUtc)
                     .Skip(3))
            File.Delete(stale);
    }
}
