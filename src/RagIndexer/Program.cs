using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using RagIndexer;
using SemanticChunkerNET;
using SharedKernel.Constants;
using SharedKernel.Models;
using Telerik.Windows.Documents.Fixed.FormatProviders.Pdf;

var ollamaUri = new Uri("http://localhost:11434");

var builder = Kernel.CreateBuilder();

builder.AddOllamaChatCompletion(OllamaModels.Llama32_1b, ollamaUri);
builder.AddOllamaTextGeneration(OllamaModels.Llama32_1b, ollamaUri);
builder.AddOllamaEmbeddingGenerator(OllamaModels.NomicEmbedText, ollamaUri);

builder.Services.AddSingleton<QdrantClient>(options => new QdrantClient("localhost"));
builder.Services.AddQdrantVectorStore("localhost");

builder.Services.AddSingleton<SemanticChunker>(sp =>
{
    var embeddingGenerator = sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
    return new SemanticChunker(embeddingGenerator, tokenLimit: 768);
});

builder.Services.AddLogging();


builder.Services.AddSingleton<IndexingService>();
builder.Services.AddSingleton<DocumentVectorStore>();


var app = builder.Build();

var indexingService = app.Services.GetRequiredService<IndexingService>();
var quadrantClient = app.Services.GetRequiredService<QdrantClient>();

var isOllamaReady = await EnsureOllamaReadyAsync(ollamaUri, OllamaModels.NomicEmbedText, CancellationToken.None);

var collectionExists = await quadrantClient.CollectionExistsAsync(VectorDbCollections.DocumentVectors);

if(!collectionExists)
{
    await quadrantClient.CreateCollectionAsync(collectionName: VectorDbCollections.DocumentVectors, vectorsConfig: new VectorParams
    {
        Size = (ulong)OllamaModels.NomicEmbedTextDimensions,
        Distance = Distance.Cosine
    });
}


string mdPath = Path.Combine(Directory.GetCurrentDirectory(), "Markdown", "The Three Little Pigs.md");

if (!File.Exists(mdPath))
{
    Console.WriteLine($"Markdown file not found at path: {mdPath}");
    return;
}

try
{
    var document = await File.ReadAllTextAsync(mdPath);

    var title = "The Three Little Pigs";
    var author = "Joseph Jacobs";

    await indexingService.BuildDocumentIndex(document, title, author);

    Console.WriteLine($"Completed embedding extraction for document '{title}' by {author}.");
}
catch (Exception exception)
{
    Console.WriteLine(exception);
    // Do not rethrow, keep the process alive and log only
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