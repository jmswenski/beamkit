using System.Text.Json;

namespace BeamKit.CiServer;

/// <summary>
/// Request to create a hosted BeamKit CI run from uploaded vendor-neutral plan content.
/// </summary>
public sealed record HostedCiRunUploadRequest
{
    /// <summary>
    /// Input format. Supported values include <c>beamkit-plan-json</c> and <c>esapi-snapshot-json</c>.
    /// </summary>
    public string? Format { get; init; }

    /// <summary>
    /// Inline BeamKit plan JSON object.
    /// </summary>
    public JsonElement? Plan { get; init; }

    /// <summary>
    /// Raw BeamKit plan JSON string. Useful for clients that cannot send nested objects.
    /// </summary>
    public string? PlanJson { get; init; }

    /// <summary>
    /// Inline ESAPI snapshot JSON object.
    /// </summary>
    public JsonElement? EsapiSnapshot { get; init; }

    /// <summary>
    /// Raw ESAPI snapshot JSON string. Useful for clients that cannot send nested objects.
    /// </summary>
    public string? EsapiSnapshotJson { get; init; }

    /// <summary>
    /// Optional registered rule-pack id. The built-in synthetic rule pack is used when omitted.
    /// </summary>
    public string? RulePackId { get; init; }

    /// <summary>
    /// Optional server-local rule-pack manifest path.
    /// </summary>
    public string? RulePackPath { get; init; }

    /// <summary>
    /// Optional managed naming-dictionary id. When supplied, the active promoted version overrides the rule-pack dictionary for this run.
    /// </summary>
    public string? NamingDictionaryId { get; init; }

    /// <summary>
    /// Optional managed naming-dictionary version id. The version must be active before it can drive a run.
    /// </summary>
    public string? NamingDictionaryVersionId { get; init; }

    /// <summary>
    /// Optional source label. Defaults to the inferred upload source and plan id.
    /// </summary>
    public string? InputSource { get; init; }

    /// <summary>
    /// Optional source-control branch.
    /// </summary>
    public string? Branch { get; init; }

    /// <summary>
    /// Optional source-control commit.
    /// </summary>
    public string? Commit { get; init; }

    /// <summary>
    /// Optional build identifier supplied by an external CI system.
    /// </summary>
    public string? BuildId { get; init; }
}
