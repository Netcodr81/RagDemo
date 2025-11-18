using Carter;
using Microsoft.AspNetCore.Mvc;
using SemanticSearchApi.Services;
using SharedKernel.ResponseModels;

namespace SemanticSearchApi.Features.RagSearch;

public class RagSearchEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/ask", async Task<IResult> (
            [FromQuery] string query,
            [FromServices] RagQuestionService questionService
        ) =>
        {
            var results = await questionService.AnswerQuest(query);
            return TypedResults.Ok(results);
        }).Produces<string>();
    }
}