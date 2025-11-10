using Microsoft.Extensions.AI;
using OllamaSharp;

namespace TextExtractorRunner;

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
  }