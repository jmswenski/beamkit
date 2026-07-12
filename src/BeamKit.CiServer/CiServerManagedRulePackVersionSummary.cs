namespace BeamKit.CiServer;

/// <summary>
/// API summary for one managed rule-pack version.
/// </summary>
public sealed record CiServerManagedRulePackVersionSummary
{
    /// <summary>
    /// Creates a managed rule-pack version summary.
    /// </summary>
    public CiServerManagedRulePackVersionSummary(CiServerManagedRulePackVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        RulePackId = version.RulePackId;
        VersionId = version.VersionId;
        ImportedAtUtc = version.ImportedAtUtc;
        ImportedBy = version.ImportedBy;
        SourceKind = version.SourceKind;
        Source = version.Source;
        Name = version.Name;
        Version = version.Version;
        Owner = version.Owner;
        Description = version.Description;
        DiseaseSite = version.DiseaseSite;
        Tags = version.Tags;
        Fingerprint = version.Fingerprint;
        IsValid = version.ValidationReport.IsValid;
        ValidationErrorCount = version.ValidationReport.ErrorCount;
        ValidationWarningCount = version.ValidationReport.WarningCount;
        TestPassed = version.TestReport?.Passed;
        TestPassedCount = version.TestReport?.PassedCount;
        TestFailedCount = version.TestReport?.FailedCount;
        IsActive = version.IsActive;
        ActivatedAtUtc = version.ActivatedAtUtc;
        ActivatedBy = version.ActivatedBy;
        ActivationNote = version.ActivationNote;
        HasSafetyEvidence = !string.IsNullOrWhiteSpace(version.SafetyEvidenceJson);
    }

    /// <summary>
    /// Stable rule-pack id.
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
    /// Deterministic policy fingerprint.
    /// </summary>
    public string Fingerprint { get; init; }

    /// <summary>
    /// Indicates whether policy validation passed.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Policy validation error count.
    /// </summary>
    public int ValidationErrorCount { get; init; }

    /// <summary>
    /// Policy validation warning count.
    /// </summary>
    public int ValidationWarningCount { get; init; }

    /// <summary>
    /// Indicates whether the most recent regression test passed.
    /// </summary>
    public bool? TestPassed { get; init; }

    /// <summary>
    /// Number of passed regression test cases.
    /// </summary>
    public int? TestPassedCount { get; init; }

    /// <summary>
    /// Number of failed regression test cases.
    /// </summary>
    public int? TestFailedCount { get; init; }

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

    /// <summary>
    /// Indicates whether this version has stored safety and validation evidence.
    /// </summary>
    public bool HasSafetyEvidence { get; init; }
}
