using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SharedKernel.Constants;
using SharedKernel.Models;

namespace RagIndexer;

public class DocumentVectorStore(VectorStore vectorStore)
{
    public async Task UpsertAsync(DocumentVector vector)
    {
        var collection = vectorStore.GetCollection<Guid, DocumentVector>(VectorDbCollections.DocumentVectors);
        // Validate embedding to avoid corrupt zero-length vectors in Qdrant
        ArgumentNullException.ThrowIfNull(vector);
        
        if (vector.Embedding is null || vector.Embedding.Length == 0)
        {
            throw new ArgumentException("Embedding is null or empty.", nameof(vector));
        }
        if (vector.Embedding.Length != OllamaModels.NomicEmbedTextDimensions)
        {
            throw new ArgumentException($"Embedding dimension mismatch. Expected {OllamaModels.NomicEmbedTextDimensions}, got {vector.Embedding.Length}.", nameof(vector));
        }
      
        await collection.UpsertAsync(vector);
    }
}
