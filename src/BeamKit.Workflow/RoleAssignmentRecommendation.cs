namespace BeamKit.Workflow;

/// <summary>
/// Role-specific assignment recommendation for a plan staffing request.
/// </summary>
public sealed record RoleAssignmentRecommendation
{
    /// <summary>
    /// Creates a role-specific assignment recommendation.
    /// </summary>
    public RoleAssignmentRecommendation(PlanningStaffRole role, PlannerAssignmentRecommendation recommendation)
    {
        Role = role;
        Recommendation = recommendation ?? throw new ArgumentNullException(nameof(recommendation));
    }

    /// <summary>
    /// Staff role being assigned.
    /// </summary>
    public PlanningStaffRole Role { get; init; }

    /// <summary>
    /// Ranked recommendation for this role.
    /// </summary>
    public PlannerAssignmentRecommendation Recommendation { get; init; }

    /// <summary>
    /// Recommended staff candidate for this role.
    /// </summary>
    public PlannerCandidateScore? RecommendedCandidate => Recommendation.RecommendedPlanner;
}
