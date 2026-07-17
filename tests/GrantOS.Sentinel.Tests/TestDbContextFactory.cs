using GrantOS.Sentinel.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GrantOS.Sentinel.Tests;

/// <summary>
/// An <see cref="IDbContextFactory{TContext}"/> backed by a single in-memory SQLite
/// connection. The connection is opened once and shared by every context the factory
/// hands out, which is what keeps the in-memory database alive across contexts
/// (a fresh connection per context would each get its own empty database).
/// </summary>
public sealed class TestDbContextFactory : IDbContextFactory<SentinelDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<SentinelDbContext> _options;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<SentinelDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new SentinelDbContext(_options);
        ctx.Database.EnsureCreated();
        KnowledgeFtsSchema.EnsureCreated(ctx);
    }

    public SentinelDbContext CreateDbContext() => new(_options);

    public Task<SentinelDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(CreateDbContext());

    public void Dispose() => _connection.Dispose();
}
