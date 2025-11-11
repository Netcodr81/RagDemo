using System.Text;
using OllamaSharp;
using Telerik.Windows.Documents.Fixed.Model;
using Telerik.Windows.Documents.Flow.Model;

namespace TextExtractorRunner;

public static class EmbeddingsService
{
    public static async Task<List<(string Content, int PageNumber, float[] Vector)>> ExtractEmbeddings(RadFixedDocument document, string embeddingModel, string? documentTitle, string? author)
    {
        var ollamaUri = new Uri("http://localhost:11434");
        var client = new OllamaApiClient(ollamaUri);
        client.SelectedModel = embeddingModel;

        var vectors = new List<(string Content, int pageNumber, float[] Vector)>();

        foreach (var page in document.Pages)
        {
            var stringBuilder = new StringBuilder();

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
            var vector = vectorResponse.Embeddings[0].ToArray();
            vectors.Add((pageText, pageNumber, vector));
        }

        return vectors;
    }
}