using AiEstimator.Models;

namespace AiEstimator.Services;

public class TeamAllocationService
{
    public GameEstimationResult AllocateTeamAndCalculateCosts(GameEstimationResult estimation, List<TeamMember> team)
    {
        if (team == null || team.Count == 0)
        {
            return estimation;
        }

        var developers = team.Where(t => t.Role.Equals("Developer", StringComparison.OrdinalIgnoreCase)).ToList();
        var artists = team.Where(t => t.Role.Equals("Artist", StringComparison.OrdinalIgnoreCase)).ToList();

        if (developers.Count == 0 && artists.Count == 0)
        {
            return estimation;
        }

        foreach (var feature in estimation.Features)
        {
            feature.Allocations = new List<ResourceAllocation>();

            if (feature.DevEstimateHours > 0 && developers.Count > 0)
            {
                var devAllocations = AllocateHours(feature.DevEstimateHours, developers, feature.Complexity);
                feature.Allocations.AddRange(devAllocations);
            }

            if (feature.ArtEstimateHours > 0 && artists.Count > 0)
            {
                var artAllocations = AllocateHours(feature.ArtEstimateHours, artists, feature.Complexity);
                feature.Allocations.AddRange(artAllocations);
            }
        }

        estimation.Team = team;
        estimation.Billing = CalculateBillingSummary(estimation.Features);
        estimation.TotalProjectCost = estimation.Billing.GrandTotal;

        return estimation;
    }

    private List<ResourceAllocation> AllocateHours(int totalHours, List<TeamMember> teamMembers, string complexity)
    {
        var allocations = new List<ResourceAllocation>();

        var seniorMembers = teamMembers.Where(t => t.Level.Equals("Senior", StringComparison.OrdinalIgnoreCase)).ToList();
        var juniorMembers = teamMembers.Where(t => t.Level.Equals("Junior", StringComparison.OrdinalIgnoreCase)).ToList();

        int seniorHours = 0;
        int juniorHours = 0;

        switch (complexity.ToLower())
        {
            case "high":
                seniorHours = (int)(totalHours * 0.7);
                juniorHours = totalHours - seniorHours;
                break;
            case "medium":
                seniorHours = (int)(totalHours * 0.5);
                juniorHours = totalHours - seniorHours;
                break;
            case "low":
                seniorHours = (int)(totalHours * 0.3);
                juniorHours = totalHours - seniorHours;
                break;
            default:
                seniorHours = totalHours / 2;
                juniorHours = totalHours - seniorHours;
                break;
        }

        if (seniorMembers.Count > 0 && seniorHours > 0)
        {
            var hoursPerSenior = seniorHours / seniorMembers.Count;
            var remainder = seniorHours % seniorMembers.Count;

            for (int i = 0; i < seniorMembers.Count; i++)
            {
                var member = seniorMembers[i];
                var hours = hoursPerSenior + (i == 0 ? remainder : 0);

                if (hours > 0)
                {
                    allocations.Add(new ResourceAllocation
                    {
                        TeamMemberId = member.Id,
                        TeamMemberName = member.Name,
                        Role = member.Role,
                        Level = member.Level,
                        AllocatedHours = hours,
                        HourlyRate = member.HourlyRate,
                        TotalCost = hours * member.HourlyRate
                    });
                }
            }
        }
        else if (seniorMembers.Count == 0 && juniorMembers.Count > 0)
        {
            juniorHours = totalHours;
        }

        if (juniorMembers.Count > 0 && juniorHours > 0)
        {
            var hoursPerJunior = juniorHours / juniorMembers.Count;
            var remainder = juniorHours % juniorMembers.Count;

            for (int i = 0; i < juniorMembers.Count; i++)
            {
                var member = juniorMembers[i];
                var hours = hoursPerJunior + (i == 0 ? remainder : 0);

                if (hours > 0)
                {
                    allocations.Add(new ResourceAllocation
                    {
                        TeamMemberId = member.Id,
                        TeamMemberName = member.Name,
                        Role = member.Role,
                        Level = member.Level,
                        AllocatedHours = hours,
                        HourlyRate = member.HourlyRate,
                        TotalCost = hours * member.HourlyRate
                    });
                }
            }
        }

        return allocations;
    }

    private BillingSummary CalculateBillingSummary(List<FeatureEstimate> features)
    {
        var summary = new BillingSummary();
        var memberTotals = new Dictionary<string, TeamMemberBilling>();

        foreach (var feature in features)
        {
            foreach (var allocation in feature.Allocations)
            {
                if (!memberTotals.ContainsKey(allocation.TeamMemberId))
                {
                    memberTotals[allocation.TeamMemberId] = new TeamMemberBilling
                    {
                        TeamMemberName = allocation.TeamMemberName,
                        Role = allocation.Role,
                        Level = allocation.Level,
                        HourlyRate = allocation.HourlyRate,
                        TotalHours = 0,
                        TotalCost = 0
                    };
                }

                memberTotals[allocation.TeamMemberId].TotalHours += allocation.AllocatedHours;
                memberTotals[allocation.TeamMemberId].TotalCost += allocation.TotalCost;
            }
        }

        summary.TeamMemberBillings = memberTotals.Values.OrderBy(x => x.Role).ThenByDescending(x => x.Level).ToList();
        summary.TotalDevCost = summary.TeamMemberBillings.Where(x => x.Role.Equals("Developer", StringComparison.OrdinalIgnoreCase)).Sum(x => x.TotalCost);
        summary.TotalArtCost = summary.TeamMemberBillings.Where(x => x.Role.Equals("Artist", StringComparison.OrdinalIgnoreCase)).Sum(x => x.TotalCost);
        summary.GrandTotal = summary.TotalDevCost + summary.TotalArtCost;

        return summary;
    }
}
