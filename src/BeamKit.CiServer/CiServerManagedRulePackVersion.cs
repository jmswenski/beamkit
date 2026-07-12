using BeamKit.Check;

namespace BeamKit.CiServer;

/// <summary>
/// Stored immutable source for one managed CI-server rule-pack version.
/// </summary>
public sealed record CiServerManagedRulePackVersion
{
    /// <summary>
    /// Creates a managed rule-pack version.
    /// </summary>
    public CiServerManagedRulePackVersion(
        string rulePackId,
        string versionId,
        DateTimeOffset importedAtUtc,
        string? importedBy,
        string sourceKind,
        string source,
        string baseDirectory,
        string manifestJson,
        string name,
        string version,
        string? owner,
        string? description,
        string? diseaseSite,
        IEnumerable<string>? tags,
        string fingerprint,
        RulePackValidationReport validationReport,
        RulePackTestReport? testReport = null,
        bool isActive = false,
        DateTimeOffset? activatedAtUtc = null,
        string? activatedBy = null,
        string? activationNote = null,
        string? bundleJson = null,
        string? safetyEvidenceJson = null)
    {
        RulePackId = CiServerText.Required(rulePackId, nameof(rulePackId));
        VersionId = CiServerText.Required(versionId, nameof(versionId));
        ImportedAtUtc = importedAtUtc;
        ImportedBy = CiServerText.Optional(importedBy);
        SourceKind = CiServerText.Required(sourceKind, nameof(sourceKind));
        Source = CiServerText.Required(source, nameof(source));
        BaseDirectory = CiServerText.Required(baseDirectory, nameof(baseDirectory));
        ManifestJson = CiServerText.Required(manifestJson, nameof(manifestJson));
        Name = CiServerText.Required(name, nameof(name));
        Version = CiServerText.Required(version, nameof(version));
        Owner = CiServerText.Optional(owner);
        Description = CiServerText.Optional(description);
        DiseaseSite = CiServerText.Optional(diseaseSite);
        Tags = tags?.Select(tag => tag.Trim()).Where(tag => tag.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            ?? Array.Empty<string>();
        Fingerprint = CiServerText.Required(fingerprint, nameof(fingerprint));
        ValidationReport = validationReport ?? throw new ArgumentNullException(nameof(validationReport));
        TestReport = testReport;
        IsActive = isActive;
        ActivatedAtUtc = activatedAtUtc;
        ActivatedBy = CiServerText.Optional(activatedBy);
        ActivationNote = CiServerText.Optional(activationNote);
        BundleJson = CiServerText.Optional(bundleJson);
        SafetyEvidenceJson = CiServerText.Optional(safetyEvidenceJson);
    }

    /// <summary>
    /// Stable rule-pack id used by CI run requests.
    /// </summary>
    public string RulePackId { get; init; }

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
    /// Base directory used to resolve manifest file references.
    /// </summary>
    public string BaseDirectory { get; init; }

    /// <summary>
    /// Imported rule-pack manifest JSON.
    /// </summary>
    public string ManifestJson { get; init; }

    /// <summary>
    /// Optional immutable release bundle JSON. New imports store this so active versions no longer depend on source files.
    /// </summary>
    public string? BundleJson { get; init; }

    /// <summary>
    /// Optional serialized safety and validation evidence captured for promotion.
    /// </summary>
    public string? SafetyEvidenceJson { get; init; }

    /// <summary>
    /// Rule-pack name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Rule-pack authoring version.
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Rule-pack owner.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Rule-pack description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Disease-site label.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Searchable tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; }

    /// <summary>
    /// Deterministic policy fingerprint computed at import.
    /// </summary>
    public string Fingerprint { get; init; }

    /// <summary>
    /// Validation report captured at import or most recent validation.
    /// </summary>
    public RulePackValidationReport ValidationReport { get; init; }

    /// <summary>
    /// Regression-test report captured at import or most recent test run.
    /// </summary>
    public RulePackTestReport? TestReport { get; init; }

    /// <summary>
    /// Indicates whether this version is the active version for its rule-pack id.
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
    public CiServerManagedRulePackVersionSummary ToSummary()
    {
        return new CiServerManagedRulePackVersionSummary(this);
    }
}
