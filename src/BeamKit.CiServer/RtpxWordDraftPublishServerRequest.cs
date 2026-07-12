using System.Text.Json;

namespace BeamKit.CiServer;

/// <summary>
/// Request to extract a Word-authored RT-PX protocol and publish it as a draft managed version.
/// </summary>
public sealed record RtpxWordDraftPublishServerRequest
{
    /// <summary>
    /// Optional server-local `.docx` path.
    /// </summary>
    public string? DocxPath { get; init; }

    /// <summary>
    /// Base64-encoded `.docx` content from an Office add-in or API client.
    /// </summary>
    public string? DocxBase64 { get; init; }

    /// <summary>
    /// Original file name supplied by the client.
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// Indicates whether the generated draft package should embed the source Word document.
    /// </summary>
    public bool IncludeSourceDocument { get; init; }

    /// <summary>
    /// Optional server-local path to an institution acceptance profile JSON file.
    /// </summary>
    public string? InstitutionProfilePath { get; init; }

    /// <summary>
    /// Optional inline institution acceptance profile object.
    /// </summary>
    public JsonElement? InstitutionProfile { get; init; }

    /// <summary>
    /// Optional inline institution acceptance profile JSON.
    /// </summary>
    public string? InstitutionProfileJson { get; init; }

    /// <summary>
    /// Managed rule-pack id for the draft. Defaults to the accepted local RT-PX package id.
    /// </summary>
    public string? RulePackId { get; init; }

    /// <summary>
    /// Actor who imported the draft when different from the authenticated API actor.
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
    /// Optional note retained in audit and import metadata.
    /// </summary>
    public string? Note { get; init; }

    /// <summary>
    /// Optional server-local directory for generated draft artifacts.
    /// </summary>
    public string? OutputDirectory { get; init; }

    /// <summary>
    /// Optional opaque client context for add-in callers.
    /// </summary>
    public JsonElement? ClientContext { get; init; }
}
