using Microsoft.EntityFrameworkCore;

namespace GrantOS.Sentinel.Infrastructure.Persistence;

/// <summary>SQLite FTS5 indexes kept synchronized with the three knowledge source tables.</summary>
public static class KnowledgeFtsSchema
{
    public const string CreateSql = """
        CREATE VIRTUAL TABLE IF NOT EXISTS MemoryEntriesFts USING fts5(Title, Category, Tags, Content, content='MemoryEntries', content_rowid='Id');
        CREATE TRIGGER IF NOT EXISTS MemoryEntriesFts_ai AFTER INSERT ON MemoryEntries BEGIN INSERT INTO MemoryEntriesFts(rowid, Title, Category, Tags, Content) VALUES (new.Id, new.Title, new.Category, new.Tags, new.Content); END;
        CREATE TRIGGER IF NOT EXISTS MemoryEntriesFts_ad AFTER DELETE ON MemoryEntries BEGIN INSERT INTO MemoryEntriesFts(MemoryEntriesFts, rowid, Title, Category, Tags, Content) VALUES ('delete', old.Id, old.Title, old.Category, old.Tags, old.Content); END;
        CREATE TRIGGER IF NOT EXISTS MemoryEntriesFts_au AFTER UPDATE ON MemoryEntries BEGIN INSERT INTO MemoryEntriesFts(MemoryEntriesFts, rowid, Title, Category, Tags, Content) VALUES ('delete', old.Id, old.Title, old.Category, old.Tags, old.Content); INSERT INTO MemoryEntriesFts(rowid, Title, Category, Tags, Content) VALUES (new.Id, new.Title, new.Category, new.Tags, new.Content); END;
        CREATE VIRTUAL TABLE IF NOT EXISTS ProjectDocumentsFts USING fts5(RelativePath, Content, content='ProjectDocuments', content_rowid='Id');
        CREATE TRIGGER IF NOT EXISTS ProjectDocumentsFts_ai AFTER INSERT ON ProjectDocuments BEGIN INSERT INTO ProjectDocumentsFts(rowid, RelativePath, Content) VALUES (new.Id, new.RelativePath, new.Content); END;
        CREATE TRIGGER IF NOT EXISTS ProjectDocumentsFts_ad AFTER DELETE ON ProjectDocuments BEGIN INSERT INTO ProjectDocumentsFts(ProjectDocumentsFts, rowid, RelativePath, Content) VALUES ('delete', old.Id, old.RelativePath, old.Content); END;
        CREATE TRIGGER IF NOT EXISTS ProjectDocumentsFts_au AFTER UPDATE ON ProjectDocuments BEGIN INSERT INTO ProjectDocumentsFts(ProjectDocumentsFts, rowid, RelativePath, Content) VALUES ('delete', old.Id, old.RelativePath, old.Content); INSERT INTO ProjectDocumentsFts(rowid, RelativePath, Content) VALUES (new.Id, new.RelativePath, new.Content); END;
        CREATE VIRTUAL TABLE IF NOT EXISTS ProjectStandardsFts USING fts5(Name, Category, AppliesTo, Content, content='ProjectStandards', content_rowid='Id');
        CREATE TRIGGER IF NOT EXISTS ProjectStandardsFts_ai AFTER INSERT ON ProjectStandards BEGIN INSERT INTO ProjectStandardsFts(rowid, Name, Category, AppliesTo, Content) VALUES (new.Id, new.Name, new.Category, new.AppliesTo, new.Content); END;
        CREATE TRIGGER IF NOT EXISTS ProjectStandardsFts_ad AFTER DELETE ON ProjectStandards BEGIN INSERT INTO ProjectStandardsFts(ProjectStandardsFts, rowid, Name, Category, AppliesTo, Content) VALUES ('delete', old.Id, old.Name, old.Category, old.AppliesTo, old.Content); END;
        CREATE TRIGGER IF NOT EXISTS ProjectStandardsFts_au AFTER UPDATE ON ProjectStandards BEGIN INSERT INTO ProjectStandardsFts(ProjectStandardsFts, rowid, Name, Category, AppliesTo, Content) VALUES ('delete', old.Id, old.Name, old.Category, old.AppliesTo, old.Content); INSERT INTO ProjectStandardsFts(rowid, Name, Category, AppliesTo, Content) VALUES (new.Id, new.Name, new.Category, new.AppliesTo, new.Content); END;
        INSERT INTO MemoryEntriesFts(MemoryEntriesFts) VALUES('rebuild');
        INSERT INTO ProjectDocumentsFts(ProjectDocumentsFts) VALUES('rebuild');
        INSERT INTO ProjectStandardsFts(ProjectStandardsFts) VALUES('rebuild');
        """;

    public const string DropSql = """
        DROP TRIGGER IF EXISTS MemoryEntriesFts_ai; DROP TRIGGER IF EXISTS MemoryEntriesFts_ad; DROP TRIGGER IF EXISTS MemoryEntriesFts_au; DROP TABLE IF EXISTS MemoryEntriesFts;
        DROP TRIGGER IF EXISTS ProjectDocumentsFts_ai; DROP TRIGGER IF EXISTS ProjectDocumentsFts_ad; DROP TRIGGER IF EXISTS ProjectDocumentsFts_au; DROP TABLE IF EXISTS ProjectDocumentsFts;
        DROP TRIGGER IF EXISTS ProjectStandardsFts_ai; DROP TRIGGER IF EXISTS ProjectStandardsFts_ad; DROP TRIGGER IF EXISTS ProjectStandardsFts_au; DROP TABLE IF EXISTS ProjectStandardsFts;
        """;

    public static void EnsureCreated(SentinelDbContext db) => db.Database.ExecuteSqlRaw(CreateSql);
}
