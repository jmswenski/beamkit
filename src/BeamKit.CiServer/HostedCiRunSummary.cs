using BeamKit.Check;

namespace BeamKit.CiServer;

/// <summary>
/// Metadata summary for a persisted hosted BeamKit CI run.
/// </summary>
public sealed record HostedCiRunSummary
{
    /// <summary>
    /// Creates a hosted CI run summary.
    /// </summary>
    public HostedCiRunSummary(
        string id,
        DateTimeOffset createdAtUtc,
        string caseId,
        CiRunInputKind inputKind,
        BeamKitCheckStatus status,
        int exitCode,
        string? inputSource,
        string? branch,
        string? commit,
        string? buildId,
        string planId,
        string rulePackName,
        string rulePackVersion,
        string planFingerprint,
        string prescriptionFingerprint,
        string rulePackFingerprint,
        bool hasPlanSnapshot = false,
        string? namingDictionaryId = null,
        string? namingDictionaryVersionId = null,
        string? namingDictionaryFingerprint = null,
        string? namingDictionaryName = null)
    {
        Id = CiServerText.Required(id, nameof(id));
        CreatedAtUtc = createdAtUtc;
        CaseId = CiServerText.Required(caseId, nameof(caseId));
        InputKind = inputKind;
        Status = status;
        ExitCode = exitCode;
        InputSource = CiServerText.Optional(inputSource);
        Branch = CiServerText.Optional(branch);
        Commit = CiServerText.Optional(commit);
        BuildId = CiServerText.Optional(buildId);
        PlanId = CiServerText.Required(planId, nameof(planId));
        RulePackName = CiServerText.Required(rulePackName, nameof(rulePackName));
        RulePackVersion = CiServerText.Required(rulePackVersion, nameof(rulePackVersion));
        PlanFingerprint = CiServerText.Required(planFingerprint, nameof(planFingerprint));
        PrescriptionFingerprint = CiServerText.Required(prescriptionFingerprint, nameof(prescriptionFingerprint));
        RulePackFingerprint = CiServerText.Required(rulePackFingerprint, nameof(rulePackFingerprint));
        HasPlanSnapshot = hasPlanSnapshot;
        NamingDictionaryId = CiServerText.Optional(namingDictionaryId);
        NamingDictionaryVersionId = CiServerText.Optional(namingDictionaryVersionId);
        NamingDictionaryFingerprint = CiServerText.Optional(namingDictionaryFingerprint);
        NamingDictionaryName = CiServerText.Optional(namingDictionaryName);
    }

    /// <summary>
    /// Server run id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// UTC timestamp when the server created the run.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Case key for the run. Synthetic runs use the synthetic case id; uploaded runs use the plan id.
    /// </summary>
    public string CaseId { get; init; }

    /// <summary>
    /// Source category for the run.
    /// </summary>
    public CiRunInputKind InputKind { get; init; }

    /// <summary>
    /// Backward-compatible alias for synthetic-only callers.
    /// </summary>
    public string SyntheticCaseId => CaseId;

    /// <summary>
    /// Top-level run status.
    /// </summary>
    public BeamKitCheckStatus Status { get; init; }

    /// <summary>
    /// Suggested process exit code.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Optional input source.
    /// </summary>
    public string? InputSource { get; init; }

    /// <summary>
    /// Optional branch.
    /// </summary>
    public string? Branch { get; init; }

    /// <summary>
    /// Optional commit.
    /// </summary>
    public string? Commit { get; init; }

    /// <summary>
    /// Optional build id.
    /// </summary>
    public string? BuildId { get; init; }

    /// <summary>
    /// Plan id.
    /// </summary>
    public string PlanId { get; init; }

    /// <summary>
    /// Rule-pack name.
    /// </summary>
    public string RulePackName { get; init; }

    /// <summary>
    /// Rule-pack version.
    /// </summary>
    public string RulePackVersion { get; init; }

    /// <summary>
    /// Plan fingerprint.
    /// </summary>
    public string PlanFingerprint { get; init; }

    /// <summary>
    /// Prescription fingerprint.
    /// </summary>
    public string PrescriptionFingerprint { get; init; }

    /// <summary>
    /// Rule-pack fingerprint.
    /// </summary>
    public string RulePackFingerprint { get; init; }

    /// <summary>
    /// Managed naming-dictionary id used for this run, when present.
    /// </summary>
    public string? NamingDictionaryId { get; init; }

    /// <summary>
    /// Managed naming-dictionary version id used for this run, when present.
    /// </summary>
    public string? NamingDictionaryVersionId { get; init; }

    /// <summary>
    /// Managed naming-dictionary fingerprint used for this run, when present.
    /// </summary>
    public string? NamingDictionaryFingerprint { get; init; }

    /// <summary>
    /// Managed naming-dictionary display name used for this run, when present.
    /// </summary>
    public string? NamingDictionaryName { get; init; }

    /// <summary>
    /// Indicates whether the server retained a vendor-neutral plan snapshot for field-level comparisons.
    /// </summary>
    public bool HasPlanSnapshot { get; init; }

    /// <summary>
    /// Creates a summary from a full hosted run record.
    /// </summary>
    public static HostedCiRunSummary FromRecord(HostedCiRunRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var provenance = record.Artifact.Provenance;
        return new HostedCiRunSummary(
            record.Id,
            record.CreatedAtUtc,
            record.CaseId,
            record.InputKind,
            record.Status,
            record.ExitCode,
            provenance.InputSource,
            provenance.Branch,
            provenance.Commit,
            provenance.BuildId,
            provenance.PlanId,
            provenance.RulePackName,
            provenance.RulePackVersion,
            provenance.PlanFingerprint,
            provenance.PrescriptionFingerprint,
            provenance.RulePackFingerprint,
            record.HasPlanSnapshot,
            provenance.NamingDictionaryId,
            provenance.NamingDictionaryVersionId,
            provenance.NamingDictionaryFingerprint,
            provenance.NamingDictionaryName);
    }
}
