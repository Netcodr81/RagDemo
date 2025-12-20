using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using SharedKernel.Constants;
using SharedKernel.Models;

namespace SemanticSearchApi.Services;

/// <summary>
/// Represents a vector search result (record + similarity score).
/// </summary>
public class DocumentVectorSearchResult
{
    public required DocumentVector Document { get; set; }

    /// <summary>
    /// Similarity score produced by the configured <see cref="VectorStore"/> implementation.
    /// Higher is better.
    /// </summary>
    public double Score { get; set; }
}

/// <summary>
/// Strongly typed search abstraction over the configured <see cref="VectorStore"/> for <see cref="DocumentVector"/> records.
/// </summary>
public class DocumentVectorSearchService(StringEmbeddingGenerator embeddingGenerator, VectorStore vectorStore)
{
    /// <summary>
    /// Performs a semantic vector similarity search against the document collection.
    /// </summary>
    /// <param name="query">Natural language query text.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="includeEmbedding">True to include stored vectors in results (may increase payload size).</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>List of matching <see cref="DocumentVector"/> records with scores (highest score first).</returns>
    public async Task<IReadOnlyList<DocumentVectorSearchResult>> SearchAsync(string query, int topK = 5, bool includeEmbedding = false, CancellationToken cancellationToken = default)
    {
        var collection = vectorStore.GetCollection<Guid, DocumentVector>(VectorDbCollections.DocumentVectors);

        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<DocumentVectorSearchResult>();
        }

        var embedding = await embeddingGenerator.GenerateAsync(new[] { query }, new EmbeddingGenerationOptions
        {
            Dimensions = OllamaModels.NomicEmbedTextDimensions
        }, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var queryVector = embedding[0].Vector.ToArray();

        var results = collection.SearchAsync(queryVector, top: topK, new VectorSearchOptions<DocumentVector>
        {
            IncludeVectors = includeEmbedding
        }, cancellationToken: cancellationToken);

        var mapped = new List<DocumentVectorSearchResult>();

        await foreach (var r in results)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var model = new DocumentVector
            {
                Id = r.Record.Id,
                DocumentName = r.Record.DocumentName ?? string.Empty,
                Author = r.Record.Author ?? string.Empty,
                Content = r.Record.Content ?? string.Empty,
                Embedding = r.Record.Embedding
            };

            mapped.Add(new DocumentVectorSearchResult
            {
                Document = model,
                Score = r.Score ?? 0
            });
        }

        return mapped;
    }

    public async Task<IReadOnlyList<DocumentVectorSearchResult>> FindInDatabase(string query, CancellationToken cancellationToken = default) => await SearchAsync(query, 10, cancellationToken: cancellationToken);

}
