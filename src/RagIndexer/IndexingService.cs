using System.Text;
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
            chunks = (await SemanticTextSplitter.SemanticSplitAsync(
                document,
                embeddingGenerator,
                chunkSize: 2000,
                chunkOverlap: 200,
                modelId: OllamaModels.NomicEmbedText,
                endpoint: new Uri(OllamaModels.OllamaLocalEndpoint),
                threshold: 0.8f,
                groupingStrategy: GroupingStrategy.Paragraph)).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IndexingService] Semantic chunking failed: {ex.Message}. Falling back to recursive chunking.");
            chunks = RecursiveTextSplitter.RecursiveSplit(document, 2000, 200).ToList();
        }

        if (chunks.Count == 0)
        {
            // Ensure at least a single chunk
            chunks = new List<string> {DocumentTools.CleanContent(document)};
        }

        Console.WriteLine($"[IndexingService] Chunk count: {chunks.Count}");

        for (int index = 0; index < chunks.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var content = DocumentTools.CleanContent(chunks[index]);
            if (string.IsNullOrWhiteSpace(content)) continue;

            IReadOnlyList<Microsoft.Extensions.AI.Embedding<float>> embeddings;
            try
            {
                embeddings = await embeddingGenerator.GenerateAsync(new[] {content}, new EmbeddingGenerationOptions
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
            if (vector.Vector.Length == 0)
            {
                Console.WriteLine($"[IndexingService] Empty embedding for chunk {index}. Skipping.");
                continue;
            }

            var documentVector = new DocumentVector
            {
                Id = Guid.NewGuid(),
                DocumentName = $"{documentTitle}.pdf" ?? "Unknown Document",
                Author = author ?? "Unknown Author",
                Content = content,
                Embedding = vector.Vector.ToArray()
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