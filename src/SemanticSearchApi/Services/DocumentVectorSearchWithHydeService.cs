using Microsoft.Extensions.AI;
using Qdrant.Client;
using SharedKernel.Constants;
using SharedKernel.Models;

namespace SemanticSearchApi.Services;

// <summary>
/// Strongly typed search abstraction over Qdrant for DocumentVector records.
/// </summary>
public class DocumentVectorSearchWithHydeService(StringEmbeddingGenerator embeddingGenerator, QdrantClient qdrantClient, IChatClient chatClient, ChatOptions chatOptions, PromptService promptService)
{
    /// <summary>
    /// Performs a semantic vector similarity search against the document collection.
    /// </summary>
    /// <param name="query">Natural language query text.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="includeEmbedding">True to include stored vector in results (uses withVectors:true).</param>
    /// <returns>List of matching DocumentVector records with scores, ordered by similarity score (highest first).</returns>
    public async Task<IReadOnlyList<DocumentVectorSearchResult>> SearchAsync(string query, int topK = 5, bool includeEmbedding = false)
    {
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

        var results = await qdrantClient.SearchAsync(
            collectionName: VectorDbCollections.DocumentVectors,
            vector: queryVector,
            limit: (uint) topK
        );

        var mapped = new List<DocumentVectorSearchResult>(results.Count);
        foreach (var r in results)
        {
            var model = new DocumentVector
            {
                Id = Guid.Parse(r.Id.Uuid),
                DocumentName = r.Payload.TryGetValue("document_name", out var docName) ? docName.StringValue : string.Empty,
                Author = r.Payload.TryGetValue("author", out var author) ? author.StringValue : string.Empty,
                Content = r.Payload.TryGetValue("content", out var content) ? content.StringValue : string.Empty,
                Embedding = includeEmbedding && r.Vectors?.Vector?.Data != null ? r.Vectors.Vector.Data.ToArray() : null
            };

            mapped.Add(new DocumentVectorSearchResult
            {
                Document = model,
                Score = r.Score
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