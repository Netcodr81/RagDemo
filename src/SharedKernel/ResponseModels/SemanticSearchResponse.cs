namespace SharedKernel.ResponseModels;

public class SemanticSearchResponse
{
    public List<Item> Items { get; init; } = new();
}

public record Item(string DocumentName, string Author, string Content, decimal RelevanceScore);
