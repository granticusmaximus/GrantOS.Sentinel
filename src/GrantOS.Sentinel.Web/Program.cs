using ElectronNET.API;
using ElectronNET.API.Entities;
using GrantOS.Sentinel.Infrastructure;
using GrantOS.Sentinel.Infrastructure.Persistence;
using GrantOS.Sentinel.UI;
using GrantOS.Sentinel.Web;
using GrantOS.Sentinel.Web.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using AppComponent = GrantOS.Sentinel.Web.Components.App;

if (args is ["--install-playwright-browsers"])
{
    Environment.ExitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Blazor Web App with interactive server rendering (simplest streaming-capable model for Phase 1).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Everything data/Ollama related lives behind this single call.
builder.Services.AddSentinelInfrastructure(builder.Configuration);
builder.Services.AddSingleton<LocalApiAccessToken>();
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("sentinel-api", limiter =>
{
    limiter.PermitLimit = 120;
    limiter.Window = TimeSpan.FromMinutes(1);
    limiter.QueueLimit = 0;
    limiter.AutoReplenishment = true;
}));
var electronWindow = new ElectronWindowController();
builder.Services.AddSingleton(electronWindow);

// Wraps this app in a native Electron window. Calling UseElectron unconditionally commits
// the whole process to Electron's hosting model (see WebHostBuilderExtensions.UseElectron):
// plain http:// on a dynamic loopback port, always - there's no separate "just a browser on
// a fixed port" mode once this is wired in.
builder.UseElectron(args, async () =>
{
    var window = await Electron.WindowManager.CreateWindowAsync(
        new BrowserWindowOptions { Show = false, Title = "GrantOS Sentinel" });
    electronWindow.SetWindow(window);
    window.OnReadyToShow += () => window.Show();
});

var app = builder.Build();

// Create/upgrade the local SQLite database on startup.
// This applies EF migrations, so run `dotnet ef migrations add InitialCreate` once first
// (see README). If you prefer to skip migrations for now, swap MigrateAsync() for
// EnsureCreatedAsync() below.
await using (var scope = app.Services.CreateAsyncScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SentinelDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await LocalDatabaseStartup.PrepareAndMigrateAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// No HSTS/HTTPS-redirect: this app only ever binds to plain http:// on a dynamic loopback
// port under Electron's hosting model (see the UseElectron comment above) - there's no
// HTTPS binding to redirect to.
app.UseStaticFiles();
app.UseAntiforgery();
app.UseRateLimiter();

// Localhost-only HTTP surface the future VS Code extension will call.
app.MapSentinelApi();

// Pages/Layout live in GrantOS.Sentinel.UI (shared with the Maui app), so the server-side
// endpoint discovery that builds the route table needs to be told to scan that assembly too.
app.MapRazorComponents<AppComponent>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

await app.StartAsync();

var server = app.Services.GetRequiredService<IServer>();
var address = server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();
if (address is not null)
{
    electronWindow.SetBaseAddress(address);
    var accessToken = app.Services.GetRequiredService<LocalApiAccessToken>();
    await RuntimeStateFile.WriteAsync(address, accessToken.Value);
}

try
{
    await app.WaitForShutdownAsync();
}
finally
{
    RuntimeStateFile.Delete();
}
