using GrantOS.Sentinel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrantOS.Sentinel.Infrastructure.Persistence;

public class SentinelDbContext(DbContextOptions<SentinelDbContext> options) : DbContext(options)
{
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<MemoryEntry> MemoryEntries => Set<MemoryEntry>();
    public DbSet<SystemPrompt> SystemPrompts => Set<SystemPrompt>();
    public DbSet<ModelProfile> ModelProfiles => Set<ModelProfile>();
    public DbSet<ToolAuditLog> ToolAuditLogs => Set<ToolAuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Conversation>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.ModelName).HasMaxLength(200);
            e.Property(x => x.Scope).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.SystemPrompt)
                .WithMany()
                .HasForeignKey(x => x.SystemPromptId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Messages)
                .WithOne(x => x.Conversation!)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ChatMessage>(e =>
        {
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Content).IsRequired();
            e.HasIndex(x => x.ConversationId);
        });

        b.Entity<MemoryEntry>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.Tags).HasMaxLength(500);
            e.Property(x => x.Scope).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.Category);
        });

        b.Entity<SystemPrompt>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Content).IsRequired();
        });

        b.Entity<ModelProfile>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<ToolAuditLog>(e =>
        {
            e.Property(x => x.ToolName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Action).HasMaxLength(200).IsRequired();
            e.Property(x => x.Scope).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.CreatedAt);
        });

        SeedData(b);
    }

    private static void SeedData(ModelBuilder b)
    {
        // Static timestamp: HasData must be deterministic across migrations.
        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        b.Entity<SystemPrompt>().HasData(new SystemPrompt
        {
            Id = 1,
            Name = "GrantOS Sentinel Default",
            IsDefault = true,
            CreatedAt = seededAt,
            UpdatedAt = seededAt,
            Content =
                "You are GrantOS Sentinel, Grant Watson's private local AI engineering companion. " +
                "You specialize in C#, ASP.NET, Blazor, Git, architecture, Docker, secure software delivery, " +
                "and practical technical mentorship. You prioritize accuracy over agreement, challenge weak " +
                "assumptions, state uncertainty clearly, and help Grant build durable engineering systems."
        });

        b.Entity<ModelProfile>().HasData(
            new ModelProfile { Id = 1, Name = "qwen2.5-coder", DisplayName = "Qwen2.5 Coder", Description = "Code-focused default.", ContextLength = 8192, Temperature = 0.3, IsDefault = true, CreatedAt = seededAt },
            new ModelProfile { Id = 2, Name = "qwen3", DisplayName = "Qwen3", Description = "General reasoning and chat.", ContextLength = 8192, Temperature = 0.7, IsDefault = false, CreatedAt = seededAt },
            new ModelProfile { Id = 3, Name = "gemma3", DisplayName = "Gemma 3", Description = "Lightweight general model.", ContextLength = 8192, Temperature = 0.7, IsDefault = false, CreatedAt = seededAt },
            new ModelProfile { Id = 4, Name = "deepseek-r1", DisplayName = "DeepSeek-R1", Description = "Reasoning model (emits <think> traces).", ContextLength = 8192, Temperature = 0.6, IsDefault = false, CreatedAt = seededAt }
        );
    }
}
