using System.Text.Json;

namespace BeamKit.CiServer;

/// <summary>
/// Request to import a managed machine-profile version into CI-server storage.
/// </summary>
public sealed record MachineProfileImportServerRequest
{
    /// <summary>
    /// Stable id that callers use for this machine profile. Defaults to a slug of the profile name when omitted.
    /// </summary>
    public string? MachineProfileId { get; init; }

    /// <summary>
    /// Server-local machine-profile JSON path to import.
    /// </summary>
    public string? ProfilePath { get; init; }

    /// <summary>
    /// Inline machine-profile JSON object.
    /// </summary>
    public JsonElement? Profile { get; init; }

    /// <summary>
    /// Raw machine-profile JSON string.
    /// </summary>
    public string? ProfileJson { get; init; }

    /// <summary>
    /// Optional source label for inline JSON imports.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Optional searchable tags for this profile version.
    /// </summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>
    /// Actor label recorded on the imported version when supplied.
    /// </summary>
    public string? ImportedBy { get; init; }

    /// <summary>
    /// Indicates whether to promote the imported version when review has no errors.
    /// </summary>
    public bool Promote { get; init; }

    /// <summary>
    /// Optional activation note when <see cref="Promote"/> is true.
    /// </summary>
    public string? Note { get; init; }
}

/// <summary>
/// Request to promote a managed machine-profile version.
/// </summary>
public sealed record MachineProfilePromotionServerRequest
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
/// Response returned after importing a managed machine-profile version.
/// </summary>
public sealed record CiServerMachineProfileImportResult
{
    /// <summary>
    /// Creates an import result.
    /// </summary>
    public CiServerMachineProfileImportResult(
        CiServerManagedMachineProfileVersionSummary version,
        CiServerMachineProfileReviewReport review,
        bool activated)
    {
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Review = review ?? throw new ArgumentNullException(nameof(review));
        Activated = activated;
    }

    /// <summary>
    /// Imported version summary.
    /// </summary>
    public CiServerManagedMachineProfileVersionSummary Version { get; init; }

    /// <summary>
    /// Review report captured during import.
    /// </summary>
    public CiServerMachineProfileReviewReport Review { get; init; }

    /// <summary>
    /// Indicates whether the version was promoted during import.
    /// </summary>
    public bool Activated { get; init; }
}
