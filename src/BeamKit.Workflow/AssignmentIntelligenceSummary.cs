namespace BeamKit.Workflow;

/// <summary>
/// Optional predictive context attached to an assignment recommendation.
/// </summary>
public sealed record AssignmentIntelligenceSummary
{
    /// <summary>
    /// Creates an assignment intelligence summary.
    /// </summary>
    public AssignmentIntelligenceSummary(
        string planId,
        string? diseaseSite,
        decimal complexityScore,
        string complexityLevel,
        decimal qaRiskScore,
        string qaRiskLevel,
        decimal estimatedPlanningHours,
        decimal estimatedPhysicsReviewMinutes,
        int appliedAssignmentComplexityScore,
        IReadOnlyList<string>? suggestedSkills,
        IReadOnlyList<string>? topSignals,
        IReadOnlyList<string>? recommendations)
    {
        PlanId = WorkflowText.Required(planId, nameof(planId));
        DiseaseSite = WorkflowText.Optional(diseaseSite);
        ComplexityScore = complexityScore;
        ComplexityLevel = WorkflowText.Required(complexityLevel, nameof(complexityLevel));
        QaRiskScore = qaRiskScore;
        QaRiskLevel = WorkflowText.Required(qaRiskLevel, nameof(qaRiskLevel));
        EstimatedPlanningHours = estimatedPlanningHours;
        EstimatedPhysicsReviewMinutes = estimatedPhysicsReviewMinutes;
        if (appliedAssignmentComplexityScore is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(appliedAssignmentComplexityScore), appliedAssignmentComplexityScore, "Assignment complexity score must be between 1 and 5.");
        }

        AppliedAssignmentComplexityScore = appliedAssignmentComplexityScore;
        SuggestedSkills = WorkflowText.CleanList(suggestedSkills);
        TopSignals = WorkflowText.CleanList(topSignals);
        Recommendations = WorkflowText.CleanList(recommendations);
    }

    /// <summary>
    /// Plan id used for prediction.
    /// </summary>
    public string PlanId { get; init; }

    /// <summary>
    /// Disease site used for prediction.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Predictive complexity score from 0 to 100.
    /// </summary>
    public decimal ComplexityScore { get; init; }

    /// <summary>
    /// Predictive complexity level.
    /// </summary>
    public string ComplexityLevel { get; init; }

    /// <summary>
    /// Predictive QA risk score from 0 to 100.
    /// </summary>
    public decimal QaRiskScore { get; init; }

    /// <summary>
    /// Predictive QA risk level.
    /// </summary>
    public string QaRiskLevel { get; init; }

    /// <summary>
    /// Estimated dosimetry planning effort in hours.
    /// </summary>
    public decimal EstimatedPlanningHours { get; init; }

    /// <summary>
    /// Estimated physics review effort in minutes.
    /// </summary>
    public decimal EstimatedPhysicsReviewMinutes { get; init; }

    /// <summary>
    /// Complexity score mapped to BeamKit assignment's 1-5 scale.
    /// </summary>
    public int AppliedAssignmentComplexityScore { get; init; }

    /// <summary>
    /// Skills inferred from plan metadata and predictive signals.
    /// </summary>
    public IReadOnlyList<string> SuggestedSkills { get; init; }

    /// <summary>
    /// Strongest predictive signals used for assignment context.
    /// </summary>
    public IReadOnlyList<string> TopSignals { get; init; }

    /// <summary>
    /// Intelligence recommendations relevant to assignment and review planning.
    /// </summary>
    public IReadOnlyList<string> Recommendations { get; init; }
}
