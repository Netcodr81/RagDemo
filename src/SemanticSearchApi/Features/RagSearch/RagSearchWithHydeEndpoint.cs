using Carter;
using Microsoft.AspNetCore.Mvc;
using SemanticSearchApi.Services;

namespace SemanticSearchApi.Features.RagSearch;

public class RagSearchWithHydeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/ask/rag-with-hyde", async Task<IResult> (
            [FromQuery] string query,
            [FromServices] RagQuestionService questionService
        ) =>
        {
            var results = await questionService.AnswerQuestWithHyde(query);
            return TypedResults.Ok(results);
        }).Produces<string>();
    }
}