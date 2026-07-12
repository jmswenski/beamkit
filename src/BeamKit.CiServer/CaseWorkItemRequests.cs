using System.Text.Json;
using BeamKit.Workflow;

namespace BeamKit.CiServer;

/// <summary>
/// Request to create a persistent planning work item.
/// </summary>
public sealed record CreateCaseWorkItemRequest
{
    /// <summary>
    /// Optional case id. When omitted, BeamKit infers it from synthetic or plan content.
    /// </summary>
    public string? CaseId { get; init; }

    /// <summary>
    /// Optional built-in PHI-free synthetic case id.
    /// </summary>
    public string? SyntheticCaseId { get; init; }

    /// <summary>
    /// Optional disease site.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Optional due date in <c>yyyy-MM-dd</c> format.
    /// </summary>
    public string? DueDate { get; init; }

    /// <summary>
    /// Optional priority score from 1 to 5.
    /// </summary>
    public int? Priority { get; init; }

    /// <summary>
    /// Optional starting status. Defaults to <see cref="CaseWorkItemStatus.NeedsAssignment"/>.
    /// </summary>
    public CaseWorkItemStatus? Status { get; init; }

    /// <summary>
    /// Optional physician label used for assignment compatibility rules.
    /// </summary>
    public string? Physician { get; init; }

    /// <summary>
    /// Optional inline BeamKit plan JSON object used for predictive assignment inference.
    /// </summary>
    public JsonElement? Plan { get; init; }

    /// <summary>
    /// Optional raw BeamKit plan JSON used for predictive assignment inference.
    /// </summary>
    public string? PlanJson { get; init; }

    /// <summary>
    /// Optional inline ESAPI snapshot JSON object used for predictive assignment inference.
    /// </summary>
    public JsonElement? EsapiSnapshot { get; init; }

    /// <summary>
    /// Optional raw ESAPI snapshot JSON used for predictive assignment inference.
    /// </summary>
    public string? EsapiSnapshotJson { get; init; }

    /// <summary>
    /// Optional rule-pack id associated with the work item.
    /// </summary>
    public string? RulePackId { get; init; }

    /// <summary>
    /// Optional CI run id associated with the work item.
    /// </summary>
    public string? LastRunId { get; init; }
}

/// <summary>
/// Request to explicitly assign staff to a case work item.
/// </summary>
public sealed record AssignCaseWorkItemRequest
{
    /// <summary>
    /// Assigned dosimetrist id.
    /// </summary>
    public string? DosimetristId { get; init; }

    /// <summary>
    /// Assigned dosimetrist display name.
    /// </summary>
    public string? DosimetristName { get; init; }

    /// <summary>
    /// Assigned physicist id.
    /// </summary>
    public string? PhysicistId { get; init; }

    /// <summary>
    /// Assigned physicist display name.
    /// </summary>
    public string? PhysicistName { get; init; }

    /// <summary>
    /// Optional status to apply with the assignment. Defaults to assigned when staff are present.
    /// </summary>
    public CaseWorkItemStatus? Status { get; init; }

    /// <summary>
    /// Optional assignment note.
    /// </summary>
    public string? Note { get; init; }
}

/// <summary>
/// Request to change a case work item status.
/// </summary>
public sealed record UpdateCaseWorkItemStatusRequest
{
    /// <summary>
    /// New queue status.
    /// </summary>
    public CaseWorkItemStatus Status { get; init; }

    /// <summary>
    /// Optional status-change note.
    /// </summary>
    public string? Note { get; init; }
}
