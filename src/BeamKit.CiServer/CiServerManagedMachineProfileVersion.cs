using BeamKit.Deliverability;

namespace BeamKit.CiServer;

/// <summary>
/// Stored immutable source for one managed machine constraint profile version.
/// </summary>
public sealed record CiServerManagedMachineProfileVersion
{
    /// <summary>
    /// Creates a managed machine-profile version.
    /// </summary>
    public CiServerManagedMachineProfileVersion(
        string machineProfileId,
        string versionId,
        DateTimeOffset importedAtUtc,
        string? importedBy,
        string sourceKind,
        string source,
        string profileJson,
        string name,
        string profileVersion,
        string? machineId,
        string? energy,
        string? beamModelId,
        string? calculationModel,
        string? calculationModelVersion,
        IEnumerable<string>? tags,
        string fingerprint,
        CiServerMachineProfileReviewReport reviewReport,
        bool isActive = false,
        DateTimeOffset? activatedAtUtc = null,
        string? activatedBy = null,
        string? activationNote = null)
    {
        MachineProfileId = CiServerText.Required(machineProfileId, nameof(machineProfileId));
        VersionId = CiServerText.Required(versionId, nameof(versionId));
        ImportedAtUtc = importedAtUtc;
        ImportedBy = CiServerText.Optional(importedBy);
        SourceKind = CiServerText.Required(sourceKind, nameof(sourceKind));
        Source = CiServerText.Required(source, nameof(source));
        ProfileJson = CiServerText.Required(profileJson, nameof(profileJson));
        Name = CiServerText.Required(name, nameof(name));
        ProfileVersion = CiServerText.Required(profileVersion, nameof(profileVersion));
        MachineId = CiServerText.Optional(machineId);
        Energy = CiServerText.Optional(energy);
        BeamModelId = CiServerText.Optional(beamModelId);
        CalculationModel = CiServerText.Optional(calculationModel);
        CalculationModelVersion = CiServerText.Optional(calculationModelVersion);
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
    /// Stable machine-profile id used by run and policy-set requests.
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
    /// Imported machine-profile JSON.
    /// </summary>
    public string ProfileJson { get; init; }

    /// <summary>
    /// Profile display name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Profile authoring version.
    /// </summary>
    public string ProfileVersion { get; init; }

    /// <summary>
    /// Profile treatment machine id when supplied.
    /// </summary>
    public string? MachineId { get; init; }

    /// <summary>
    /// Profile energy selector when supplied.
    /// </summary>
    public string? Energy { get; init; }

    /// <summary>
    /// Profile beam model id when supplied.
    /// </summary>
    public string? BeamModelId { get; init; }

    /// <summary>
    /// Profile dose calculation model when supplied.
    /// </summary>
    public string? CalculationModel { get; init; }

    /// <summary>
    /// Profile dose calculation model version when supplied.
    /// </summary>
    public string? CalculationModelVersion { get; init; }

    /// <summary>
    /// Searchable tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; }

    /// <summary>
    /// Deterministic machine-profile fingerprint computed at import.
    /// </summary>
    public string Fingerprint { get; init; }

    /// <summary>
    /// Review report captured at import or most recent review.
    /// </summary>
    public CiServerMachineProfileReviewReport ReviewReport { get; init; }

    /// <summary>
    /// Indicates whether this version is active for its machine-profile id.
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
    public CiServerManagedMachineProfileVersionSummary ToSummary()
    {
        return new CiServerManagedMachineProfileVersionSummary(this);
    }
}
