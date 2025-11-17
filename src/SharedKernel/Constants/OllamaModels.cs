namespace SharedKernel.Constants;

public static class OllamaModels
{
    public const string NomicEmbedText = "nomic-embed-text:latest";
    public const string Llama32_1b = "llama3.2:1b";
    
    // Embedding dimensions for nomic-embed-text model
    public const int NomicEmbedTextDimensions = 768;
    
    public const string OllamaLocalEndpoint = "http://localhost:11434";
}