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
        DateOnly? ptoUntil = null,
        PlanningStaffRole role = PlanningStaffRole.Dosimetrist,
        int maxComplexityScore = 5,
        IEnumerable<string>? preferredPhysicians = null,
        IEnumerable<string>? blockedPhysicians = null,
        IEnumerable<PlannerScheduleDay>? schedule = null)
    {
        Id = WorkflowText.Required(id, nameof(id));
        DisplayName = WorkflowText.Required(displayName, nameof(displayName));
        Skills = WorkflowText.CleanList(skills);
        PreferredDiseaseSites = WorkflowText.CleanList(preferredDiseaseSites);
        PreferredPhysicians = WorkflowText.CleanList(preferredPhysicians);
        BlockedPhysicians = WorkflowText.CleanList(blockedPhysicians);
        if (activeCaseCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(activeCaseCount), activeCaseCount, "Active case count cannot be negative.");
        }

        if (maxActiveCaseCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxActiveCaseCount), maxActiveCaseCount, "Maximum active case count must be positive.");
        }

        if (maxComplexityScore is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(maxComplexityScore), maxComplexityScore, "Maximum complexity score must be between 1 and 5.");
        }

        ActiveCaseCount = activeCaseCount;
        MaxActiveCaseCount = maxActiveCaseCount;
        PtoUntil = ptoUntil;
        Role = role;
        MaxComplexityScore = maxComplexityScore;
        Schedule = schedule?.OrderBy(day => day.Date).ToArray() ?? Array.Empty<PlannerScheduleDay>();
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
    /// Staff role used for role-specific assignment.
    /// </summary>
    public PlanningStaffRole Role { get; init; }

    /// <summary>
    /// Maximum case complexity this staff member should receive without override.
    /// </summary>
    public int MaxComplexityScore { get; init; }

    /// <summary>
    /// Physicians this staff member commonly works with.
    /// </summary>
    public IReadOnlyList<string> PreferredPhysicians { get; init; }

    /// <summary>
    /// Physicians this staff member should not be paired with under local assignment rules.
    /// </summary>
    public IReadOnlyList<string> BlockedPhysicians { get; init; }

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
    /// Optional day-level schedule capacity.
    /// </summary>
    public IReadOnlyList<PlannerScheduleDay> Schedule { get; init; }

    /// <summary>
    /// Workload utilization from 0 to values above 1 when over capacity.
    /// </summary>
    public decimal Utilization => (decimal)ActiveCaseCount / MaxActiveCaseCount;
}
