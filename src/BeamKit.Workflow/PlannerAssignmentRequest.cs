namespace BeamKit.Workflow;

/// <summary>
/// Request for planner assignment recommendation.
/// </summary>
public sealed record PlannerAssignmentRequest
{
    /// <summary>
    /// Creates an assignment request.
    /// </summary>
    public PlannerAssignmentRequest(
        string caseId,
        string diseaseSite,
        DateOnly dueDate,
        IEnumerable<PlannerProfile> planners,
        IEnumerable<string>? requiredSkills = null,
        int complexityScore = 3,
        int priority = 3,
        string? physician = null,
        DateOnly? assignmentDate = null)
    {
        CaseId = WorkflowText.Required(caseId, nameof(caseId));
        DiseaseSite = WorkflowText.Required(diseaseSite, nameof(diseaseSite));
        DueDate = dueDate;
        Planners = planners?.ToArray() ?? throw new ArgumentNullException(nameof(planners));
        RequiredSkills = WorkflowText.CleanList(requiredSkills);
        ComplexityScore = ValidateScore(complexityScore, nameof(complexityScore));
        Priority = ValidateScore(priority, nameof(priority));
        Physician = WorkflowText.Optional(physician);
        AssignmentDate = assignmentDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (Planners.Count == 0)
        {
            throw new ArgumentException("At least one planner is required.", nameof(planners));
        }
    }

    /// <summary>
    /// Case id.
    /// </summary>
    public string CaseId { get; init; }

    /// <summary>
    /// Disease site.
    /// </summary>
    public string DiseaseSite { get; init; }

    /// <summary>
    /// Due date for planning completion.
    /// </summary>
    public DateOnly DueDate { get; init; }

    /// <summary>
    /// Date used for assignment scoring.
    /// </summary>
    public DateOnly AssignmentDate { get; init; }

    /// <summary>
    /// Required skills.
    /// </summary>
    public IReadOnlyList<string> RequiredSkills { get; init; }

    /// <summary>
    /// Complexity score from 1 to 5.
    /// </summary>
    public int ComplexityScore { get; init; }

    /// <summary>
    /// Priority score from 1 to 5.
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    /// Optional physician label.
    /// </summary>
    public string? Physician { get; init; }

    /// <summary>
    /// Candidate planners.
    /// </summary>
    public IReadOnlyList<PlannerProfile> Planners { get; init; }

    private static int ValidateScore(int value, string parameterName)
    {
        return value is < 1 or > 5
            ? throw new ArgumentOutOfRangeException(parameterName, value, "Score must be between 1 and 5.")
            : value;
    }
}
