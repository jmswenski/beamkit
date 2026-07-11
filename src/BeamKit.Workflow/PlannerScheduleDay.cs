namespace BeamKit.Workflow;

/// <summary>
/// One staff schedule day used for assignment capacity scoring.
/// </summary>
public sealed record PlannerScheduleDay
{
    /// <summary>
    /// Creates a schedule day.
    /// </summary>
    public PlannerScheduleDay(DateOnly date, int assignedCaseCount = 0, int capacity = 1, bool isUnavailable = false, string? note = null)
    {
        if (assignedCaseCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(assignedCaseCount), assignedCaseCount, "Assigned case count cannot be negative.");
        }

        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity cannot be negative.");
        }

        Date = date;
        AssignedCaseCount = assignedCaseCount;
        Capacity = capacity;
        IsUnavailable = isUnavailable;
        Note = WorkflowText.Optional(note);
    }

    /// <summary>
    /// Schedule date.
    /// </summary>
    public DateOnly Date { get; init; }

    /// <summary>
    /// Number of already assigned cases on this date.
    /// </summary>
    public int AssignedCaseCount { get; init; }

    /// <summary>
    /// Number of cases this staff member can reasonably accept on this date.
    /// </summary>
    public int Capacity { get; init; }

    /// <summary>
    /// Indicates whether this date is blocked by PTO, call coverage, clinic, or another commitment.
    /// </summary>
    public bool IsUnavailable { get; init; }

    /// <summary>
    /// Optional schedule note.
    /// </summary>
    public string? Note { get; init; }

    /// <summary>
    /// Open case slots on this date.
    /// </summary>
    public int AvailableSlots => IsUnavailable ? 0 : Math.Max(0, Capacity - AssignedCaseCount);

    /// <summary>
    /// Schedule utilization from 0 to values above 1 when over scheduled capacity.
    /// </summary>
    public decimal Utilization => Capacity == 0 ? 1m : (decimal)AssignedCaseCount / Capacity;
}
