
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using OllamaSharp;
using RagIndexer;
using SharedKernel.Constants;
using SharedKernel.Models;


var ollamaUri = new Uri("http://localhost:11434");
var vectorDbConnectionString = "Host=localhost;Port=5432;Database=vector_db;Username=postgres;Password=postgres";

var builder = Host.CreateApplicationBuilder();

builder.Services.AddChatClient(new OllamaApiClient(ollamaUri, OllamaModels.Llama32_1b));
builder.Services.AddEmbeddingGenerator(new OllamaApiClient(ollamaUri, OllamaModels.NomicEmbedText));

builder.Services.AddPostgresVectorStore(connectionString: vectorDbConnectionString);



builder.Services.AddLogging();


builder.Services.AddTransient<IndexingService>();
builder.Services.AddSingleton<DocumentVectorStore>();

var app = builder.Build();

var indexingService = app.Services.GetRequiredService<IndexingService>();
var vectorStore = app.Services.GetRequiredService<VectorStore>();

var collection = vectorStore.GetCollection<Guid, DocumentVector>(VectorDbCollections.DocumentVectors);
await collection.EnsureCollectionExistsAsync();

var isOllamaReady = await EnsureOllamaReadyAsync(ollamaUri, OllamaModels.NomicEmbedText, CancellationToken.None);

if (!isOllamaReady)
{
    throw new InvalidOperationException($"Ollama service at {ollamaUri} is not ready or does not have the required model '{OllamaModels.NomicEmbedText}' available.");
}


var mdDir = Path.Combine(Directory.GetCurrentDirectory(), "Markdown");

if (!Directory.Exists(mdDir))
{
    Console.WriteLine($"Markdown folder not found at path: {mdDir}");
    return;
}

var mdFilesList = new List<string>();
mdFilesList.AddRange(Directory.EnumerateFiles(mdDir, "*.md", SearchOption.TopDirectoryOnly));
mdFilesList.AddRange(Directory.EnumerateFiles(mdDir, "*.markdown", SearchOption.TopDirectoryOnly));

if (mdFilesList.Count == 0)
{
    Console.WriteLine($"No markdown files found in {mdDir}");
    return;
}

foreach (var filePath in mdFilesList)
{
    try
    {
        var document = await File.ReadAllTextAsync(filePath);
        var title = Path.GetFileNameWithoutExtension(filePath); // simple title from filename
        var author = GetAuthor(title) ?? "Unknown";

        Console.WriteLine($"Starting indexing for {title}");

        await indexingService.BuildDocumentIndex(document, title, author);

        Console.WriteLine($"Completed embedding extraction for '{title}' ({Path.GetFileName(filePath)}).");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to process '{filePath}': {ex.Message}");
        // continue processing remaining files
    }
}


static async Task<bool> EnsureOllamaReadyAsync(Uri baseUri, string requiredModel, CancellationToken ct)
{
    try
    {
        using var http = new HttpClient { BaseAddress = baseUri };
        using var resp = await http.GetAsync("/api/tags", ct);
        if (!resp.IsSuccessStatusCode) return false;
        var json = await resp.Content.ReadAsStringAsync(ct);
        return json?.IndexOf($"\"name\":\"{requiredModel}\"", StringComparison.OrdinalIgnoreCase) >= 0;
    }
    catch
    {
        return false;
    }
}

static string? GetAuthor(string file)
{
    Dictionary<string, string> Authors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "1984", "George Orwell" },
        { "A Christmas Carol", "Charles Dickens" },
        { "Frankenstein", "Mary Shelley" },
        {"Grimms' Fairy Tales", "Brothers Grimm"},
        {"The Adventures of Tom Sawyer", "Mark Twain"},
        {"The Great Gatsby", "F. Scott Fitzgerald"},
        {"The Three Little Pigs", "Joseph Jacobs"}
    };

    return Authors.FirstOrDefault(x => x.Key == file).Value;
}