using GrantOS.Sentinel.Application.Abstractions;
using GrantOS.Sentinel.Application.Options;
using GrantOS.Sentinel.Infrastructure.Persistence;
using GrantOS.Sentinel.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GrantOS.Sentinel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSentinelInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Strongly-typed options.
        services.Configure<OllamaClientOptions>(configuration.GetSection(OllamaClientOptions.SectionName));
        services.Configure<SentinelOptions>(configuration.GetSection(SentinelOptions.SectionName));

        // SQLite via a context factory (short-lived contexts, safe for Blazor Server circuits).
        var connectionString = configuration.GetConnectionString("Sentinel")
                               ?? "Data Source=grantos-sentinel.db";
        services.AddDbContextFactory<SentinelDbContext>(options => options.UseSqlite(connectionString));

        // Typed HttpClient for Ollama, configured from options.
        services.AddHttpClient<IOllamaChatService, OllamaChatService>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<OllamaClientOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(30, opts.TimeoutSeconds));
        });

        // Application services.
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IMemoryService, MemoryService>();
        services.AddScoped<ISystemPromptService, SystemPromptService>();
        services.AddScoped<IModelProfileService, ModelProfileService>();
        services.AddScoped<IToolAuditService, ToolAuditService>();

        return services;
    }
}
