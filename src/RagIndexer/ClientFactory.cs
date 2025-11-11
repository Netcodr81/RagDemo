using Milvus.Client;
using OllamaSharp;

namespace RagIndexer;

public static class ClientFactory
{
    public static OllamaApiClient CreateOllamaClient(string model, Uri uri)
    {
        return new OllamaApiClient(uri, model);
    }
    
    public static OllamaApiClient CreateOllamaClient(Uri uri)
    {
        return new OllamaApiClient(uri);
    }
    
    public static MilvusClient CreateMilvusClient(string host = "localhost", string username = "minioadmin", string password = "minioadmin", int port = 19530)
    {
        return new MilvusClient(host, username: "minioadmin", password: "minioadmin", port);
    }
  }