namespace BeamKit.Safety;

/// <summary>
/// Validation evidence package for one versioned subject such as a rule pack, adapter, or deployment.
/// </summary>
public sealed record ValidationEvidencePackage
{
    /// <summary>
    /// Creates an empty validation evidence package for JSON deserialization.
    /// </summary>
    public ValidationEvidencePackage()
    {
        Id = string.Empty;
        SubjectType = string.Empty;
        SubjectId = string.Empty;
        SubjectVersion = string.Empty;
        SubjectFingerprint = string.Empty;
        EvidenceItems = Array.Empty<ValidationEvidenceItem>();
    }

    /// <summary>
    /// Creates a validation evidence package.
    /// </summary>
    public ValidationEvidencePackage(
        string id,
        string subjectType,
        string subjectId,
        string subjectVersion,
        string subjectFingerprint,
        DateTimeOffset generatedAtUtc,
        ClinicalUseClassification intendedUse,
        IEnumerable<ValidationEvidenceItem> evidenceItems,
        SafetyControlChecklist? checklist = null,
        string? owner = null,
        string? reviewer = null,
        string? summary = null)
    {
        Id = SafetyText.Required(id, nameof(id));
        SubjectType = SafetyText.Required(subjectType, nameof(subjectType));
        SubjectId = SafetyText.Required(subjectId, nameof(subjectId));
        SubjectVersion = SafetyText.Required(subjectVersion, nameof(subjectVersion));
        SubjectFingerprint = SafetyText.Required(subjectFingerprint, nameof(subjectFingerprint));
        GeneratedAtUtc = generatedAtUtc;
        IntendedUse = intendedUse;
        Owner = SafetyText.Optional(owner);
        Reviewer = SafetyText.Optional(reviewer);
        Summary = SafetyText.Optional(summary);
        Checklist = checklist;
        EvidenceItems = evidenceItems?.ToArray() ?? throw new ArgumentNullException(nameof(evidenceItems));
    }

    /// <summary>
    /// Evidence package id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Subject type, for example RulePack.
    /// </summary>
    public string SubjectType { get; init; }

    /// <summary>
    /// Subject id.
    /// </summary>
    public string SubjectId { get; init; }

    /// <summary>
    /// Subject version id.
    /// </summary>
    public string SubjectVersion { get; init; }

    /// <summary>
    /// Subject fingerprint.
    /// </summary>
    public string SubjectFingerprint { get; init; }

    /// <summary>
    /// UTC time when the package was generated.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary>
    /// Intended use covered by the evidence.
    /// </summary>
    public ClinicalUseClassification IntendedUse { get; init; }

    /// <summary>
    /// Package owner.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Package reviewer.
    /// </summary>
    public string? Reviewer { get; init; }

    /// <summary>
    /// Package summary.
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// Optional safety-control checklist.
    /// </summary>
    public SafetyControlChecklist? Checklist { get; init; }

    /// <summary>
    /// Evidence items included in the package.
    /// </summary>
    public IReadOnlyList<ValidationEvidenceItem> EvidenceItems { get; init; }

    /// <summary>
    /// Indicates whether any evidence item has failed.
    /// </summary>
    public bool HasFailedEvidence => EvidenceItems.Any(item => item.Status == ValidationEvidenceStatus.Fail);

    /// <summary>
    /// Indicates whether the package contains passing evidence of the requested kind.
    /// </summary>
    public bool HasPassingEvidence(ValidationEvidenceKind kind)
    {
        return EvidenceItems.Any(item => item.Kind == kind && item.Status == ValidationEvidenceStatus.Pass);
    }
}
