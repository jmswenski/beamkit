using System.Text.Json;
using BeamKit.Naming;

namespace BeamKit.CiServer;

/// <summary>
/// Request to import a managed naming-dictionary version into CI-server storage.
/// </summary>
public sealed record NamingDictionaryImportServerRequest
{
    /// <summary>
    /// Stable id that callers use for this dictionary. Defaults to the dictionary JSON id when omitted.
    /// </summary>
    public string? DictionaryId { get; init; }

    /// <summary>
    /// Server-local dictionary JSON path to import.
    /// </summary>
    public string? DictionaryPath { get; init; }

    /// <summary>
    /// Inline dictionary JSON object.
    /// </summary>
    public JsonElement? Dictionary { get; init; }

    /// <summary>
    /// Raw dictionary JSON string.
    /// </summary>
    public string? DictionaryJson { get; init; }

    /// <summary>
    /// Optional source label for inline JSON imports.
    /// </summary>
    public string? Source { get; init; }

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
/// Request to promote a managed naming-dictionary version.
/// </summary>
public sealed record NamingDictionaryPromotionServerRequest
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
/// Response returned after importing a managed naming-dictionary version.
/// </summary>
public sealed record CiServerNamingDictionaryImportResult
{
    /// <summary>
    /// Creates an import result.
    /// </summary>
    public CiServerNamingDictionaryImportResult(
        CiServerManagedNamingDictionaryVersionSummary version,
        StructureNameDictionaryReviewReport review,
        bool activated)
    {
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Review = review ?? throw new ArgumentNullException(nameof(review));
        Activated = activated;
    }

    /// <summary>
    /// Imported version summary.
    /// </summary>
    public CiServerManagedNamingDictionaryVersionSummary Version { get; init; }

    /// <summary>
    /// Review report captured during import.
    /// </summary>
    public StructureNameDictionaryReviewReport Review { get; init; }

    /// <summary>
    /// Indicates whether the version was promoted during import.
    /// </summary>
    public bool Activated { get; init; }
}

/// <summary>
/// Response for reviewing a naming-dictionary draft before import.
/// </summary>
public sealed record CiServerNamingDictionaryDraftReviewResult
{
    /// <summary>
    /// Creates a naming-dictionary draft review result.
    /// </summary>
    public CiServerNamingDictionaryDraftReviewResult(
        string dictionaryId,
        string? baselineVersionId,
        StructureNameDictionaryReviewReport review,
        StructureNameDictionaryDiffReport? diff)
    {
        DictionaryId = CiServerText.Required(dictionaryId, nameof(dictionaryId));
        BaselineVersionId = CiServerText.Optional(baselineVersionId);
        Review = review ?? throw new ArgumentNullException(nameof(review));
        Diff = diff;
    }

    /// <summary>
    /// Stable dictionary id reviewed.
    /// </summary>
    public string DictionaryId { get; init; }

    /// <summary>
    /// Active baseline version id used for diffing, when available.
    /// </summary>
    public string? BaselineVersionId { get; init; }

    /// <summary>
    /// Draft dictionary review report.
    /// </summary>
    public StructureNameDictionaryReviewReport Review { get; init; }

    /// <summary>
    /// Diff against the active version, when one exists.
    /// </summary>
    public StructureNameDictionaryDiffReport? Diff { get; init; }

    /// <summary>
    /// Indicates whether the draft can be promoted once imported.
    /// </summary>
    public bool IsPromotable => Review.IsValid;
}
