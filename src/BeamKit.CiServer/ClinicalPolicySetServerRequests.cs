namespace BeamKit.CiServer;

/// <summary>
/// Request to create a clinical policy-set version from managed CI-server artifacts.
/// </summary>
public sealed record ClinicalPolicySetImportServerRequest
{
    /// <summary>
    /// Stable policy-set id.
    /// </summary>
    public string? PolicySetId { get; init; }

    /// <summary>
    /// Policy-set display name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Institution-authored version label. Defaults to the rule-pack version when omitted.
    /// </summary>
    public string? PolicyVersion { get; init; }

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
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>
    /// Managed rule-pack id to pin.
    /// </summary>
    public string? RulePackId { get; init; }

    /// <summary>
    /// Managed rule-pack version id to pin. Defaults to the active version for <see cref="RulePackId"/>.
    /// </summary>
    public string? RulePackVersionId { get; init; }

    /// <summary>
    /// Managed naming-dictionary id to pin.
    /// </summary>
    public string? NamingDictionaryId { get; init; }

    /// <summary>
    /// Managed naming-dictionary version id to pin. Defaults to the active version for <see cref="NamingDictionaryId"/>.
    /// </summary>
    public string? NamingDictionaryVersionId { get; init; }

    /// <summary>
    /// Managed machine-profile id to pin.
    /// </summary>
    public string? MachineProfileId { get; init; }

    /// <summary>
    /// Managed machine-profile version id to pin. Defaults to the active version for <see cref="MachineProfileId"/>.
    /// </summary>
    public string? MachineProfileVersionId { get; init; }

    /// <summary>
    /// Actor label recorded on the imported version when supplied.
    /// </summary>
    public string? ImportedBy { get; init; }

    /// <summary>
    /// Indicates whether to promote the policy-set version after creation.
    /// </summary>
    public bool Promote { get; init; }

    /// <summary>
    /// Optional activation note when <see cref="Promote"/> is true.
    /// </summary>
    public string? Note { get; init; }
}

/// <summary>
/// Request to promote a clinical policy-set version.
/// </summary>
public sealed record ClinicalPolicySetPromotionServerRequest
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
/// Response returned after creating a clinical policy-set version.
/// </summary>
public sealed record CiServerClinicalPolicySetImportResult
{
    /// <summary>
    /// Creates a policy-set import result.
    /// </summary>
    public CiServerClinicalPolicySetImportResult(CiServerClinicalPolicySetVersionSummary version, bool activated)
    {
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Activated = activated;
    }

    /// <summary>
    /// Created policy-set version summary.
    /// </summary>
    public CiServerClinicalPolicySetVersionSummary Version { get; init; }

    /// <summary>
    /// Indicates whether the version was promoted during creation.
    /// </summary>
    public bool Activated { get; init; }
}
