using BeamKit.Check;

namespace BeamKit.CiServer;

/// <summary>
/// Promoted CI run metadata used as the approved baseline for a case key.
/// </summary>
public sealed record CiRunBaseline
{
    /// <summary>
    /// Creates a CI run baseline.
    /// </summary>
    public CiRunBaseline(
        string caseId,
        string baselineRunId,
        DateTimeOffset promotedAtUtc,
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
        string? promotedBy = null,
        string? note = null,
        string? namingDictionaryId = null,
        string? namingDictionaryVersionId = null,
        string? namingDictionaryFingerprint = null,
        string? namingDictionaryName = null)
    {
        CaseId = CiServerText.Required(caseId, nameof(caseId));
        BaselineRunId = CiServerText.Required(baselineRunId, nameof(baselineRunId));
        PromotedAtUtc = promotedAtUtc;
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
        PromotedBy = CiServerText.Optional(promotedBy);
        Note = CiServerText.Optional(note);
        NamingDictionaryId = CiServerText.Optional(namingDictionaryId);
        NamingDictionaryVersionId = CiServerText.Optional(namingDictionaryVersionId);
        NamingDictionaryFingerprint = CiServerText.Optional(namingDictionaryFingerprint);
        NamingDictionaryName = CiServerText.Optional(namingDictionaryName);
    }

    /// <summary>
    /// Case key covered by this baseline.
    /// </summary>
    public string CaseId { get; init; }

    /// <summary>
    /// Run id that was promoted as the baseline.
    /// </summary>
    public string BaselineRunId { get; init; }

    /// <summary>
    /// UTC timestamp when the baseline was promoted.
    /// </summary>
    public DateTimeOffset PromotedAtUtc { get; init; }

    /// <summary>
    /// User or automation that promoted the baseline.
    /// </summary>
    public string? PromotedBy { get; init; }

    /// <summary>
    /// Optional promotion note.
    /// </summary>
    public string? Note { get; init; }

    /// <summary>
    /// Source category for the promoted run.
    /// </summary>
    public CiRunInputKind InputKind { get; init; }

    /// <summary>
    /// Baseline run status.
    /// </summary>
    public BeamKitCheckStatus Status { get; init; }

    /// <summary>
    /// Baseline exit code.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Optional baseline input source.
    /// </summary>
    public string? InputSource { get; init; }

    /// <summary>
    /// Optional baseline branch.
    /// </summary>
    public string? Branch { get; init; }

    /// <summary>
    /// Optional baseline commit.
    /// </summary>
    public string? Commit { get; init; }

    /// <summary>
    /// Optional baseline build id.
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
    /// Baseline plan fingerprint.
    /// </summary>
    public string PlanFingerprint { get; init; }

    /// <summary>
    /// Baseline prescription fingerprint.
    /// </summary>
    public string PrescriptionFingerprint { get; init; }

    /// <summary>
    /// Baseline rule-pack fingerprint.
    /// </summary>
    public string RulePackFingerprint { get; init; }

    /// <summary>
    /// Managed naming-dictionary id used for the baseline run, when present.
    /// </summary>
    public string? NamingDictionaryId { get; init; }

    /// <summary>
    /// Managed naming-dictionary version id used for the baseline run, when present.
    /// </summary>
    public string? NamingDictionaryVersionId { get; init; }

    /// <summary>
    /// Managed naming-dictionary fingerprint used for the baseline run, when present.
    /// </summary>
    public string? NamingDictionaryFingerprint { get; init; }

    /// <summary>
    /// Managed naming-dictionary display name used for the baseline run, when present.
    /// </summary>
    public string? NamingDictionaryName { get; init; }

    /// <summary>
    /// Creates a baseline from a stored CI run summary.
    /// </summary>
    public static CiRunBaseline FromRun(HostedCiRunSummary run, DateTimeOffset promotedAtUtc, string? promotedBy = null, string? note = null)
    {
        ArgumentNullException.ThrowIfNull(run);

        return new CiRunBaseline(
            run.CaseId,
            run.Id,
            promotedAtUtc,
            run.InputKind,
            run.Status,
            run.ExitCode,
            run.InputSource,
            run.Branch,
            run.Commit,
            run.BuildId,
            run.PlanId,
            run.RulePackName,
            run.RulePackVersion,
            run.PlanFingerprint,
            run.PrescriptionFingerprint,
            run.RulePackFingerprint,
            promotedBy,
            note,
            run.NamingDictionaryId,
            run.NamingDictionaryVersionId,
            run.NamingDictionaryFingerprint,
            run.NamingDictionaryName);
    }
}
