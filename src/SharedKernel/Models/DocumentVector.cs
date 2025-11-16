using Microsoft.Extensions.VectorData;

namespace SharedKernel.Models;

public class DocumentVector
{
    [VectorStoreKey(StorageName = "id")]
    public Guid Id { get; set; }
    
    [VectorStoreData(StorageName = "document_name")]
    public required string DocumentName { get; set; }
    
    [VectorStoreData(StorageName = "author")]
    public required string Author { get; set; }
    
    [VectorStoreData(StorageName = "content")]
    public required string Content { get; set; }
    
    [VectorStoreVector(768, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw, StorageName = "document_embedding")]
    public float[]? Embedding { get; set; }
}