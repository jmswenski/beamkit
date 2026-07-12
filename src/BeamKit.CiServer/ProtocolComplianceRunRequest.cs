using System.Text.Json;

namespace BeamKit.CiServer;

/// <summary>
/// Request to run a plan against an active RT-PX protocol.
/// </summary>
public sealed record ProtocolComplianceRunRequest
{
    /// <summary>
    /// Optional synthetic case id.
    /// </summary>
    public string? SyntheticCaseId { get; init; }

    /// <summary>
    /// Uploaded input format. Supported values include beamkit-plan-json and esapi-snapshot-json.
    /// </summary>
    public string? Format { get; init; }

    /// <summary>
    /// Inline BeamKit plan JSON object.
    /// </summary>
    public JsonElement? Plan { get; init; }

    /// <summary>
    /// Raw BeamKit plan JSON string.
    /// </summary>
    public string? PlanJson { get; init; }

    /// <summary>
    /// Inline ESAPI snapshot JSON object.
    /// </summary>
    public JsonElement? EsapiSnapshot { get; init; }

    /// <summary>
    /// Raw ESAPI snapshot JSON string.
    /// </summary>
    public string? EsapiSnapshotJson { get; init; }

    /// <summary>
    /// Active managed rule-pack id. Required unless an RT-PX acceptance id is supplied.
    /// </summary>
    public string? RulePackId { get; init; }

    /// <summary>
    /// Optional active managed rule-pack version id.
    /// </summary>
    public string? VersionId { get; init; }

    /// <summary>
    /// Optional promoted RT-PX acceptance id to bind explicitly.
    /// </summary>
    public string? RtpxAcceptanceId { get; init; }

    /// <summary>
    /// Optional input source label.
    /// </summary>
    public string? InputSource { get; init; }

    /// <summary>
    /// Optional variances to apply at creation time.
    /// </summary>
    public IReadOnlyList<ProtocolComplianceVarianceRequest>? Variances { get; init; }
}
