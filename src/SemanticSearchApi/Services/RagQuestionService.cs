using Microsoft.Extensions.AI;

namespace SemanticSearchApi.Services;

public class RagQuestionService(DocumentVectorSearchService vectorSearch, IChatClient chatClient, ChatOptions chatOptions, PromptService promptService)
{
    public async Task<string> AnswerQuest(string question)
    {
        var searchResults = await vectorSearch.SearchAsync(question, topK: 10, includeEmbedding: false);

        var systemPrompt = promptService.RagSystemPrompt;

        var userPrompt = $@"User question: {question}

Retrieved documents:
{string.Join("\n\n", searchResults.Select(chunk => $@" Book Title: {chunk.Document.DocumentName}
 Author:{chunk.Document.Author}
Content: {chunk.Document.Content}"))}
";

        var messages = (new[]
        {
            new ChatMessage(ChatRole.System, systemPrompt),
            new ChatMessage(ChatRole.User, userPrompt)
        }).ToList();

        var response = await chatClient.GetResponseAsync(messages, chatOptions);

        return response.Text;
    }
}