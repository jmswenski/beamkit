namespace BeamKit.Workflow;

/// <summary>
/// Recommends planner assignment from workload, skills, disease site, PTO, priority, and due-date context.
/// </summary>
public sealed class PlannerAssignmentEngine
{
    /// <summary>
    /// Recommends a planner for the supplied request.
    /// </summary>
    public PlannerAssignmentRecommendation Recommend(PlannerAssignmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var candidates = request.Planners.Select(planner => Score(planner, request)).ToArray();
        return new PlannerAssignmentRecommendation(request.CaseId, candidates);
    }

    private static PlannerCandidateScore Score(PlannerProfile planner, PlannerAssignmentRequest request)
    {
        var score = 50;
        var reasons = new List<string>();
        var isAvailable = true;

        if (planner.PtoUntil.HasValue && planner.PtoUntil.Value >= request.AssignmentDate)
        {
            score -= 45;
            isAvailable = false;
            reasons.Add($"Unavailable through {planner.PtoUntil.Value:yyyy-MM-dd}.");
        }
        else
        {
            score += 10;
            reasons.Add("Available before assignment date.");
        }

        if (planner.Utilization >= 1m)
        {
            score -= 30;
            isAvailable = false;
            reasons.Add("At or above configured workload capacity.");
        }
        else
        {
            var workloadBonus = (int)Math.Round((1m - planner.Utilization) * 25m, MidpointRounding.AwayFromZero);
            score += workloadBonus;
            reasons.Add($"{planner.ActiveCaseCount}/{planner.MaxActiveCaseCount} active workload.");
        }

        if (planner.PreferredDiseaseSites.Contains(request.DiseaseSite, StringComparer.OrdinalIgnoreCase)
            || planner.Skills.Contains(request.DiseaseSite, StringComparer.OrdinalIgnoreCase))
        {
            score += 15;
            reasons.Add($"Disease-site match for {request.DiseaseSite}.");
        }

        var missingSkills = request.RequiredSkills
            .Where(skill => !planner.Skills.Contains(skill, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        if (missingSkills.Length == 0 && request.RequiredSkills.Count > 0)
        {
            score += 15;
            reasons.Add("All required skills matched.");
        }
        else if (missingSkills.Length > 0)
        {
            score -= missingSkills.Length * 10;
            reasons.Add($"Missing required skills: {string.Join(", ", missingSkills)}.");
        }

        var daysUntilDue = request.DueDate.DayNumber - request.AssignmentDate.DayNumber;
        if (daysUntilDue <= 1)
        {
            score -= request.Priority >= 4 ? 5 : 10;
            reasons.Add("Due within one day.");
        }
        else if (daysUntilDue <= 3)
        {
            score += 3;
            reasons.Add("Due within three days.");
        }
        else
        {
            score += 8;
            reasons.Add("Due date allows assignment flexibility.");
        }

        if (request.ComplexityScore >= 4 && planner.Skills.Count >= 3)
        {
            score += 5;
            reasons.Add("Broad skill profile for complex case.");
        }

        return new PlannerCandidateScore(planner, score, reasons, isAvailable);
    }
}
