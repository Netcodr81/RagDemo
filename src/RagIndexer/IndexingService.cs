using System.Text;
using Qdrant.Client;
using RagIndexer.Data;
using RagIndexer.Models;
using Telerik.Windows.Documents.Fixed.Model;
using EmbeddingGenerationOptions = Microsoft.Extensions.AI.EmbeddingGenerationOptions;

namespace RagIndexer;

public class IndexingService(StringEmbeddingGenerator embeddingGenerator, QdrantClient qdrantClient)
{
    public async Task BuildDocumentIndex(RadFixedDocument document, string? documentTitle, string? author)
    {
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

            var embedding = await embeddingGenerator.GenerateAsync([pageText], new EmbeddingGenerationOptions
            {
                Dimensions = 512
            });
            var pageNumber = page.PageNo;

            var vectorArray = embedding[0].Vector.ToArray();
            
            var documentVector = new DocumentVector {
                Id = Guid.CreateVersion7(),
                DocumentName = documentTitle ?? "Unknown Document",
                Author = author ?? "Unknown Author",
                Content = pageText,
                PageNumber = pageNumber,
                Embedding = vectorArray
            };
            
        }
    }
}