namespace BeamKit.Workflow;

/// <summary>
/// Planner assignment recommendation with ranked candidates.
/// </summary>
public sealed record PlannerAssignmentRecommendation
{
    /// <summary>
    /// Creates an assignment recommendation.
    /// </summary>
    public PlannerAssignmentRecommendation(string caseId, IEnumerable<PlannerCandidateScore> candidates, AssignmentIntelligenceSummary? intelligence = null)
    {
        CaseId = WorkflowText.Required(caseId, nameof(caseId));
        Candidates = candidates?.OrderByDescending(candidate => candidate.Score).ThenBy(candidate => candidate.Planner.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray()
            ?? throw new ArgumentNullException(nameof(candidates));
        Intelligence = intelligence;
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
    /// Optional predictive intelligence context used to derive assignment inputs.
    /// </summary>
    public AssignmentIntelligenceSummary? Intelligence { get; init; }

    /// <summary>
    /// Top recommended candidate, when available.
    /// </summary>
    public PlannerCandidateScore? RecommendedPlanner => Candidates.FirstOrDefault(candidate => candidate.IsAvailable) ?? Candidates.FirstOrDefault();
}
