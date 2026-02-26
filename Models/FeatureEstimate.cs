namespace AiEstimator.Models;

public class FeatureEstimate
{
    public string Feature { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DevEstimateHours { get; set; }
    public int ArtEstimateHours { get; set; }
    public string Complexity { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<ResourceAllocation> Allocations { get; set; } = new();
}

public class ResourceAllocation
{
    public string TeamMemberId { get; set; } = string.Empty;
    public string TeamMemberName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public int AllocatedHours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal TotalCost { get; set; }
}

public class GameEstimationResult
{
    public List<FeatureEstimate> Features { get; set; } = new();
    public int TotalDevHours { get; set; }
    public int TotalArtHours { get; set; }
    public int TotalHours { get; set; }
    public string ProjectSummary { get; set; } = string.Empty;
    public List<TeamMember> Team { get; set; } = new();
    public decimal TotalProjectCost { get; set; }
    public BillingSummary? Billing { get; set; }
}

public class BillingSummary
{
    public List<TeamMemberBilling> TeamMemberBillings { get; set; } = new();
    public decimal TotalDevCost { get; set; }
    public decimal TotalArtCost { get; set; }
    public decimal GrandTotal { get; set; }
}

public class TeamMemberBilling
{
    public string TeamMemberName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public int TotalHours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal TotalCost { get; set; }
}
