using GrantOS.Sentinel.Infrastructure;
using GrantOS.Sentinel.Infrastructure.Persistence;
using GrantOS.Sentinel.Web.Components;
using GrantOS.Sentinel.Web.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Blazor Web App with interactive server rendering (simplest streaming-capable model for Phase 1).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Everything data/Ollama related lives behind this single call.
builder.Services.AddSentinelInfrastructure(builder.Configuration);

var app = builder.Build();

// Create/upgrade the local SQLite database on startup.
// This applies EF migrations, so run `dotnet ef migrations add InitialCreate` once first
// (see README). If you prefer to skip migrations for now, swap MigrateAsync() for
// EnsureCreatedAsync() below.
await using (var scope = app.Services.CreateAsyncScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SentinelDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Localhost-only HTTP surface the future VS Code extension will call.
app.MapSentinelApi();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
