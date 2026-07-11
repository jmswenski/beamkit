namespace BeamKit.CiServer;

/// <summary>
/// Request to create a planner assignment recommendation.
/// </summary>
public sealed record AssignmentServerRequest
{
    /// <summary>
    /// Optional case id.
    /// </summary>
    public string? CaseId { get; init; }

    /// <summary>
    /// Disease site used for assignment matching.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Required planner skills such as VMAT, SBRT, or SRS.
    /// </summary>
    public IReadOnlyList<string>? RequiredSkills { get; init; }

    /// <summary>
    /// Due date in <c>yyyy-MM-dd</c> format.
    /// </summary>
    public string? DueDate { get; init; }

    /// <summary>
    /// Complexity score from 1 to 5.
    /// </summary>
    public int? ComplexityScore { get; init; }

    /// <summary>
    /// Priority score from 1 to 5.
    /// </summary>
    public int? Priority { get; init; }

    /// <summary>
    /// Optional physician label.
    /// </summary>
    public string? Physician { get; init; }
}
