using System.Text;
using SemanticChunkerNET;
using SharedKernel.Constants;
using SharedKernel.Models;
using EmbeddingGenerationOptions = Microsoft.Extensions.AI.EmbeddingGenerationOptions;

namespace RagIndexer;

public class IndexingService(StringEmbeddingGenerator embeddingGenerator, DocumentVectorStore vectorStore, SemanticChunker chunker)
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
            var produced = await chunker.CreateChunksAsync(document, cancellationToken);
            chunks = (produced?.Select(ExtractChunkText).Where(s => !string.IsNullOrWhiteSpace(s)).ToList()) ?? new List<string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IndexingService] Semantic chunking failed: {ex.Message}. Falling back to rule-based chunking.");
            chunks = NaiveChunks(document, maxChunkChars: 2000);
        }

        if (chunks.Count == 0)
        {
            // Ensure at least a single chunk
            chunks = new List<string> { CleanContent(document) };
        }

        Console.WriteLine($"[IndexingService] Chunk count: {chunks.Count}");

        for (int index = 0; index < chunks.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var content = CleanContent(chunks[index]);
            if (string.IsNullOrWhiteSpace(content)) continue;

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
            if (vector.Vector.Length == 0)
            {
                Console.WriteLine($"[IndexingService] Empty embedding for chunk {index}. Skipping.");
                continue;
            }

            var documentVector = new DocumentVector
            {
                Id = Guid.NewGuid(),
                DocumentName = documentTitle ?? "Unknown Document",
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

    private static List<string> NaiveChunks(string text, int maxChunkChars)
    {
        // Prefer to break on double-newlines or page breaks, keep paragraphs together
        var cleaned = CleanContent(text);
        var parts = cleaned.Split(new[] { "\f\n", "\f", "\n\n" }, StringSplitOptions.None);
        var chunks = new List<string>();
        var current = new StringBuilder();

        void Flush()
        {
            var s = current.ToString().Trim();
            if (s.Length > 0) chunks.Add(s);
            current.Clear();
        }

        foreach (var p in parts)
        {
            var piece = p.Trim();
            if (piece.Length == 0) continue;

            if (current.Length + piece.Length + 2 <= maxChunkChars)
            {
                if (current.Length > 0) current.AppendLine().AppendLine();
                current.Append(piece);
            }
            else if (piece.Length <= maxChunkChars)
            {
                Flush();
                current.Append(piece);
            }
            else
            {
                // Hard wrap overly long paragraphs
                int start = 0;
                while (start < piece.Length)
                {
                    var len = Math.Min(maxChunkChars, piece.Length - start);
                    var slice = piece.AsSpan(start, len).ToString();
                    if (current.Length > 0)
                    {
                        Flush();
                    }
                    current.Append(slice);
                    Flush();
                    start += len;
                }
            }
        }
        Flush();
        return chunks;
    }

    private static string ExtractChunkText(object? chunk)
    {
        if (chunk == null) return string.Empty;
        if (chunk is string s) return s;
        try
        {
            var type = chunk.GetType();
            var textProp = type.GetProperty("Text") ?? type.GetProperty("Content") ?? type.GetProperty("Value");
            if (textProp?.GetValue(chunk) is string textValue) return textValue;
        }
        catch (Exception)
        {
            // Ignored: best-effort attempt to read a text-like property. Fallback to ToString.
        }
        return chunk.ToString() ?? string.Empty;
    }

    private static string CleanContent(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t' && c != '\f')
            {
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString().Trim();
    }
}