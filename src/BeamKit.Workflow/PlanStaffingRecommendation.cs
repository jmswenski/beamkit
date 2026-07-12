namespace BeamKit.Workflow;

/// <summary>
/// Recommended planning team for a case, grouped by required clinical planning role.
/// </summary>
public sealed record PlanStaffingRecommendation
{
    /// <summary>
    /// Creates a plan staffing recommendation.
    /// </summary>
    public PlanStaffingRecommendation(string caseId, IEnumerable<RoleAssignmentRecommendation> roleRecommendations, AssignmentIntelligenceSummary? intelligence = null)
    {
        CaseId = WorkflowText.Required(caseId, nameof(caseId));
        RoleRecommendations = roleRecommendations?.ToArray() ?? throw new ArgumentNullException(nameof(roleRecommendations));
        Intelligence = intelligence;
    }

    /// <summary>
    /// Case id.
    /// </summary>
    public string CaseId { get; init; }

    /// <summary>
    /// Recommendations for each requested role.
    /// </summary>
    public IReadOnlyList<RoleAssignmentRecommendation> RoleRecommendations { get; init; }

    /// <summary>
    /// Optional predictive intelligence context used to derive assignment inputs.
    /// </summary>
    public AssignmentIntelligenceSummary? Intelligence { get; init; }

    /// <summary>
    /// Indicates whether every requested role has an available recommendation.
    /// </summary>
    public bool IsFullyStaffed => RoleRecommendations.All(role => role.RecommendedCandidate?.IsAvailable == true);
}
