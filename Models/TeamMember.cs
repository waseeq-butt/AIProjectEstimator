namespace AiEstimator.Models;

public class TeamMember
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Developer" or "Artist"
    public string Level { get; set; } = string.Empty; // "Senior" or "Junior"
    public decimal HourlyRate { get; set; }
}

public class TeamManagementViewModel
{
    public List<TeamMember> TeamMembers { get; set; } = new();
}
