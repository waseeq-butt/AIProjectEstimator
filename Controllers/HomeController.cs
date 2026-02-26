using Microsoft.AspNetCore.Mvc;
using AiEstimator.Services;
using AiEstimator.Models;
using System.Text.Json;

namespace AiEstimator.Controllers;

public class HomeController : Controller
{
    private readonly PdfService _pdfService;
    private readonly AiSummarizationService _aiService;
    private readonly OpenRouterService _openRouterService;
    private readonly GameEstimationService _gameEstimationService;
    private readonly PdfExportService _pdfExportService;
    private readonly TeamAllocationService _teamAllocationService;

    public HomeController(PdfService pdfService, AiSummarizationService aiService, OpenRouterService openRouterService, GameEstimationService gameEstimationService, PdfExportService pdfExportService, TeamAllocationService teamAllocationService)
    {
        _pdfService = pdfService;
        _aiService = aiService;
        _openRouterService = openRouterService;
        _gameEstimationService = gameEstimationService;
        _pdfExportService = pdfExportService;
        _teamAllocationService = teamAllocationService;
    }

    private List<TeamMember> GetTeamFromSession()
    {
        var teamJson = HttpContext.Session.GetString("Team");
        if (string.IsNullOrEmpty(teamJson))
        {
            return new List<TeamMember>();
        }
        return JsonSerializer.Deserialize<List<TeamMember>>(teamJson) ?? new List<TeamMember>();
    }

    private void SaveTeamToSession(List<TeamMember> team)
    {
        var teamJson = JsonSerializer.Serialize(team);
        HttpContext.Session.SetString("Team", teamJson);
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
            
            var team = GetTeamFromSession();
            if (team.Count > 0)
            {
                estimationResult = _teamAllocationService.AllocateTeamAndCalculateCosts(estimationResult, team);
            }
            
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
        var estimationJson = TempData.Peek("EstimationResult") as string;
        var fileName = TempData.Peek("FileName") as string ?? "Unknown";
        
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

    public IActionResult ExportPdf()
    {
        var estimationJson = TempData["EstimationResult"] as string;
        var fileName = TempData["FileName"] as string ?? "GameDesignDocument.pdf";
        
        if (string.IsNullOrEmpty(estimationJson))
        {
            return BadRequest("No estimation data available for export");
        }

        var estimationResult = JsonSerializer.Deserialize<Models.GameEstimationResult>(estimationJson);
        
        if (estimationResult == null)
        {
            return BadRequest("Failed to parse estimation data");
        }

        var pdfBytes = _pdfExportService.GenerateEstimationPdf(estimationResult, fileName);
        
        var exportFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_Estimation_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        
        return File(pdfBytes, "application/pdf", exportFileName);
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

    public IActionResult ManageTeam()
    {
        var team = GetTeamFromSession();
        var viewModel = new TeamManagementViewModel
        {
            TeamMembers = team
        };
        return View(viewModel);
    }

    [HttpPost]
    public IActionResult AddTeamMember([FromBody] TeamMember member)
    {
        if (string.IsNullOrWhiteSpace(member.Name) || 
            string.IsNullOrWhiteSpace(member.Role) || 
            string.IsNullOrWhiteSpace(member.Level) || 
            member.HourlyRate <= 0)
        {
            return BadRequest("All fields are required and rate must be greater than 0");
        }

        var team = GetTeamFromSession();
        member.Id = Guid.NewGuid().ToString();
        team.Add(member);
        SaveTeamToSession(team);

        return Ok(new { success = true, member = member });
    }

    [HttpDelete]
    public IActionResult DeleteTeamMember(string id)
    {
        var team = GetTeamFromSession();
        var member = team.FirstOrDefault(m => m.Id == id);
        
        if (member == null)
        {
            return NotFound("Team member not found");
        }

        team.Remove(member);
        SaveTeamToSession(team);

        return Ok(new { success = true });
    }
}
