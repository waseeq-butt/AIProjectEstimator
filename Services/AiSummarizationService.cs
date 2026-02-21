using OpenAI.Chat;

namespace AiEstimator.Services;

public class AiSummarizationService
{
    private readonly IConfiguration _configuration;

    public AiSummarizationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> SummarizeTextAsync(string text, int maxWords = 250)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured. Please add it to appsettings.json");
        }

        var client = new ChatClient("gpt-4o-mini", apiKey);

        var prompt = $@"Please summarize the following text in a maximum of {maxWords} words. 
Be concise and capture the key points:

{text}";

        var completion = await client.CompleteChatAsync(prompt);

        return completion.Value.Content[0].Text;
    }
}
