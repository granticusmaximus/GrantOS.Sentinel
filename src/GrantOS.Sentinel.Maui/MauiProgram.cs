using GrantOS.Sentinel.Infrastructure;
using GrantOS.Sentinel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GrantOS.Sentinel.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { });

        builder.Services.AddMauiBlazorWebView();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // The "Data Source=grantos-sentinel.db" relative fallback in Infrastructure's
        // DependencyInjection.cs resolves against the process working directory, which is
        // unpredictable/read-only inside a packaged Mac Catalyst app's sandbox container.
        // Use an absolute path under the app's own sandboxed data directory instead.
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "grantos-sentinel.db");

        // MAUI has no automatic appsettings.json binding. In-memory config mirrors the
        // Web project's appsettings.json Ollama/Sentinel/ConnectionStrings keys.
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Sentinel"] = $"Data Source={dbPath}",
            ["Ollama:BaseUrl"] = "http://localhost:11434",
            ["Ollama:DefaultModel"] = "qwen2.5-coder",
            ["Ollama:TimeoutSeconds"] = "300",
            ["Sentinel:DefaultScope"] = "Personal",
        });

        // Same in-process DI wiring the Web project uses - no HTTP hop into a localhost API.
        builder.Services.AddSentinelInfrastructure(builder.Configuration);

        var app = builder.Build();

        // MauiApp.CreateBuilder().Build() is synchronous, unlike Web's async top-level Main,
        // so migrate with the blocking overload.
        using (var scope = app.Services.CreateScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SentinelDbContext>>();
            using var db = factory.CreateDbContext();
            db.Database.Migrate();
        }

        return app;
    }
}
