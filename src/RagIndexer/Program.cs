using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using RagIndexer;
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

// Remove registration for IVectorStoreRecordCollection since we now resolve IVectorStore directly

builder.Services.AddLogging();


builder.Services.AddSingleton<IndexingService>();
builder.Services.AddSingleton<DocumentVectorStore>();


var app = builder.Build();

var indexingService = app.Services.GetRequiredService<IndexingService>();
var quadrantClient = app.Services.GetRequiredService<QdrantClient>();

var collectionExists = await quadrantClient.CollectionExistsAsync(VectorDbCollections.DocumentVectors);

if(!collectionExists)
{
    await quadrantClient.CreateCollectionAsync(collectionName: VectorDbCollections.DocumentVectors, vectorsConfig: new VectorParams
    {
        Size = 512,
        Distance = Distance.Cosine
    });
}

string pdfPath = Path.Combine(Directory.GetCurrentDirectory(), "PDFs", "The Great Gatsby.pdf");

if (!File.Exists(pdfPath))
{
    Console.WriteLine($"$PDF file not found at path: {pdfPath}");
    return;
}

try
{
    using var fileStream = new FileStream(pdfPath, FileMode.Open);
    var pdfFormatProvider = new PdfFormatProvider();
    var document = pdfFormatProvider.Import(fileStream, timeout: null);    
    
    var title = document.DocumentInfo.Title ?? "The Great Gatsby";
    var author = document.DocumentInfo.Author ?? "F. Scott Fitzgerald";

    await indexingService.BuildDocumentIndex(document, title, author);
    

    Console.WriteLine($"Completed embedding extraction for document '{title}' by {author}.");
}
catch (Exception exception)
{
    Console.WriteLine(exception);
    throw;
}