using RagIndexer.TextUtilities;
using SharedKernel.Constants;
using SharedKernel.Models;
using EmbeddingGenerationOptions = Microsoft.Extensions.AI.EmbeddingGenerationOptions;

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

            // Use conservative chunk sizes to avoid oversized payloads
            const int chunkSize = 1000;
            const int chunkOverlap = 200;
            const float similarityThreshold = 0.8f;

            chunks = (await SemanticTextSplitter.SemanticSplitAsync(
                document,
                embeddingGenerator,
                chunkSize,
                chunkOverlap,
                similarityThreshold,
                GroupingStrategy.Paragraph)).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IndexingService] Semantic chunking failed: {ex.Message}. Falling back to recursive chunking.");
            chunks = RecursiveTextSplitter.RecursiveSplit(document, 1000, 200).ToList();
        }

        if (chunks.Count == 0)
        {
            // Ensure at least a single chunk
            chunks = new List<string> { DocumentTools.CleanContent(document) };
        }

        Console.WriteLine($"[IndexingService] Chunk count: {chunks.Count}");

        Console.WriteLine($"[IndexingService] Beginning embedding generation and indexing.");

        for (int index = 0; index < chunks.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var content = DocumentTools.CleanContent(chunks[index]);
            if (string.IsNullOrWhiteSpace(content)) continue;

            // Guard on max content length to avoid server-side 500s from oversized inputs
            if (content.Length > 8000)
            {
                content = content.Substring(0, 8000);
            }

            IReadOnlyList<Microsoft.Extensions.AI.Embedding<float>> embeddings;
            try
            {
                embeddings = await embeddingGenerator.GenerateAsync(new[] { content }, new EmbeddingGenerationOptions
                {
                    Dimensions = EmbeddingDimensions
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IndexingService] Embedding generation failed for chunk {index}: {ex.Message}");
                continue;
            }

            if (embeddings.Count == 0)
            {
                Console.WriteLine($"[IndexingService] No embedding returned for chunk {index}. Skipping.");
                continue;
            }

            var vector = embeddings[0];
            var vecArray = vector.Vector.ToArray();
            if (vecArray.Length != EmbeddingDimensions)
            {
                Console.WriteLine($"[IndexingService] Invalid embedding length for chunk {index}. Expected {EmbeddingDimensions}, got {vecArray.Length}. Skipping.");
                continue;
            }

            var documentVector = new DocumentVector
            {
                Id = Guid.NewGuid(),
                DocumentName = string.IsNullOrWhiteSpace(documentTitle) ? "Unknown Document" : documentTitle,
                Author = string.IsNullOrWhiteSpace(author) ? "Unknown Author" : author,
                Content = content,
                Embedding = vecArray
            };

            try
            {
                await vectorStore.UpsertAsync(documentVector);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IndexingService] Upsert failed for chunk {index}: {ex.Message}");
            }
        }

        Console.WriteLine("[IndexingService] Indexing completed.");
    }
}