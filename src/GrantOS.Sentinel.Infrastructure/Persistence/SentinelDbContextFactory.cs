using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GrantOS.Sentinel.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add` build a <see cref="SentinelDbContext"/> directly, without
/// going through Web's Program.cs. Program.cs now calls UseElectron unconditionally, whose
/// static initializer throws when invoked outside a real Electron launch - exactly what `dotnet
/// ef`'s design-time host discovery does. Migrations don't need the real runtime connection
/// string, just a valid SQLite provider to compare the model against.
/// </summary>
public sealed class SentinelDbContextFactory : IDesignTimeDbContextFactory<SentinelDbContext>
{
    public SentinelDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SentinelDbContext>()
            .UseSqlite("Data Source=grantos-sentinel.db")
            .Options;
        return new SentinelDbContext(options);
    }
}
