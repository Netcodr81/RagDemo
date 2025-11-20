using Microsoft.Extensions.AI;
using SemanticSearchApi.Services;

namespace SemanticSearchApi.Registries;

public static class FunctionRegistry
{
    public static IEnumerable<AITool> GetTools(this IServiceProvider provider)
    {
        var vectorService = provider.GetRequiredService<DocumentVectorSearchService>();

        yield return AIFunctionFactory.Create(
            typeof(DocumentVectorSearchService).GetMethod(nameof(DocumentVectorSearchService.FindInDatabase),
            [typeof(string)])!,
            vectorService, new AIFunctionFactoryOptions
            {
                Name = "database_search_service",
                Description = "Searches for information about books currently stored in the database based on a semantic search query."
            });
    }
}