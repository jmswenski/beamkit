namespace BeamKit.Workflow;

/// <summary>
/// Planner assignment recommendation with ranked candidates.
/// </summary>
public sealed record PlannerAssignmentRecommendation
{
    /// <summary>
    /// Creates an assignment recommendation.
    /// </summary>
    public PlannerAssignmentRecommendation(string caseId, IEnumerable<PlannerCandidateScore> candidates)
    {
        CaseId = WorkflowText.Required(caseId, nameof(caseId));
        Candidates = candidates?.OrderByDescending(candidate => candidate.Score).ThenBy(candidate => candidate.Planner.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray()
            ?? throw new ArgumentNullException(nameof(candidates));
    }

    /// <summary>
    /// Case id.
    /// </summary>
    public string CaseId { get; init; }

    /// <summary>
    /// Ranked candidates.
    /// </summary>
    public IReadOnlyList<PlannerCandidateScore> Candidates { get; init; }

    /// <summary>
    /// Top recommended candidate, when available.
    /// </summary>
    public PlannerCandidateScore? RecommendedPlanner => Candidates.FirstOrDefault(candidate => candidate.IsAvailable) ?? Candidates.FirstOrDefault();
}
