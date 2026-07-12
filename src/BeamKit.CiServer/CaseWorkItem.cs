using BeamKit.Check;
using BeamKit.Workflow;

namespace BeamKit.CiServer;

/// <summary>
/// Persistent queue item representing one planning case in the hosted BeamKit server.
/// </summary>
public sealed record CaseWorkItem
{
    /// <summary>
    /// Stable work-item id.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// UTC time when the work item was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// UTC time when the work item was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; init; }

    /// <summary>
    /// Vendor-neutral case id used to connect queue state, CI runs, and baseline checks.
    /// </summary>
    public string CaseId { get; init; } = string.Empty;

    /// <summary>
    /// Optional built-in synthetic case id used to create or infer the queue item.
    /// </summary>
    public string? SyntheticCaseId { get; init; }

    /// <summary>
    /// Disease site used for assignment, filtering, and workload review.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Planning due date.
    /// </summary>
    public DateOnly? DueDate { get; init; }

    /// <summary>
    /// Priority score from 1 to 5.
    /// </summary>
    public int Priority { get; init; } = 3;

    /// <summary>
    /// Current queue status.
    /// </summary>
    public CaseWorkItemStatus Status { get; init; } = CaseWorkItemStatus.NeedsAssignment;

    /// <summary>
    /// Optional physician label used for compatibility rules.
    /// </summary>
    public string? Physician { get; init; }

    /// <summary>
    /// Assigned dosimetrist id.
    /// </summary>
    public string? AssignedDosimetristId { get; init; }

    /// <summary>
    /// Assigned dosimetrist display name.
    /// </summary>
    public string? AssignedDosimetristName { get; init; }

    /// <summary>
    /// Assigned physicist id.
    /// </summary>
    public string? AssignedPhysicistId { get; init; }

    /// <summary>
    /// Assigned physicist display name.
    /// </summary>
    public string? AssignedPhysicistName { get; init; }

    /// <summary>
    /// Optional rule-pack id associated with the work item.
    /// </summary>
    public string? RulePackId { get; init; }

    /// <summary>
    /// Most recent BeamKit CI run id linked to this work item.
    /// </summary>
    public string? LastRunId { get; init; }

    /// <summary>
    /// Most recent BeamKit CI status linked to this work item.
    /// </summary>
    public BeamKitCheckStatus? LastCheckStatus { get; init; }

    /// <summary>
    /// Predictive assignment context inferred from plan content, when available.
    /// </summary>
    public AssignmentIntelligenceSummary? Intelligence { get; init; }

    /// <summary>
    /// Assignment and status history for the work item.
    /// </summary>
    public IReadOnlyList<CaseWorkItemAssignmentEvent> AssignmentHistory { get; init; } = Array.Empty<CaseWorkItemAssignmentEvent>();

    /// <summary>
    /// Indicates whether this item should count toward assigned staff workload.
    /// </summary>
    public bool IsActiveWorkload => CaseWorkItemStatusFacts.IsActiveWorkload(Status);
}
