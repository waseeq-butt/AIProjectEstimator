using System.Text.Json;
using AiEstimator.Models;

namespace AiEstimator.Services;

public class GameEstimationService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public GameEstimationService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<GameEstimationResult> EstimateGameFeaturesAsync(string gameDesignDocument)
    {
        var apiKey = _configuration["OpenRouter:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenRouter API key is not configured.");
        }

        var model = _configuration["OpenRouter:Model"] ?? "qwen/qwen-2.5-72b-instruct:free";
        
        var prompt = $@"You are an experienced game development project manager specializing in Unity engine projects. 

Analyze the following game design document and extract all features/requirements. For each feature, provide detailed estimates considering Unity development workflow.

For Development Estimates, consider:
- Unity scripting (C#)
- Physics/collision systems
- UI implementation with Unity UI/Canvas
- Integration with Unity systems (animation, audio, etc.)
- Testing and debugging in Unity

For Art Estimates, consider:
- 2D/3D asset creation
- Animation work
- UI/UX design
- Particle effects
- Shader work if needed

IMPORTANT: Return ONLY a valid JSON object. Do not include any explanatory text, markdown formatting, or code blocks. Just the raw JSON.

Required JSON structure:
{{
  ""projectSummary"": ""Brief 2-3 sentence overview of the game"",
  ""features"": [
    {{
      ""feature"": ""Feature name"",
      ""description"": ""Brief description"",
      ""devEstimateHours"": 40,
      ""artEstimateHours"": 20,
      ""complexity"": ""Low"",
      ""notes"": ""Unity-specific considerations""
    }}
  ]
}}

Game Design Document:
{gameDesignDocument}";

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            temperature = 0.7
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

        var jsonContent = result.Choices[0].Message.Content;
        
        // Clean up potential markdown code blocks and extra text
        jsonContent = jsonContent.Trim();
        
        // Remove markdown code blocks
        if (jsonContent.Contains("```json"))
        {
            var startIndex = jsonContent.IndexOf("```json") + 7;
            var endIndex = jsonContent.LastIndexOf("```");
            if (endIndex > startIndex)
            {
                jsonContent = jsonContent.Substring(startIndex, endIndex - startIndex);
            }
        }
        else if (jsonContent.Contains("```"))
        {
            var startIndex = jsonContent.IndexOf("```") + 3;
            var endIndex = jsonContent.LastIndexOf("```");
            if (endIndex > startIndex)
            {
                jsonContent = jsonContent.Substring(startIndex, endIndex - startIndex);
            }
        }
        
        // Extract JSON object if there's extra text
        var jsonStart = jsonContent.IndexOf('{');
        var jsonEnd = jsonContent.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            jsonContent = jsonContent.Substring(jsonStart, jsonEnd - jsonStart + 1);
        }
        
        jsonContent = jsonContent.Trim();

        EstimationResponseDto? estimationData;
        try
        {
            estimationData = JsonSerializer.Deserialize<EstimationResponseDto>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            throw new Exception($"Failed to parse AI response as JSON. Error: {ex.Message}. Content received: {jsonContent.Substring(0, Math.Min(200, jsonContent.Length))}...");
        }

        if (estimationData == null || estimationData.Features == null)
        {
            throw new Exception($"AI response did not contain valid estimation data. Content: {jsonContent.Substring(0, Math.Min(200, jsonContent.Length))}...");
        }

        var estimationResult = new GameEstimationResult
        {
            ProjectSummary = estimationData.ProjectSummary ?? "No summary provided",
            Features = estimationData.Features,
            TotalDevHours = estimationData.Features.Sum(f => f.DevEstimateHours),
            TotalArtHours = estimationData.Features.Sum(f => f.ArtEstimateHours),
            TotalHours = estimationData.Features.Sum(f => f.DevEstimateHours + f.ArtEstimateHours)
        };

        return estimationResult;
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

    private class EstimationResponseDto
    {
        public string? ProjectSummary { get; set; }
        public List<FeatureEstimate> Features { get; set; } = new();
    }
}
