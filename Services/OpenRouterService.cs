using OpenAI.Chat;

namespace AiEstimator.Services;

public class OpenRouterService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public OpenRouterService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<string> ExplainTextAsync(string text, int maxWords = 100)
    {
        var apiKey = _configuration["OpenRouter:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenRouter API key is not configured. Please add it to appsettings.json");
        }

        var model = _configuration["OpenRouter:Model"] ?? "openai/gpt-4o-mini";
        
        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = $@"Please explain the following text in detail. Keep your explanation to a maximum of {maxWords} words. Be clear and comprehensive:

{text}"
                }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions")
        {
            Content = JsonContent.Create(requestBody)
        };

        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        
        var siteUrl = _configuration["OpenRouter:SiteUrl"];
        var siteName = _configuration["OpenRouter:SiteName"];
        
        if (!string.IsNullOrEmpty(siteUrl))
        {
            request.Headers.Add("HTTP-Referer", siteUrl);
        }
        
        if (!string.IsNullOrEmpty(siteName))
        {
            request.Headers.Add("X-Title", siteName);
        }

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"OpenRouter API request failed: {response.StatusCode} - {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<OpenRouterResponse>();
        
        if (result?.Choices == null || result.Choices.Length == 0)
        {
            throw new Exception("No response from OpenRouter API");
        }

        return result.Choices[0].Message.Content;
    }

    private class OpenRouterResponse
    {
        public required Choice[] Choices { get; set; }
    }

    private class Choice
    {
        public required Message Message { get; set; }
    }

    private class Message
    {
        public required string Content { get; set; }
    }
}
