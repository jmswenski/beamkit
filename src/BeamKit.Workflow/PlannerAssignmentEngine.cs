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

    /// <summary>
    /// Recommends a planning team across the requested roles.
    /// </summary>
    public PlanStaffingRecommendation RecommendTeam(PlannerAssignmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var roleRecommendations = request.RequiredRoles
            .Select(role => new RoleAssignmentRecommendation(
                role,
                Recommend(request with
                {
                    RequiredRole = role,
                    RequiredRoles = new[] { role }
                })))
            .ToArray();

        return new PlanStaffingRecommendation(request.CaseId, roleRecommendations);
    }

    private static PlannerCandidateScore Score(PlannerProfile planner, PlannerAssignmentRequest request)
    {
        var score = 30;
        var reasons = new List<string>();
        var isAvailable = true;

        if (planner.Role == request.RequiredRole)
        {
            score += 8;
            reasons.Add($"Role match for {request.RequiredRole}.");
        }
        else
        {
            score -= 60;
            isAvailable = false;
            reasons.Add($"Role mismatch: {planner.Role} cannot fill {request.RequiredRole} assignment.");
        }

        if (planner.PtoUntil.HasValue && planner.PtoUntil.Value >= request.AssignmentDate)
        {
            score -= 45;
            isAvailable = false;
            reasons.Add($"Unavailable through {planner.PtoUntil.Value:yyyy-MM-dd}.");
        }
        else
        {
            score += 8;
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
            var workloadBonus = (int)Math.Round((1m - planner.Utilization) * 20m, MidpointRounding.AwayFromZero);
            score += workloadBonus;
            reasons.Add($"{planner.ActiveCaseCount}/{planner.MaxActiveCaseCount} active workload.");
        }

        ApplyScheduleScoring(planner, request, reasons, ref score, ref isAvailable);

        if (planner.PreferredDiseaseSites.Contains(request.DiseaseSite, StringComparer.OrdinalIgnoreCase)
            || planner.Skills.Contains(request.DiseaseSite, StringComparer.OrdinalIgnoreCase))
        {
            score += 12;
            reasons.Add($"Disease-site match for {request.DiseaseSite}.");
        }

        if (!string.IsNullOrWhiteSpace(request.Physician))
        {
            if (planner.BlockedPhysicians.Contains(request.Physician, StringComparer.OrdinalIgnoreCase))
            {
                score -= 80;
                isAvailable = false;
                reasons.Add($"Blocked with physician {request.Physician} by assignment rule.");
            }
            else if (planner.PreferredPhysicians.Contains(request.Physician, StringComparer.OrdinalIgnoreCase))
            {
                score += 8;
                reasons.Add($"Physician match for {request.Physician}.");
            }
        }

        var missingSkills = request.RequiredSkills
            .Where(skill => !planner.Skills.Contains(skill, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        if (missingSkills.Length == 0 && request.RequiredSkills.Count > 0)
        {
            score += 10;
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

        if (request.ComplexityScore > planner.MaxComplexityScore)
        {
            score -= 35;
            isAvailable = false;
            reasons.Add($"Complexity {request.ComplexityScore} exceeds configured maximum {planner.MaxComplexityScore}.");
        }

        return new PlannerCandidateScore(planner, score, reasons, isAvailable);
    }

    private static void ApplyScheduleScoring(
        PlannerProfile planner,
        PlannerAssignmentRequest request,
        List<string> reasons,
        ref int score,
        ref bool isAvailable)
    {
        if (planner.Schedule.Count == 0)
        {
            reasons.Add("No day-level schedule supplied; using active workload capacity.");
            return;
        }

        var scheduleWindow = planner.Schedule
            .Where(day => day.Date >= request.AssignmentDate && day.Date <= request.DueDate)
            .ToArray();
        if (scheduleWindow.Length == 0)
        {
            score -= 20;
            isAvailable = false;
            reasons.Add("No schedule coverage between assignment date and due date.");
            return;
        }

        var availableSlots = scheduleWindow.Sum(day => day.AvailableSlots);
        if (availableSlots <= 0)
        {
            score -= 35;
            isAvailable = false;
            reasons.Add("No open schedule capacity before due date.");
            return;
        }

        var scheduleBonus = Math.Min(12, availableSlots * 3);
        score += scheduleBonus;
        reasons.Add($"{availableSlots} open schedule slot(s) before due date.");

        var utilization = scheduleWindow.Average(day => day.Utilization);
        if (utilization >= 0.85m)
        {
            score -= 8;
            reasons.Add("Schedule is heavily loaded before due date.");
        }

        var assignmentDay = scheduleWindow.FirstOrDefault(day => day.Date == request.AssignmentDate);
        if (assignmentDay is { IsUnavailable: true })
        {
            score -= 5;
            reasons.Add("Unavailable on assignment date but has later schedule capacity.");
        }
    }
}
