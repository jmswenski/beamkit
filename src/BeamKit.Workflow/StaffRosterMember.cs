namespace BeamKit.Workflow;

/// <summary>
/// Configurable staff member from a BeamKit assignment roster.
/// </summary>
public sealed record StaffRosterMember
{
    /// <summary>
    /// Creates a staff roster member.
    /// </summary>
    public StaffRosterMember(
        string id,
        string displayName,
        PlanningStaffRole role = PlanningStaffRole.Dosimetrist,
        IReadOnlyList<string>? skills = null,
        IReadOnlyList<string>? preferredDiseaseSites = null,
        int activeCaseCount = 0,
        int maxActiveCaseCount = 10,
        int maxComplexityScore = 5,
        DateOnly? ptoUntil = null,
        IReadOnlyList<string>? preferredPhysicians = null,
        IReadOnlyList<string>? blockedPhysicians = null,
        IReadOnlyList<PlannerScheduleDay>? schedule = null,
        IReadOnlyList<StaffUnavailableDateRange>? unavailableDateRanges = null)
    {
        Id = WorkflowText.Required(id, nameof(id));
        DisplayName = WorkflowText.Required(displayName, nameof(displayName));
        Role = role;
        Skills = WorkflowText.CleanList(skills);
        PreferredDiseaseSites = WorkflowText.CleanList(preferredDiseaseSites);
        ActiveCaseCount = ValidateNonNegative(activeCaseCount, nameof(activeCaseCount));
        MaxActiveCaseCount = ValidatePositive(maxActiveCaseCount, nameof(maxActiveCaseCount));
        MaxComplexityScore = ValidateScore(maxComplexityScore, nameof(maxComplexityScore));
        PtoUntil = ptoUntil;
        PreferredPhysicians = WorkflowText.CleanList(preferredPhysicians);
        BlockedPhysicians = WorkflowText.CleanList(blockedPhysicians);
        Schedule = schedule?.OrderBy(day => day.Date).ToArray() ?? Array.Empty<PlannerScheduleDay>();
        UnavailableDateRanges = unavailableDateRanges?.OrderBy(range => range.StartDate).ThenBy(range => range.EndDate).ToArray()
            ?? Array.Empty<StaffUnavailableDateRange>();
    }

    /// <summary>
    /// Stable staff id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Human-readable staff name.
    /// </summary>
    public string DisplayName { get; init; }

    /// <summary>
    /// Staff role.
    /// </summary>
    public PlanningStaffRole Role { get; init; }

    /// <summary>
    /// Staff skills such as VMAT, SBRT, SRS, breast, or head and neck.
    /// </summary>
    public IReadOnlyList<string> Skills { get; init; }

    /// <summary>
    /// Disease sites this staff member is preferred for.
    /// </summary>
    public IReadOnlyList<string> PreferredDiseaseSites { get; init; }

    /// <summary>
    /// Current active case load.
    /// </summary>
    public int ActiveCaseCount { get; init; }

    /// <summary>
    /// Maximum active case load.
    /// </summary>
    public int MaxActiveCaseCount { get; init; }

    /// <summary>
    /// Maximum case complexity this staff member should receive without override.
    /// </summary>
    public int MaxComplexityScore { get; init; }

    /// <summary>
    /// Date through which this staff member is unavailable for PTO, when applicable.
    /// </summary>
    public DateOnly? PtoUntil { get; init; }

    /// <summary>
    /// Physicians this staff member commonly works with.
    /// </summary>
    public IReadOnlyList<string> PreferredPhysicians { get; init; }

    /// <summary>
    /// Physicians this staff member should not be paired with under local assignment rules.
    /// </summary>
    public IReadOnlyList<string> BlockedPhysicians { get; init; }

    /// <summary>
    /// Optional day-level schedule capacity.
    /// </summary>
    public IReadOnlyList<PlannerScheduleDay> Schedule { get; init; }

    /// <summary>
    /// PTO, clinic, call, or coverage ranges that block assignment.
    /// </summary>
    public IReadOnlyList<StaffUnavailableDateRange> UnavailableDateRanges { get; init; }

    /// <summary>
    /// Converts this roster member into the assignment engine profile for a request window.
    /// </summary>
    public PlannerProfile ToPlannerProfile(DateOnly assignmentDate, DateOnly dueDate)
    {
        return new PlannerProfile(
            Id,
            DisplayName,
            Skills,
            PreferredDiseaseSites,
            ActiveCaseCount,
            MaxActiveCaseCount,
            PtoUntil,
            Role,
            MaxComplexityScore,
            PreferredPhysicians,
            BlockedPhysicians,
            ExpandSchedule(assignmentDate, dueDate));
    }

    private IReadOnlyList<PlannerScheduleDay> ExpandSchedule(DateOnly assignmentDate, DateOnly dueDate)
    {
        if (UnavailableDateRanges.Count == 0)
        {
            return Schedule;
        }

        var byDate = Schedule.ToDictionary(day => day.Date);
        var start = Min(assignmentDate, UnavailableDateRanges.Min(range => range.StartDate));
        var end = Max(dueDate, UnavailableDateRanges.Max(range => range.EndDate));

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var range = UnavailableDateRanges.FirstOrDefault(item => item.Contains(date));
            if (range is null)
            {
                continue;
            }

            byDate[date] = byDate.TryGetValue(date, out var existing)
                ? existing with { IsUnavailable = true, Note = range.Note ?? existing.Note }
                : new PlannerScheduleDay(date, capacity: 0, isUnavailable: true, note: range.Note);
        }

        return byDate.Values.OrderBy(day => day.Date).ToArray();
    }

    private static int ValidateNonNegative(int value, string parameterName)
    {
        return value < 0
            ? throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.")
            : value;
    }

    private static int ValidatePositive(int value, string parameterName)
    {
        return value <= 0
            ? throw new ArgumentOutOfRangeException(parameterName, value, "Value must be positive.")
            : value;
    }

    private static int ValidateScore(int value, string parameterName)
    {
        return value is < 1 or > 5
            ? throw new ArgumentOutOfRangeException(parameterName, value, "Score must be between 1 and 5.")
            : value;
    }

    private static DateOnly Min(DateOnly left, DateOnly right)
    {
        return left < right ? left : right;
    }

    private static DateOnly Max(DateOnly left, DateOnly right)
    {
        return left > right ? left : right;
    }
}
