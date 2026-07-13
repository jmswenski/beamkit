namespace BeamKit.CiServer;

/// <summary>
/// API summary for one managed naming-dictionary version.
/// </summary>
public sealed record CiServerManagedNamingDictionaryVersionSummary
{
    /// <summary>
    /// Creates a managed naming-dictionary version summary.
    /// </summary>
    public CiServerManagedNamingDictionaryVersionSummary(CiServerManagedNamingDictionaryVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        DictionaryId = version.DictionaryId;
        VersionId = version.VersionId;
        ImportedAtUtc = version.ImportedAtUtc;
        ImportedBy = version.ImportedBy;
        SourceKind = version.SourceKind;
        Source = version.Source;
        Name = version.Name;
        DictionaryVersion = version.DictionaryVersion;
        Description = version.Description;
        DictionarySource = version.DictionarySource;
        Tags = version.Tags;
        Fingerprint = version.Fingerprint;
        IsValid = version.ReviewReport.IsValid;
        ReviewErrorCount = version.ReviewReport.ErrorCount;
        ReviewWarningCount = version.ReviewReport.WarningCount;
        IsActive = version.IsActive;
        ActivatedAtUtc = version.ActivatedAtUtc;
        ActivatedBy = version.ActivatedBy;
        ActivationNote = version.ActivationNote;
    }

    /// <summary>
    /// Stable dictionary id.
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
    /// Actor who imported the version.
    /// </summary>
    public string? ImportedBy { get; init; }

    /// <summary>
    /// Source kind.
    /// </summary>
    public string SourceKind { get; init; }

    /// <summary>
    /// Source path or label.
    /// </summary>
    public string Source { get; init; }

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
    /// Dictionary source label.
    /// </summary>
    public string? DictionarySource { get; init; }

    /// <summary>
    /// Searchable tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; }

    /// <summary>
    /// Deterministic dictionary fingerprint.
    /// </summary>
    public string Fingerprint { get; init; }

    /// <summary>
    /// Indicates whether dictionary review found no blocking issues.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Dictionary review error count.
    /// </summary>
    public int ReviewErrorCount { get; init; }

    /// <summary>
    /// Dictionary review warning count.
    /// </summary>
    public int ReviewWarningCount { get; init; }

    /// <summary>
    /// Indicates whether this is the active version.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// UTC timestamp when the version was activated.
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
}
