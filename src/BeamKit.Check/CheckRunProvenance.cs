namespace BeamKit.Check;

/// <summary>
/// Transparent provenance metadata for one check run.
/// </summary>
public sealed record CheckRunProvenance
{
    /// <summary>
    /// Creates check-run provenance.
    /// </summary>
    public CheckRunProvenance(
        string runId,
        string planId,
        string patientId,
        string planFingerprint,
        string prescriptionFingerprint,
        string rulePackName,
        string rulePackVersion,
        string rulePackFingerprint,
        BeamKitCheckStatus status,
        DateTimeOffset generatedAtUtc,
        string? inputSource = null,
        string? branch = null,
        string? commit = null,
        string? buildId = null,
        string? namingDictionaryId = null,
        string? namingDictionaryVersionId = null,
        string? namingDictionaryFingerprint = null,
        string? namingDictionaryName = null)
    {
        RunId = CheckText.Required(runId, nameof(runId));
        PlanId = CheckText.Required(planId, nameof(planId));
        PatientId = CheckText.Required(patientId, nameof(patientId));
        PlanFingerprint = CheckText.Required(planFingerprint, nameof(planFingerprint));
        PrescriptionFingerprint = CheckText.Required(prescriptionFingerprint, nameof(prescriptionFingerprint));
        RulePackName = CheckText.Required(rulePackName, nameof(rulePackName));
        RulePackVersion = CheckText.Required(rulePackVersion, nameof(rulePackVersion));
        RulePackFingerprint = CheckText.Required(rulePackFingerprint, nameof(rulePackFingerprint));
        Status = status;
        GeneratedAtUtc = generatedAtUtc;
        InputSource = CheckText.Optional(inputSource);
        Branch = CheckText.Optional(branch);
        Commit = CheckText.Optional(commit);
        BuildId = CheckText.Optional(buildId);
        NamingDictionaryId = CheckText.Optional(namingDictionaryId);
        NamingDictionaryVersionId = CheckText.Optional(namingDictionaryVersionId);
        NamingDictionaryFingerprint = CheckText.Optional(namingDictionaryFingerprint);
        NamingDictionaryName = CheckText.Optional(namingDictionaryName);
    }

    /// <summary>
    /// Stable run id.
    /// </summary>
    public string RunId { get; init; }

    /// <summary>
    /// Plan id.
    /// </summary>
    public string PlanId { get; init; }

    /// <summary>
    /// Patient id.
    /// </summary>
    public string PatientId { get; init; }

    /// <summary>
    /// Deterministic plan fingerprint.
    /// </summary>
    public string PlanFingerprint { get; init; }

    /// <summary>
    /// Deterministic prescription fingerprint.
    /// </summary>
    public string PrescriptionFingerprint { get; init; }

    /// <summary>
    /// Rule-pack name.
    /// </summary>
    public string RulePackName { get; init; }

    /// <summary>
    /// Rule-pack version.
    /// </summary>
    public string RulePackVersion { get; init; }

    /// <summary>
    /// Deterministic rule-pack fingerprint.
    /// </summary>
    public string RulePackFingerprint { get; init; }

    /// <summary>
    /// Top-level check status.
    /// </summary>
    public BeamKitCheckStatus Status { get; init; }

    /// <summary>
    /// UTC timestamp when provenance was generated.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary>
    /// Optional input source.
    /// </summary>
    public string? InputSource { get; init; }

    /// <summary>
    /// Optional source-control branch.
    /// </summary>
    public string? Branch { get; init; }

    /// <summary>
    /// Optional source-control commit.
    /// </summary>
    public string? Commit { get; init; }

    /// <summary>
    /// Optional CI build id.
    /// </summary>
    public string? BuildId { get; init; }

    /// <summary>
    /// Managed naming-dictionary id used for this run, when supplied by the caller.
    /// </summary>
    public string? NamingDictionaryId { get; init; }

    /// <summary>
    /// Managed naming-dictionary version id used for this run, when supplied by the caller.
    /// </summary>
    public string? NamingDictionaryVersionId { get; init; }

    /// <summary>
    /// Managed naming-dictionary fingerprint used for this run, when supplied by the caller.
    /// </summary>
    public string? NamingDictionaryFingerprint { get; init; }

    /// <summary>
    /// Managed naming-dictionary display name used for this run, when supplied by the caller.
    /// </summary>
    public string? NamingDictionaryName { get; init; }
}
