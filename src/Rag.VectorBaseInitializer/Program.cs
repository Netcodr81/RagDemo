using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using RagIndexer;
using SharedKernel.Constants;


var ollamaUri = new Uri("http://localhost:11434");

var builder = Kernel.CreateBuilder();

builder.AddOllamaChatCompletion(OllamaModels.Llama32_1b, ollamaUri);
builder.AddOllamaTextGeneration(OllamaModels.Llama32_1b, ollamaUri);
builder.AddOllamaEmbeddingGenerator(OllamaModels.NomicEmbedText, ollamaUri);

builder.Services.AddSingleton<QdrantClient>(options => new QdrantClient("localhost"));
builder.Services.AddQdrantVectorStore("localhost");

builder.Services.AddLogging();


builder.Services.AddSingleton<IndexingService>();
builder.Services.AddSingleton<DocumentVectorStore>();

var app = builder.Build();

var indexingService = app.Services.GetRequiredService<IndexingService>();
var qdrantClient = app.Services.GetRequiredService<QdrantClient>();

var isOllamaReady = await EnsureOllamaReadyAsync(ollamaUri, OllamaModels.NomicEmbedText, CancellationToken.None);

var collectionExists = await qdrantClient.CollectionExistsAsync(VectorDbCollections.DocumentVectors);

if (!collectionExists)
{
    // Create collection with expected vector size and cosine distance
    await qdrantClient.CreateCollectionAsync(collectionName: VectorDbCollections.DocumentVectors, vectorsConfig: new VectorParams
    {
        Size = (ulong)OllamaModels.NomicEmbedTextDimensions,
        Distance = Distance.Cosine
    });
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