using System.Text.Json;
using BeamKit.Workflow;

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
    /// Optional built-in PHI-free synthetic case id used to infer disease site, skills, complexity, and risk.
    /// </summary>
    public string? SyntheticCaseId { get; init; }

    /// <summary>
    /// Disease site used for assignment matching.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Required planner skills such as VMAT, SBRT, or SRS.
    /// </summary>
    public IReadOnlyList<string>? RequiredSkills { get; init; }

    /// <summary>
    /// Required assignment roles. Defaults to dosimetrist for single recommendations and dosimetrist plus physicist for team recommendations.
    /// </summary>
    public IReadOnlyList<string>? RequiredRoles { get; init; }

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
    /// Optional embedded staff roster. When omitted, the server uses synthetic PHI-free defaults.
    /// </summary>
    public StaffRoster? Roster { get; init; }

    /// <summary>
    /// Optional raw staff roster JSON.
    /// </summary>
    public string? RosterJson { get; init; }

    /// <summary>
    /// Optional local staff roster path on the server host.
    /// </summary>
    public string? RosterPath { get; init; }

    /// <summary>
    /// When true, active hosted queue assignments are added to roster workload before scoring.
    /// </summary>
    public bool UseLiveWorkload { get; init; } = true;
}
