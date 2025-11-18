namespace SemanticSearchApi.Services;

public class PromptService
{
    static readonly Dictionary<string, string> Prompts = [];

    static PromptService()
    {
        var promptsDirectory = Path.Combine(AppContext.BaseDirectory, "Prompts");
        
        foreach(var promptName in new []{ PromptTypes.RagSystemPrompt })
        {
            var promptText = File.ReadAllText(Path.Combine(promptsDirectory, promptName + ".txt"));
            Prompts[promptName] = promptText;
        }
    }
    
    public string RagSystemPrompt => Prompts.FirstOrDefault(x => x.Key == PromptTypes.RagSystemPrompt).Value;
}

public static class PromptTypes
{
    public const string RagSystemPrompt = "RagSystemPrompt";
}