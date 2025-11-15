using Microsoft.Extensions.AI;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SharedKernel.Constants;
using SharedKernel.Models;

namespace RagApi.Services;

/// <summary>
/// Strongly typed search abstraction over Qdrant for DocumentVector records.
/// </summary>
public class DocumentVectorSearch(StringEmbeddingGenerator embeddingGenerator, QdrantClient qdrantClient)
{
    /// <summary>
    /// Performs a semantic vector similarity search against the document collection.
    /// </summary>
    /// <param name="query">Natural language query text.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="includeEmbedding">True to include stored vector in results (uses withVectors:true).</param>
    /// <returns>List of matching DocumentVector records ordered by similarity score.</returns>
    public async Task<IReadOnlyList<DocumentVector>> SearchAsync(string query, int topK = 5, bool includeEmbedding = false)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<DocumentVector>();
        }

        var embedding = await embeddingGenerator.GenerateAsync([query], new EmbeddingGenerationOptions
        {
            Dimensions = 512
        });

        var queryVector = embedding[0].Vector.ToArray();

        var results = await qdrantClient.SearchAsync(
            collectionName: VectorDbCollections.DocumentVectors,
            vector: queryVector,
            limit: (uint)topK
        );

        var mapped = new List<DocumentVector>(results.Count);
        foreach (var r in results)
        {
            var model = new DocumentVector
            {
                Id = Guid.Parse(r.Id.Uuid),
                DocumentName = r.Payload.TryGetValue("document_name", out var docName) ? docName.StringValue : string.Empty,
                Author = r.Payload.TryGetValue("author", out var author) ? author.StringValue : string.Empty,
                Content = r.Payload.TryGetValue("content", out var content) ? content.StringValue : string.Empty,
                PageNumber = r.Payload.TryGetValue("page_number", out var page) ? (int)page.IntegerValue : -1,
                Embedding = includeEmbedding && r.Vectors?.Vector?.Data != null ? r.Vectors.Vector.Data.ToArray() : null
            };
            mapped.Add(model);
        }

        return mapped;
    }
}
