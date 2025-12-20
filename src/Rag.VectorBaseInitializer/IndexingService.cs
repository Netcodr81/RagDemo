using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;

using SemanticChunkerNET;
using SharedKernel.Constants;
using SharedKernel.Models;
using EmbeddingGenerationOptions = Microsoft.Extensions.AI.EmbeddingGenerationOptions;
#pragma warning disable SKEXP0050
namespace RagIndexer;

public class IndexingService(StringEmbeddingGenerator embeddingGenerator, DocumentVectorStore vectorStore)
{
    private const int EmbeddingDimensions = OllamaModels.NomicEmbedTextDimensions;

    public async Task BuildDocumentIndex(string document, string? documentTitle, string? author, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(document))
        {
            Console.WriteLine("[IndexingService] Incoming document text is empty. Skipping.");
            return;
        }

        List<string> chunks;
        try
        {
            Console.WriteLine("[IndexingService] Beginning Semantic chunking.");

           var semanticChunker = new SemanticChunker(
                embeddingGenerator,
                tokenLimit: 512,
                thresholdType: BreakpointThresholdType.Percentile
                );

            var chunkedData = await semanticChunker.CreateChunksAsync(document);
            chunks = chunkedData.Select(c => c.Text).ToList();


        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IndexingService] Semantic chunking failed: {ex.Message}. Falling back to recursive chunking.");
            const int maxCharsPerChunk = 1200;
            const int overlapChars = 200;

            chunks = TextChunker
                .SplitPlainTextParagraphs([document], maxCharsPerChunk, overlapChars)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();
        }


        Console.WriteLine($"[IndexingService] Chunk count: {chunks.Count}");

        Console.WriteLine($"[IndexingService] Beginning embedding generation and indexing.");

       Console.WriteLine($"[IndexingService] Chunk count: {chunks.Count}");
        Console.WriteLine("[IndexingService] Beginning embedding generation and indexing.");

        var safeTitle = string.IsNullOrWhiteSpace(documentTitle) ? "Unknown Document" : documentTitle.Trim();
        var safeAuthor = string.IsNullOrWhiteSpace(author) ? "Unknown Author" : author.Trim();

        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var searchText =
                $"Title: {safeTitle}\n" +
                $"Author: {safeAuthor}\n\n" +
                chunk;

            IReadOnlyList<Microsoft.Extensions.AI.Embedding<float>> embeddings;
            try
            {
                embeddings = await embeddingGenerator.GenerateAsync(
                    new[] { searchText },
                    new EmbeddingGenerationOptions { Dimensions = EmbeddingDimensions },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IndexingService] Embedding generation failed: {ex.Message}");
                continue;
            }

            if (embeddings.Count == 0)
            {
                Console.WriteLine("[IndexingService] No embedding returned. Skipping.");
                continue;
            }

            var vecArray = embeddings[0].Vector.ToArray();
            if (vecArray.Length != EmbeddingDimensions)
            {
                Console.WriteLine($"[IndexingService] Invalid embedding length. Expected {EmbeddingDimensions}, got {vecArray.Length}. Skipping.");
                continue;
            }

            var documentVector = new DocumentVector
            {
                Id = Guid.NewGuid(),
                DocumentName = safeTitle,
                Author = safeAuthor,
                Content = chunk,
                Embedding = vecArray
            };

            try
            {
                await vectorStore.UpsertAsync(documentVector);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IndexingService] Upsert failed: {ex.Message}");
            }
        }

        Console.WriteLine("[IndexingService] Indexing completed.");
    }
}