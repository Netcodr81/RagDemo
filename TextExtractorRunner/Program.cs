using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Milvus.Client;
using OllamaSharp;
using Telerik.Windows.Documents.Fixed.FormatProviders.Pdf;
using Telerik.Windows.Documents.Fixed.Model.Text;
using TextExtractorRunner;

var services = new ServiceCollection();

services.AddDbContext<SqlLiteDbContext>();
var ollamaUri = new Uri("http://localhost:11434");


const string ollamaEmbedModel = "nomic-embed-text";
const string ollamaChatModel = "llama3.2:1b";
var client = new OllamaApiClient(ollamaUri);

var models = await client.ListLocalModelsAsync();

var milvusClient = new MilvusClient("localhost", username: "minioadmin", password: "minioadmin");

string pdfPath = Path.Combine(Directory.GetCurrentDirectory(), "PDFs", "the-great-gatsby.pdf");

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
    
    var vectors = await EmbeddingsService.ExtractEmbeddings(document, ollamaEmbedModel);
    

 var final = "Completed embedding extraction for document '{title}' by {author} with {vectors.Count} pages.";
}
catch (Exception exception)
{
    Console.WriteLine(exception);
    throw;
}


