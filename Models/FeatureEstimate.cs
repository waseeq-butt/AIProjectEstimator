namespace AiEstimator.Models;

public class FeatureEstimate
{
    public string Feature { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DevEstimateHours { get; set; }
    public int ArtEstimateHours { get; set; }
    public string Complexity { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class GameEstimationResult
{
    public List<FeatureEstimate> Features { get; set; } = new();
    public int TotalDevHours { get; set; }
    public int TotalArtHours { get; set; }
    public int TotalHours { get; set; }
    public string ProjectSummary { get; set; } = string.Empty;
}
