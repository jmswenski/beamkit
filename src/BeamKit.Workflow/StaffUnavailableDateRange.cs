namespace BeamKit.Workflow;

/// <summary>
/// Date range during which a planning staff member is unavailable for assignment.
/// </summary>
public sealed record StaffUnavailableDateRange
{
    /// <summary>
    /// Creates an unavailable date range.
    /// </summary>
    public StaffUnavailableDateRange(DateOnly startDate, DateOnly endDate, string? note = null)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("Unavailable range end date cannot be before start date.", nameof(endDate));
        }

        StartDate = startDate;
        EndDate = endDate;
        Note = WorkflowText.Optional(note);
    }

    /// <summary>
    /// First unavailable date.
    /// </summary>
    public DateOnly StartDate { get; init; }

    /// <summary>
    /// Last unavailable date.
    /// </summary>
    public DateOnly EndDate { get; init; }

    /// <summary>
    /// Optional reason such as PTO, clinic, call coverage, or service coverage.
    /// </summary>
    public string? Note { get; init; }

    /// <summary>
    /// Returns true when the supplied date falls inside the unavailable range.
    /// </summary>
    public bool Contains(DateOnly date)
    {
        return date >= StartDate && date <= EndDate;
    }
}
