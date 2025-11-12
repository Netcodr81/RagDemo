// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using RagIndexer;
using RagIndexer.Data;
using Telerik.Windows.Documents.Fixed.FormatProviders.Pdf;

var ollamaUri = new Uri("http://localhost:11434");

var builder = Kernel.CreateBuilder();

builder.AddOllamaChatCompletion(OllamaModels.Llama32_1b, ollamaUri);
builder.AddOllamaTextGeneration(OllamaModels.Llama32_1b, ollamaUri);
builder.AddOllamaEmbeddingGenerator(OllamaModels.NomicEmbedText, ollamaUri);

builder.Services.AddSingleton<QdrantClient>(options => new QdrantClient("localhost"));
builder.Services.AddQdrantVectorStore("localhost");

builder.Services.AddLogging();


builder.Services.AddSingleton<IndexingService>();


var app = builder.Build();

var indexingService = app.Services.GetRequiredService<IndexingService>();
var quadrantClient = app.Services.GetRequiredService<QdrantClient>();

var quadrantCollections = await quadrantClient.CollectionExistsAsync(VectorDbCollections.DocumentVectors);

if(!quadrantCollections)
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
    Console.WriteLine("$PDF file not found at path: {pdfPath}");
    return;
}

try
{
    using var fileStream = new FileStream(pdfPath, FileMode.Open);
    var pdfFormatProvider = new PdfFormatProvider();
    var document = pdfFormatProvider.Import(fileStream);    
    
    var title = document.DocumentInfo.Title ?? "The Great Gatsby";
    var author = document.DocumentInfo.Author ?? "F. Scott Fitzgerald";

    // await indexingService.BuildDocumentIndex(document, title, author);
    

    var final = "Completed embedding extraction for document '{title}' by {author} with {vectors.Count} pages.";
}
catch (Exception exception)
{
    Console.WriteLine(exception);
    throw;
}