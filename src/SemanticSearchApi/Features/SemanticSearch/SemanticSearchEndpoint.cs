using Carter;
using Microsoft.AspNetCore.Mvc;
using SemanticSearchApi.Services;
using SharedKernel.ResponseModels;

namespace SemanticSearchApi.Features.SemanticSearch;

public class SemanticSearchEndpoint : ICarterModule
{

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/semantic-search", async Task<IResult> (
            [FromQuery] string query,
            [FromQuery] int? topK,
            [FromQuery] bool? includeEmbedding,
            [FromServices] DocumentVectorSearchService searchService
        ) =>
        {
            var results = await searchService.SearchAsync(query, topK ?? 5, includeEmbedding ?? false);
            return TypedResults.Ok(new SemanticSearchResponse
            {
                Items = !results.Any()
                    ? []
                    : results.Select(r => new Item(r.Document.DocumentName ?? string.Empty,
                        r.Document.Author ?? string.Empty, r.Document.Content ?? string.Empty,
                        RelevanceScore: Math.Round((decimal)r.Score * 100, 2))).ToList()
            });
        }).Produces<SemanticSearchResponse>();
    }
}