namespace BeamKit.Workflow;

/// <summary>
/// Planner profile used by the vendor-neutral assignment recommendation engine.
/// </summary>
public sealed record PlannerProfile
{
    /// <summary>
    /// Creates a planner profile.
    /// </summary>
    public PlannerProfile(
        string id,
        string displayName,
        IEnumerable<string>? skills = null,
        IEnumerable<string>? preferredDiseaseSites = null,
        int activeCaseCount = 0,
        int maxActiveCaseCount = 10,
        DateOnly? ptoUntil = null)
    {
        Id = WorkflowText.Required(id, nameof(id));
        DisplayName = WorkflowText.Required(displayName, nameof(displayName));
        Skills = WorkflowText.CleanList(skills);
        PreferredDiseaseSites = WorkflowText.CleanList(preferredDiseaseSites);
        if (activeCaseCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(activeCaseCount), activeCaseCount, "Active case count cannot be negative.");
        }

        if (maxActiveCaseCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxActiveCaseCount), maxActiveCaseCount, "Maximum active case count must be positive.");
        }

        ActiveCaseCount = activeCaseCount;
        MaxActiveCaseCount = maxActiveCaseCount;
        PtoUntil = ptoUntil;
    }

    /// <summary>
    /// Stable planner id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Human-readable planner name.
    /// </summary>
    public string DisplayName { get; init; }

    /// <summary>
    /// Planner skills such as VMAT, SBRT, SRS, or Head and Neck.
    /// </summary>
    public IReadOnlyList<string> Skills { get; init; }

    /// <summary>
    /// Disease sites this planner is preferred for.
    /// </summary>
    public IReadOnlyList<string> PreferredDiseaseSites { get; init; }

    /// <summary>
    /// Current active workload.
    /// </summary>
    public int ActiveCaseCount { get; init; }

    /// <summary>
    /// Configured maximum active workload.
    /// </summary>
    public int MaxActiveCaseCount { get; init; }

    /// <summary>
    /// Date through which the planner is unavailable for PTO, when applicable.
    /// </summary>
    public DateOnly? PtoUntil { get; init; }

    /// <summary>
    /// Workload utilization from 0 to values above 1 when over capacity.
    /// </summary>
    public decimal Utilization => (decimal)ActiveCaseCount / MaxActiveCaseCount;
}
