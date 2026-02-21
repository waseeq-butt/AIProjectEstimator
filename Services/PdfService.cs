using System.Text;
using System.Text.Json;

namespace AiEstimator.Services;

public class PdfService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private const string API_BASE_URL = "https://api.pdf.co/v1";

    public PdfService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<string> ExtractTextFromPdfAsync(Stream pdfStream)
    {
        var apiKey = _configuration["PdfCo:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("PDF.co API key is not configured. Please add it to appsettings.json");
        }

        // Step 1: Upload the PDF file to PDF.co
        var uploadUrl = await UploadFileAsync(pdfStream, apiKey);

        // Step 2: Extract text from the uploaded PDF
        var extractedText = await ExtractTextAsync(uploadUrl, apiKey);

        return extractedText;
    }

    private async Task<string> UploadFileAsync(Stream fileStream, string apiKey)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        content.Add(streamContent, "file", "document.pdf");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{API_BASE_URL}/file/upload");
        request.Headers.Add("x-api-key", apiKey);
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);
        
        return jsonDoc.RootElement.GetProperty("url").GetString() 
            ?? throw new Exception("Failed to get upload URL");
    }

    private async Task<string> ExtractTextAsync(string fileUrl, string apiKey)
    {
        var requestBody = new
        {
            url = fileUrl,
            async = false
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{API_BASE_URL}/pdf/convert/to/text");
        request.Headers.Add("x-api-key", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);

        if (!jsonDoc.RootElement.GetProperty("error").GetBoolean())
        {
            var textUrl = jsonDoc.RootElement.GetProperty("url").GetString();
            if (!string.IsNullOrEmpty(textUrl))
            {
                var textResponse = await _httpClient.GetStringAsync(textUrl);
                return textResponse;
            }
        }

        throw new Exception("Failed to extract text from PDF");
    }
}
