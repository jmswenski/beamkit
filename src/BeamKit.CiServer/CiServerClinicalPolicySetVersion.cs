namespace BeamKit.CiServer;

/// <summary>
/// Immutable CI-server clinical policy set version that pins managed policy artifacts together.
/// </summary>
public sealed record CiServerClinicalPolicySetVersion
{
    /// <summary>
    /// Creates a clinical policy-set version.
    /// </summary>
    public CiServerClinicalPolicySetVersion(
        string policySetId,
        string versionId,
        DateTimeOffset importedAtUtc,
        string? importedBy,
        string name,
        string policyVersion,
        string? description,
        string? diseaseSite,
        string? technique,
        IEnumerable<string>? tags,
        string rulePackId,
        string rulePackVersionId,
        string rulePackFingerprint,
        string rulePackName,
        string rulePackVersion,
        string? namingDictionaryId,
        string? namingDictionaryVersionId,
        string? namingDictionaryFingerprint,
        string? namingDictionaryName,
        string? machineProfileId,
        string? machineProfileVersionId,
        string? machineProfileFingerprint,
        string? machineProfileName,
        string? safetyRegistryFingerprint,
        string fingerprint,
        bool isActive = false,
        DateTimeOffset? activatedAtUtc = null,
        string? activatedBy = null,
        string? activationNote = null)
    {
        PolicySetId = CiServerText.Required(policySetId, nameof(policySetId));
        VersionId = CiServerText.Required(versionId, nameof(versionId));
        ImportedAtUtc = importedAtUtc;
        ImportedBy = CiServerText.Optional(importedBy);
        Name = CiServerText.Required(name, nameof(name));
        PolicyVersion = CiServerText.Required(policyVersion, nameof(policyVersion));
        Description = CiServerText.Optional(description);
        DiseaseSite = CiServerText.Optional(diseaseSite);
        Technique = CiServerText.Optional(technique);
        Tags = tags?.Select(tag => tag.Trim()).Where(tag => tag.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            ?? Array.Empty<string>();
        RulePackId = CiServerText.Required(rulePackId, nameof(rulePackId));
        RulePackVersionId = CiServerText.Required(rulePackVersionId, nameof(rulePackVersionId));
        RulePackFingerprint = CiServerText.Required(rulePackFingerprint, nameof(rulePackFingerprint));
        RulePackName = CiServerText.Required(rulePackName, nameof(rulePackName));
        RulePackVersion = CiServerText.Required(rulePackVersion, nameof(rulePackVersion));
        NamingDictionaryId = CiServerText.Optional(namingDictionaryId);
        NamingDictionaryVersionId = CiServerText.Optional(namingDictionaryVersionId);
        NamingDictionaryFingerprint = CiServerText.Optional(namingDictionaryFingerprint);
        NamingDictionaryName = CiServerText.Optional(namingDictionaryName);
        MachineProfileId = CiServerText.Optional(machineProfileId);
        MachineProfileVersionId = CiServerText.Optional(machineProfileVersionId);
        MachineProfileFingerprint = CiServerText.Optional(machineProfileFingerprint);
        MachineProfileName = CiServerText.Optional(machineProfileName);
        SafetyRegistryFingerprint = CiServerText.Optional(safetyRegistryFingerprint);
        Fingerprint = CiServerText.Required(fingerprint, nameof(fingerprint));
        IsActive = isActive;
        ActivatedAtUtc = activatedAtUtc;
        ActivatedBy = CiServerText.Optional(activatedBy);
        ActivationNote = CiServerText.Optional(activationNote);
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
    /// Actor who created the version when supplied.
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
    /// Pinned managed rule-pack id.
    /// </summary>
    public string RulePackId { get; init; }

    /// <summary>
    /// Pinned managed rule-pack version id.
    /// </summary>
    public string RulePackVersionId { get; init; }

    /// <summary>
    /// Pinned managed rule-pack fingerprint.
    /// </summary>
    public string RulePackFingerprint { get; init; }

    /// <summary>
    /// Rule-pack display name captured at policy-set creation.
    /// </summary>
    public string RulePackName { get; init; }

    /// <summary>
    /// Rule-pack authoring version captured at policy-set creation.
    /// </summary>
    public string RulePackVersion { get; init; }

    /// <summary>
    /// Pinned naming-dictionary id, when this policy set includes naming policy.
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
    /// Naming-dictionary display name captured at policy-set creation.
    /// </summary>
    public string? NamingDictionaryName { get; init; }

    /// <summary>
    /// Pinned machine-profile id, when this policy set includes machine policy.
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
    /// Machine-profile display name captured at policy-set creation.
    /// </summary>
    public string? MachineProfileName { get; init; }

    /// <summary>
    /// Safety-registry fingerprint captured at policy-set creation.
    /// </summary>
    public string? SafetyRegistryFingerprint { get; init; }

    /// <summary>
    /// Deterministic fingerprint of the full policy-set component binding.
    /// </summary>
    public string Fingerprint { get; init; }

    /// <summary>
    /// Indicates whether this version is the active policy set for its id.
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
    public CiServerClinicalPolicySetVersionSummary ToSummary()
    {
        return new CiServerClinicalPolicySetVersionSummary(this);
    }
}
