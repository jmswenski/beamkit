namespace BeamKit.CiServer;

/// <summary>
/// API summary for one managed machine-profile version.
/// </summary>
public sealed record CiServerManagedMachineProfileVersionSummary
{
    /// <summary>
    /// Creates a managed machine-profile version summary.
    /// </summary>
    public CiServerManagedMachineProfileVersionSummary(CiServerManagedMachineProfileVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        MachineProfileId = version.MachineProfileId;
        VersionId = version.VersionId;
        ImportedAtUtc = version.ImportedAtUtc;
        ImportedBy = version.ImportedBy;
        SourceKind = version.SourceKind;
        Source = version.Source;
        Name = version.Name;
        ProfileVersion = version.ProfileVersion;
        MachineId = version.MachineId;
        Energy = version.Energy;
        BeamModelId = version.BeamModelId;
        CalculationModel = version.CalculationModel;
        CalculationModelVersion = version.CalculationModelVersion;
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
    /// Stable machine-profile id.
    /// </summary>
    public string MachineProfileId { get; init; }

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
    /// Profile display name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Profile authoring version.
    /// </summary>
    public string ProfileVersion { get; init; }

    /// <summary>
    /// Treatment machine id when supplied.
    /// </summary>
    public string? MachineId { get; init; }

    /// <summary>
    /// Energy selector when supplied.
    /// </summary>
    public string? Energy { get; init; }

    /// <summary>
    /// Beam model id when supplied.
    /// </summary>
    public string? BeamModelId { get; init; }

    /// <summary>
    /// Dose calculation model when supplied.
    /// </summary>
    public string? CalculationModel { get; init; }

    /// <summary>
    /// Dose calculation model version when supplied.
    /// </summary>
    public string? CalculationModelVersion { get; init; }

    /// <summary>
    /// Searchable tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; }

    /// <summary>
    /// Deterministic profile fingerprint.
    /// </summary>
    public string Fingerprint { get; init; }

    /// <summary>
    /// Indicates whether profile review found no blocking issues.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Profile review error count.
    /// </summary>
    public int ReviewErrorCount { get; init; }

    /// <summary>
    /// Profile review warning count.
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
