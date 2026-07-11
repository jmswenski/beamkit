using System.Text.Json;

namespace BeamKit.CiServer;

/// <summary>
/// Request to import a managed rule-pack version into CI-server storage.
/// </summary>
public sealed record RulePackImportServerRequest
{
    /// <summary>
    /// Stable id that callers use in <c>rulePackId</c>.
    /// </summary>
    public string? RulePackId { get; init; }

    /// <summary>
    /// Server-local manifest path to import.
    /// </summary>
    public string? ManifestPath { get; init; }

    /// <summary>
    /// Inline manifest JSON object.
    /// </summary>
    public JsonElement? Manifest { get; init; }

    /// <summary>
    /// Raw manifest JSON string.
    /// </summary>
    public string? ManifestJson { get; init; }

    /// <summary>
    /// Base directory used to resolve relative manifest file references for inline JSON.
    /// </summary>
    public string? BaseDirectory { get; init; }

    /// <summary>
    /// Optional source label for inline JSON imports.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Actor label recorded on the imported version when supplied.
    /// </summary>
    public string? ImportedBy { get; init; }

    /// <summary>
    /// Indicates whether to run regression tests during import.
    /// </summary>
    public bool RunRegressionTests { get; init; } = true;

    /// <summary>
    /// Optional synthetic case id for a focused regression test.
    /// </summary>
    public string? SyntheticCaseId { get; init; }

    /// <summary>
    /// Indicates whether to promote the imported version when validation and tests pass.
    /// </summary>
    public bool Promote { get; init; }

    /// <summary>
    /// Optional activation note when <see cref="Promote"/> is true.
    /// </summary>
    public string? Note { get; init; }
}

/// <summary>
/// Request to promote a managed rule-pack version.
/// </summary>
public sealed record RulePackPromotionServerRequest
{
    /// <summary>
    /// Actor who promoted the version.
    /// </summary>
    public string? PromotedBy { get; init; }

    /// <summary>
    /// Promotion note.
    /// </summary>
    public string? Note { get; init; }
}

/// <summary>
/// Request to regression-test a managed rule-pack version.
/// </summary>
public sealed record RulePackVersionTestServerRequest
{
    /// <summary>
    /// Optional synthetic case id. The default regression suite is used when omitted.
    /// </summary>
    public string? SyntheticCaseId { get; init; }
}
