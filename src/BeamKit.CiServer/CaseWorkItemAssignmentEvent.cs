namespace BeamKit.CiServer;

/// <summary>
/// Assignment or status history entry for a hosted case work item.
/// </summary>
public sealed record CaseWorkItemAssignmentEvent
{
    /// <summary>
    /// Event id.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// UTC time when the event occurred.
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; init; }

    /// <summary>
    /// Actor that triggered the event.
    /// </summary>
    public string Actor { get; init; } = "service";

    /// <summary>
    /// Event action, such as created, recommended, assigned, or status-changed.
    /// </summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>
    /// Work-item status at the time of the event.
    /// </summary>
    public CaseWorkItemStatus Status { get; init; }

    /// <summary>
    /// Assigned dosimetrist id after the event, when available.
    /// </summary>
    public string? DosimetristId { get; init; }

    /// <summary>
    /// Assigned dosimetrist display name after the event, when available.
    /// </summary>
    public string? DosimetristName { get; init; }

    /// <summary>
    /// Assigned physicist id after the event, when available.
    /// </summary>
    public string? PhysicistId { get; init; }

    /// <summary>
    /// Assigned physicist display name after the event, when available.
    /// </summary>
    public string? PhysicistName { get; init; }

    /// <summary>
    /// Optional human-readable event note.
    /// </summary>
    public string? Note { get; init; }
}
