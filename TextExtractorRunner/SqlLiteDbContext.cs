using Microsoft.EntityFrameworkCore;

namespace TextExtractorRunner;

public class SqlLiteDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=Data\\embeddings-source.db");
    }
}