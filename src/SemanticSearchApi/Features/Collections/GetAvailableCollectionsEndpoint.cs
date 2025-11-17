using Carter;
using Microsoft.AspNetCore.Mvc;
using Qdrant.Client;

namespace SemanticSearchApi.Features.Collections;

public class GetAvailableCollectionsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("collections", async Task<IResult> ([FromServices] QdrantClient qdrantClient) =>
        {
            var collections = await qdrantClient.ListFullSnapshotsAsync();
            return TypedResults.Ok(collections);
        });
    }
}