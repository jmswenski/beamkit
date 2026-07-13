using BeamKit.Naming;

namespace BeamKit.CiServer;

/// <summary>
/// Stored immutable source for one managed CI-server naming-dictionary version.
/// </summary>
public sealed record CiServerManagedNamingDictionaryVersion
{
    /// <summary>
    /// Creates a managed naming-dictionary version.
    /// </summary>
    public CiServerManagedNamingDictionaryVersion(
        string dictionaryId,
        string versionId,
        DateTimeOffset importedAtUtc,
        string? importedBy,
        string sourceKind,
        string source,
        string dictionaryJson,
        string name,
        string? dictionaryVersion,
        string? description,
        string? dictionarySource,
        IEnumerable<string>? tags,
        string fingerprint,
        StructureNameDictionaryReviewReport reviewReport,
        bool isActive = false,
        DateTimeOffset? activatedAtUtc = null,
        string? activatedBy = null,
        string? activationNote = null)
    {
        DictionaryId = CiServerText.Required(dictionaryId, nameof(dictionaryId));
        VersionId = CiServerText.Required(versionId, nameof(versionId));
        ImportedAtUtc = importedAtUtc;
        ImportedBy = CiServerText.Optional(importedBy);
        SourceKind = CiServerText.Required(sourceKind, nameof(sourceKind));
        Source = CiServerText.Required(source, nameof(source));
        DictionaryJson = CiServerText.Required(dictionaryJson, nameof(dictionaryJson));
        Name = CiServerText.Required(name, nameof(name));
        DictionaryVersion = CiServerText.Optional(dictionaryVersion);
        Description = CiServerText.Optional(description);
        DictionarySource = CiServerText.Optional(dictionarySource);
        Tags = tags?.Select(tag => tag.Trim()).Where(tag => tag.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            ?? Array.Empty<string>();
        Fingerprint = CiServerText.Required(fingerprint, nameof(fingerprint));
        ReviewReport = reviewReport ?? throw new ArgumentNullException(nameof(reviewReport));
        IsActive = isActive;
        ActivatedAtUtc = activatedAtUtc;
        ActivatedBy = CiServerText.Optional(activatedBy);
        ActivationNote = CiServerText.Optional(activationNote);
    }

    /// <summary>
    /// Stable dictionary id used by CI-server normalization policy.
    /// </summary>
    public string DictionaryId { get; init; }

    /// <summary>
    /// CI-server version id.
    /// </summary>
    public string VersionId { get; init; }

    /// <summary>
    /// UTC timestamp when the version was imported.
    /// </summary>
    public DateTimeOffset ImportedAtUtc { get; init; }

    /// <summary>
    /// Actor who imported the version when supplied.
    /// </summary>
    public string? ImportedBy { get; init; }

    /// <summary>
    /// Source kind, such as File or InlineJson.
    /// </summary>
    public string SourceKind { get; init; }

    /// <summary>
    /// Source path or label.
    /// </summary>
    public string Source { get; init; }

    /// <summary>
    /// Imported structure-name dictionary JSON.
    /// </summary>
    public string DictionaryJson { get; init; }

    /// <summary>
    /// Dictionary display name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Dictionary authoring version.
    /// </summary>
    public string? DictionaryVersion { get; init; }

    /// <summary>
    /// Human-readable dictionary description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Dictionary source label, such as TG-263, institution, physician, or protocol.
    /// </summary>
    public string? DictionarySource { get; init; }

    /// <summary>
    /// Searchable tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; }

    /// <summary>
    /// Deterministic dictionary fingerprint computed at import.
    /// </summary>
    public string Fingerprint { get; init; }

    /// <summary>
    /// Review report captured at import or most recent review.
    /// </summary>
    public StructureNameDictionaryReviewReport ReviewReport { get; init; }

    /// <summary>
    /// Indicates whether this version is the active naming dictionary for its id.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// UTC timestamp when this version was activated.
    /// </summary>
    public DateTimeOffset? ActivatedAtUtc { get; init; }

    /// <summary>
    /// Actor who activated this version.
    /// </summary>
    public string? ActivatedBy { get; init; }

    /// <summary>
    /// Activation note.
    /// </summary>
    public string? ActivationNote { get; init; }

    /// <summary>
    /// Creates an API-safe summary.
    /// </summary>
    public CiServerManagedNamingDictionaryVersionSummary ToSummary()
    {
        return new CiServerManagedNamingDictionaryVersionSummary(this);
    }
}
