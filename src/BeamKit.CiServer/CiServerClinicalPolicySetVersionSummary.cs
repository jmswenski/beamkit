namespace BeamKit.CiServer;

/// <summary>
/// API summary for one clinical policy-set version.
/// </summary>
public sealed record CiServerClinicalPolicySetVersionSummary
{
    /// <summary>
    /// Creates a clinical policy-set version summary.
    /// </summary>
    public CiServerClinicalPolicySetVersionSummary(CiServerClinicalPolicySetVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        PolicySetId = version.PolicySetId;
        VersionId = version.VersionId;
        ImportedAtUtc = version.ImportedAtUtc;
        ImportedBy = version.ImportedBy;
        Name = version.Name;
        PolicyVersion = version.PolicyVersion;
        Description = version.Description;
        DiseaseSite = version.DiseaseSite;
        Technique = version.Technique;
        Tags = version.Tags;
        RulePackId = version.RulePackId;
        RulePackVersionId = version.RulePackVersionId;
        RulePackFingerprint = version.RulePackFingerprint;
        RulePackName = version.RulePackName;
        RulePackVersion = version.RulePackVersion;
        NamingDictionaryId = version.NamingDictionaryId;
        NamingDictionaryVersionId = version.NamingDictionaryVersionId;
        NamingDictionaryFingerprint = version.NamingDictionaryFingerprint;
        NamingDictionaryName = version.NamingDictionaryName;
        MachineProfileId = version.MachineProfileId;
        MachineProfileVersionId = version.MachineProfileVersionId;
        MachineProfileFingerprint = version.MachineProfileFingerprint;
        MachineProfileName = version.MachineProfileName;
        SafetyRegistryFingerprint = version.SafetyRegistryFingerprint;
        Fingerprint = version.Fingerprint;
        IsActive = version.IsActive;
        ActivatedAtUtc = version.ActivatedAtUtc;
        ActivatedBy = version.ActivatedBy;
        ActivationNote = version.ActivationNote;
    }

    /// <summary>
    /// Stable policy-set id.
    /// </summary>
    public string PolicySetId { get; init; }

    /// <summary>
    /// CI-server version id.
    /// </summary>
    public string VersionId { get; init; }

    /// <summary>
    /// UTC timestamp when the version was created.
    /// </summary>
    public DateTimeOffset ImportedAtUtc { get; init; }

    /// <summary>
    /// Actor who created the version.
    /// </summary>
    public string? ImportedBy { get; init; }

    /// <summary>
    /// Policy-set display name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Institution-authored policy-set version label.
    /// </summary>
    public string PolicyVersion { get; init; }

    /// <summary>
    /// Human-readable policy-set description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Disease-site label.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Technique label.
    /// </summary>
    public string? Technique { get; init; }

    /// <summary>
    /// Searchable tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; }

    /// <summary>
    /// Pinned rule-pack id.
    /// </summary>
    public string RulePackId { get; init; }

    /// <summary>
    /// Pinned rule-pack version id.
    /// </summary>
    public string RulePackVersionId { get; init; }

    /// <summary>
    /// Pinned rule-pack fingerprint.
    /// </summary>
    public string RulePackFingerprint { get; init; }

    /// <summary>
    /// Rule-pack display name.
    /// </summary>
    public string RulePackName { get; init; }

    /// <summary>
    /// Rule-pack authoring version.
    /// </summary>
    public string RulePackVersion { get; init; }

    /// <summary>
    /// Pinned naming-dictionary id.
    /// </summary>
    public string? NamingDictionaryId { get; init; }

    /// <summary>
    /// Pinned naming-dictionary version id.
    /// </summary>
    public string? NamingDictionaryVersionId { get; init; }

    /// <summary>
    /// Pinned naming-dictionary fingerprint.
    /// </summary>
    public string? NamingDictionaryFingerprint { get; init; }

    /// <summary>
    /// Naming-dictionary display name.
    /// </summary>
    public string? NamingDictionaryName { get; init; }

    /// <summary>
    /// Pinned machine-profile id.
    /// </summary>
    public string? MachineProfileId { get; init; }

    /// <summary>
    /// Pinned machine-profile version id.
    /// </summary>
    public string? MachineProfileVersionId { get; init; }

    /// <summary>
    /// Pinned machine-profile fingerprint.
    /// </summary>
    public string? MachineProfileFingerprint { get; init; }

    /// <summary>
    /// Machine-profile display name.
    /// </summary>
    public string? MachineProfileName { get; init; }

    /// <summary>
    /// Safety-registry fingerprint captured at policy-set creation.
    /// </summary>
    public string? SafetyRegistryFingerprint { get; init; }

    /// <summary>
    /// Deterministic fingerprint of the full policy-set binding.
    /// </summary>
    public string Fingerprint { get; init; }

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
