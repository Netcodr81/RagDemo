using Qdrant.Client;
using Qdrant.Client.Grpc;
using SharedKernel.Constants;
using SharedKernel.Models;

namespace RagIndexer;

public class DocumentVectorStore(QdrantClient client)
{
    public async Task UpsertAsync(DocumentVector vector)
    {
        await client.UpsertAsync(
            collectionName: VectorDbCollections.DocumentVectors,
            points: new[]
            {
                new PointStruct
                {
                    Id = new PointId { Uuid = vector.Id.ToString() },
                    Vectors = vector.Embedding ?? Array.Empty<float>(),
                    Payload =
                    {
                        ["document_name"] = vector.DocumentName,
                        ["author"] = vector.Author,
                        ["content"] = vector.Content,
                    }
                }
            }
        );
    }
}
