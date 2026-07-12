using System.Text.Json;

namespace BeamKit.CiServer;

/// <summary>
/// Server request to accept a portable RT-PX package into a local CI-server rule-pack workflow.
/// </summary>
public sealed record RtpxAcceptanceServerRequest
{
    /// <summary>
    /// Server-local path to a `.rtpx.zip` package.
    /// </summary>
    public string? PackagePath { get; init; }

    /// <summary>
    /// Base64-encoded `.rtpx.zip` package content for API uploads.
    /// </summary>
    public string? PackageBase64 { get; init; }

    /// <summary>
    /// Server-local path to an institution acceptance profile JSON file.
    /// </summary>
    public string? InstitutionProfilePath { get; init; }

    /// <summary>
    /// Inline institution acceptance profile object.
    /// </summary>
    public JsonElement? InstitutionProfile { get; init; }

    /// <summary>
    /// Inline institution acceptance profile JSON.
    /// </summary>
    public string? InstitutionProfileJson { get; init; }

    /// <summary>
    /// Optional server-local ESAPI snapshot JSON path used as acceptance evidence.
    /// </summary>
    public string? EsapiSnapshotPath { get; init; }

    /// <summary>
    /// Optional inline ESAPI snapshot object used as acceptance evidence.
    /// </summary>
    public JsonElement? EsapiSnapshot { get; init; }

    /// <summary>
    /// Optional inline ESAPI snapshot JSON used as acceptance evidence.
    /// </summary>
    public string? EsapiSnapshotJson { get; init; }

    /// <summary>
    /// Managed CI-server rule-pack id to use for the accepted package. Defaults to the accepted RT-PX local package id.
    /// </summary>
    public string? RulePackId { get; init; }

    /// <summary>
    /// Actor who imported the package when different from the authenticated API actor.
    /// </summary>
    public string? ImportedBy { get; init; }

    /// <summary>
    /// Indicates whether generated rule-pack regression tests should run during import.
    /// </summary>
    public bool RunRegressionTests { get; init; } = true;

    /// <summary>
    /// Optional synthetic case id used to narrow regression testing.
    /// </summary>
    public string? SyntheticCaseId { get; init; }

    /// <summary>
    /// Indicates whether the accepted package should be promoted active after validation and safety evidence pass.
    /// </summary>
    public bool Promote { get; init; }

    /// <summary>
    /// Optional promotion or acceptance note.
    /// </summary>
    public string? Note { get; init; }

    /// <summary>
    /// Optional server-local directory for acceptance artifacts.
    /// </summary>
    public string? OutputDirectory { get; init; }

    /// <summary>
    /// Allows replacing existing acceptance output files.
    /// </summary>
    public bool Overwrite { get; init; }
}
