namespace SemanticSearchApi.Services;

public class PromptService
{
    static readonly Dictionary<string, string> Prompts = [];

    static PromptService()
    {
        var promptsDirectory = Path.Combine(AppContext.BaseDirectory, "Prompts");
        
        foreach(var promptName in new []{ PromptTypes.RagSystemPrompt, PromptTypes.ChatSystemPrompt, PromptTypes.HydePrompt })
        {
            var promptText = File.ReadAllText(Path.Combine(promptsDirectory, promptName + ".txt"));
            Prompts[promptName] = promptText;
        }
    }
    
    public string RagSystemPrompt => Prompts.FirstOrDefault(x => x.Key == PromptTypes.RagSystemPrompt).Value;
    public string ChatSystemPrompt => Prompts.FirstOrDefault(x => x.Key == PromptTypes.ChatSystemPrompt).Value;
    public string HydePrompt => Prompts.FirstOrDefault(x => x.Key == PromptTypes.HydePrompt).Value;
}

public static class PromptTypes
{
    public const string RagSystemPrompt = "RagSystemPrompt";
    public const string ChatSystemPrompt = "ChatSystemPrompt";
    public const string HydePrompt = "HydePrompt";
}