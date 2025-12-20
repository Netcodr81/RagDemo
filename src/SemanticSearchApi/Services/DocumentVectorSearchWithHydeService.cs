using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using SharedKernel.Constants;
using SharedKernel.Models;

namespace SemanticSearchApi.Services;

/// <summary>
/// HyDE variant of vector search: generate a short hypothetical answer (HyDE) and embed that for retrieval.
/// Uses the configured <see cref="VectorStore"/> (Postgres/pgvector in this solution).
/// </summary>
public class DocumentVectorSearchWithHydeService(StringEmbeddingGenerator embeddingGenerator, VectorStore vectorStore, IChatClient chatClient, ChatOptions chatOptions, PromptService promptService)
{
    /// <summary>
    /// Performs a semantic vector similarity search against the document collection using HyDE.
    /// </summary>
    /// <param name="query">Natural language query text.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="includeEmbedding">True to include stored vectors in results (may increase payload size).</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>List of matching DocumentVector records with scores, ordered by similarity score (highest first).</returns>
    public async Task<IReadOnlyList<DocumentVectorSearchResult>> SearchAsync(string query, int topK = 5, bool includeEmbedding = false, CancellationToken cancellationToken = default)
    {
        var collection = vectorStore.GetCollection<Guid, DocumentVector>(VectorDbCollections.DocumentVectors);
        
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<DocumentVectorSearchResult>();
        }

        var hypothesis = await GenerateHypothesisAsync(query);

        var textToEmbed = hypothesis ?? query;
        
        var embedding = await embeddingGenerator.GenerateAsync([textToEmbed], new EmbeddingGenerationOptions
        {
            Dimensions = OllamaModels.NomicEmbedTextDimensions
        });

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
    
    private async Task<string?> GenerateHypothesisAsync(string question)
    {
        var systemText = "You create concise, factual reference passages.";
        var userText = promptService.HydePrompt.Replace("{{question}}", question);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemText),
            new(ChatRole.User, userText)
        };

        var response = await chatClient.GetResponseAsync(messages, chatOptions);
        var text = response?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;
        return text.Length > 1500 ? text[..1500] : text;
    }
}