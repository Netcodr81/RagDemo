using System.Text;
using OllamaSharp;
using RagIndexer.Models;
using Telerik.Windows.Documents.Fixed.Model;

namespace RagIndexer;

public static class IndexingService
{
    public static async Task BuildDocumentIndex(RadFixedDocument document, string? documentTitle, string? author)
    {
        var ollamaUri = new Uri("http://localhost:11434");

        foreach (var page in document.Pages)
        {
            var stringBuilder = new StringBuilder();
            var client = ClientFactory.CreateOllamaClient(OllamaModels.NomicEmbedText, ollamaUri);
            var milvusClient = ClientFactory.CreateMilvusClient();
            client.SelectedModel = OllamaModels.NomicEmbedText;

            foreach (var element in page.Content)
            {
                if (element is Telerik.Windows.Documents.Fixed.Model.Text.TextFragment textFragment)
                {
                    stringBuilder.Append(textFragment.Text);
                }
            }

            var pageText = stringBuilder.ToString();
            var pageNumber = page.PageNo;

            var vectorResponse = await client.EmbedAsync(pageText);
            var vectors = vectorResponse.Embeddings?.FirstOrDefault()?.ToArray() ?? [];
        }
    }
}