using Carter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using SemanticSearchApi.Services;

namespace SemanticSearchApi.Features.Chat;

public class ChatEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/chat", async Task<IResult> (
            HttpContext httpContext,
            [FromBody] List<ChatMessage> messages,
            [FromServices] IChatClient chatClient,
            [FromServices] ChatOptions chatOptions,
            [FromServices] PromptService promptService
        ) =>
        {
            try
            {
                var withSystemPrompt = (new[] {new ChatMessage(ChatRole.System, promptService.ChatSystemPrompt)})
                    .Concat(messages)
                    .ToList();
                
                var response = await chatClient.GetResponseAsync(withSystemPrompt, chatOptions, httpContext.RequestAborted);
                
                return Results.Ok(response.Messages);
            }
            catch (OperationCanceledException)
            {
                return Results.StatusCode(499);
            }
        }).Produces<IEnumerable<ChatMessage>>();
    }
}