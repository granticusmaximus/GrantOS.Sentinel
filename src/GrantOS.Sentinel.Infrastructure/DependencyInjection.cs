using GrantOS.Sentinel.Application.Abstractions;
using GrantOS.Sentinel.Application.Options;
using GrantOS.Sentinel.Application.Services;
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
        services.AddOptions<OllamaClientOptions>()
            .Bind(configuration.GetSection(OllamaClientOptions.SectionName))
            .Validate(value => Uri.TryCreate(value.BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https", "Ollama:BaseUrl must be an absolute HTTP(S) URL.")
            .Validate(value => !string.IsNullOrWhiteSpace(value.DefaultModel), "Ollama:DefaultModel is required.")
            .Validate(value => value.TimeoutSeconds is >= 1 and <= 3_600, "Ollama:TimeoutSeconds must be between 1 and 3600.")
            .ValidateOnStart();
        services.AddOptions<SentinelOptions>().Bind(configuration.GetSection(SentinelOptions.SectionName)).ValidateOnStart();
        services.AddOptions<AgentOptions>()
            .Bind(configuration.GetSection(AgentOptions.SectionName))
            .Validate(value => value.MaxReadBytes > 0 && value.MaxWriteBytes > 0 && value.MaxDirectoryEntries > 0, "Agent file limits must be positive.")
            .Validate(value => value.MaxBrowserTextCharacters > 0 && value.BrowserTimeoutSeconds is >= 1 and <= 600, "Agent browser limits are invalid.")
            .ValidateOnStart();
        services.AddOptions<MemoryRetrievalOptions>()
            .Bind(configuration.GetSection(MemoryRetrievalOptions.SectionName))
            .Validate(value => value.MaxEntries > 0 && value.MinimumScore >= 0 && value.MaxContextCharacters > 0 && value.MaxEntryContentCharacters > 0, "Memory retrieval limits are invalid.")
            .ValidateOnStart();
        services.AddOptions<WorkspaceIndexOptions>()
            .Bind(configuration.GetSection(WorkspaceIndexOptions.SectionName))
            .Validate(value => value.MaxFilesPerWorkspace > 0 && value.MaxFileBytes > 0 && value.MaxRetrievedDocuments > 0 && value.MaxContextCharacters > 0 && value.MaxDocumentContextCharacters > 0, "Workspace index limits must be positive.")
            .ValidateOnStart();
        services.AddOptions<StandardsOptions>()
            .Bind(configuration.GetSection(StandardsOptions.SectionName))
            .Validate(value => value.MaxStandards > 0 && value.MaxContextCharacters > 0 && value.MaxStandardContentCharacters > 0, "Standards limits must be positive.")
            .ValidateOnStart();
        services.AddOptions<ChatContextOptions>()
            .Bind(configuration.GetSection(ChatContextOptions.SectionName))
            .Validate(value => value.ReserveOutputTokens >= 0 && value.EstimatedCharactersPerToken is >= 1 and <= 12 && value.MaxToolResultCharacters > 0 && value.MinimumRecentMessageGroups > 0, "Chat context limits are invalid.")
            .ValidateOnStart();

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
        services.AddScoped<IMemoryRetrievalService, MemoryRetrievalService>();
        services.AddScoped<ISystemPromptService, SystemPromptService>();
        services.AddScoped<IModelProfileService, ModelProfileService>();
        services.AddScoped<IToolAuditService, ToolAuditService>();
        services.AddScoped<IProjectWorkspaceService, ProjectWorkspaceService>();
        services.AddScoped<IKnowledgeService, KnowledgeService>();
        services.AddScoped<IProjectStandardService, ProjectStandardService>();
        services.AddSingleton(sp => new ChatRequestComposer(sp.GetRequiredService<IOptions<ChatContextOptions>>().Value));

        // Agentic tools the model can request (each gated by user approval in the UI).
        services.AddSingleton<FileSystemPathPolicy>();
        services.AddScoped<IAgentTool, ShellCommandTool>();
        services.AddScoped<IAgentTool, ReadFileTool>();
        services.AddScoped<IAgentTool, WriteFileTool>();
        services.AddScoped<IAgentTool, ListDirectoryTool>();
        services.AddScoped<IAgentTool, BrowserControlTool>();

        return services;
    }
}
