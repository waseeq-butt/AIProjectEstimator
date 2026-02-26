using Microsoft.AspNetCore.Mvc;
using AiEstimator.Services;
using System.Text.Json;

namespace AiEstimator.Controllers;

public class HomeController : Controller
{
    private readonly PdfService _pdfService;
    private readonly AiSummarizationService _aiService;
    private readonly OpenRouterService _openRouterService;
    private readonly GameEstimationService _gameEstimationService;

    public HomeController(PdfService pdfService, AiSummarizationService aiService, OpenRouterService openRouterService, GameEstimationService gameEstimationService)
    {
        _pdfService = pdfService;
        _aiService = aiService;
        _openRouterService = openRouterService;
        _gameEstimationService = gameEstimationService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> UploadPdf(IFormFile pdfFile)
    {
        if (pdfFile == null || pdfFile.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        if (!pdfFile.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only PDF files are allowed");
        }

        try
        {
            using var stream = pdfFile.OpenReadStream();
            var extractedText = await _pdfService.ExtractTextFromPdfAsync(stream);
            
            var estimationResult = await _gameEstimationService.EstimateGameFeaturesAsync(extractedText);
            
            TempData["EstimationResult"] = JsonSerializer.Serialize(estimationResult);
            TempData["FileName"] = pdfFile.FileName;
            
            return Ok(new { success = true, redirectUrl = Url.Action("Result") });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error processing PDF: {ex.Message}");
        }
    }

    public IActionResult Result()
    {
        var estimationJson = TempData["EstimationResult"] as string;
        var fileName = TempData["FileName"] as string ?? "Unknown";
        
        if (string.IsNullOrEmpty(estimationJson))
        {
            ViewBag.FileName = fileName;
            ViewBag.Error = "No estimation data available";
            return View();
        }

        var estimationResult = JsonSerializer.Deserialize<Models.GameEstimationResult>(estimationJson);
        
        ViewBag.FileName = fileName;
        ViewBag.EstimationResult = estimationResult;
        
        return View();
    }

    public IActionResult TestAi()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> TestAi(string inputText)
    {
        if (string.IsNullOrWhiteSpace(inputText))
        {
            return BadRequest("Please enter some text");
        }

        try
        {
            var explanation = await _openRouterService.ExplainTextAsync(inputText, maxWords: 100);
            return Ok(new { success = true, summary = explanation });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error: {ex.Message}");
        }
    }
}
