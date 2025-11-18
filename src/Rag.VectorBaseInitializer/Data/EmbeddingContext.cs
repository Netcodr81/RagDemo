using Microsoft.EntityFrameworkCore;

namespace RagIndexer.Data;

public class EmbeddingContext: DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=Data\\data.db");
    }
}