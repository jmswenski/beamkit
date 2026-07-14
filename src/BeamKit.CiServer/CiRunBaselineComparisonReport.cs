using BeamKit.ChangeDetection;

namespace BeamKit.CiServer;

/// <summary>
/// Result of comparing one CI run to the promoted baseline for the same case key.
/// </summary>
public sealed record CiRunBaselineComparisonReport
{
    /// <summary>
    /// Creates a CI baseline comparison report.
    /// </summary>
    public CiRunBaselineComparisonReport(
        string caseId,
        string baselineRunId,
        string comparisonRunId,
        DateTimeOffset comparedAtUtc,
        CiRunBaseline baseline,
        HostedCiRunSummary comparison,
        bool usedPlanSnapshotComparison = false,
        PlanChangeReport? planChanges = null,
        IEnumerable<CiRunBaselineFinding>? findings = null)
    {
        CaseId = CiServerText.Required(caseId, nameof(caseId));
        BaselineRunId = CiServerText.Required(baselineRunId, nameof(baselineRunId));
        ComparisonRunId = CiServerText.Required(comparisonRunId, nameof(comparisonRunId));
        ComparedAtUtc = comparedAtUtc;
        Baseline = baseline ?? throw new ArgumentNullException(nameof(baseline));
        Comparison = comparison ?? throw new ArgumentNullException(nameof(comparison));
        UsedPlanSnapshotComparison = usedPlanSnapshotComparison;
        PlanChanges = planChanges;
        Findings = findings?.ToArray() ?? Array.Empty<CiRunBaselineFinding>();
    }

    /// <summary>
    /// Case key being compared.
    /// </summary>
    public string CaseId { get; init; }

    /// <summary>
    /// Promoted baseline run id.
    /// </summary>
    public string BaselineRunId { get; init; }

    /// <summary>
    /// Comparison run id.
    /// </summary>
    public string ComparisonRunId { get; init; }

    /// <summary>
    /// UTC timestamp when the comparison report was generated.
    /// </summary>
    public DateTimeOffset ComparedAtUtc { get; init; }

    /// <summary>
    /// Promoted baseline metadata.
    /// </summary>
    public CiRunBaseline Baseline { get; init; }

    /// <summary>
    /// Comparison run metadata.
    /// </summary>
    public HostedCiRunSummary Comparison { get; init; }

    /// <summary>
    /// Indicates whether the comparison included field-level plan snapshot analysis.
    /// </summary>
    public bool UsedPlanSnapshotComparison { get; init; }

    /// <summary>
    /// Field-level plan changes when both runs had stored vendor-neutral snapshots.
    /// </summary>
    public PlanChangeReport? PlanChanges { get; init; }

    /// <summary>
    /// Baseline comparison findings.
    /// </summary>
    public IReadOnlyList<CiRunBaselineFinding> Findings { get; init; }

    /// <summary>
    /// Number of blocking findings.
    /// </summary>
    public int BlockingCount => Findings.Count(finding => finding.Severity == CiRunBaselineFindingSeverity.Blocking);

    /// <summary>
    /// Number of warning findings.
    /// </summary>
    public int WarningCount => Findings.Count(finding => finding.Severity == CiRunBaselineFindingSeverity.Warning);

    /// <summary>
    /// Indicates whether no blocking or warning differences were found.
    /// </summary>
    public bool MatchesBaseline => BlockingCount == 0 && WarningCount == 0;

    /// <summary>
    /// Compares a run summary to a promoted baseline.
    /// </summary>
    public static CiRunBaselineComparisonReport Create(
        CiRunBaseline baseline,
        HostedCiRunSummary comparison,
        DateTimeOffset comparedAtUtc,
        PlanChangeReport? planChanges = null)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(comparison);

        if (!string.Equals(baseline.CaseId, comparison.CaseId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Baseline and comparison run must use the same case id.", nameof(comparison));
        }

        var findings = new List<CiRunBaselineFinding>();
        var usedPlanSnapshotComparison = planChanges is not null;
        AddIfChanged(
            findings,
            "input-kind.changed",
            CiRunBaselineFindingSeverity.Warning,
            "InputKind",
            "Run source category changed.",
            baseline.InputKind.ToString(),
            comparison.InputKind.ToString());
        AddIfChanged(
            findings,
            "status.changed",
            comparison.Status == BeamKit.Check.BeamKitCheckStatus.Fail ? CiRunBaselineFindingSeverity.Blocking : CiRunBaselineFindingSeverity.Warning,
            "Status",
            "CI status changed from the promoted baseline.",
            baseline.Status.ToString(),
            comparison.Status.ToString());
        AddIfChanged(
            findings,
            "plan-id.changed",
            CiRunBaselineFindingSeverity.Blocking,
            "PlanId",
            "Plan id changed from the promoted baseline.",
            baseline.PlanId,
            comparison.PlanId);
        AddIfChanged(
            findings,
            "plan-fingerprint.changed",
            usedPlanSnapshotComparison ? CiRunBaselineFindingSeverity.Informational : CiRunBaselineFindingSeverity.Blocking,
            "PlanFingerprint",
            usedPlanSnapshotComparison
                ? "Exact plan fingerprint changed; field-level plan snapshot changes are included in this report."
                : "Plan fingerprint changed from the promoted baseline.",
            baseline.PlanFingerprint,
            comparison.PlanFingerprint);
        AddIfChanged(
            findings,
            "prescription-fingerprint.changed",
            usedPlanSnapshotComparison ? CiRunBaselineFindingSeverity.Informational : CiRunBaselineFindingSeverity.Blocking,
            "PrescriptionFingerprint",
            usedPlanSnapshotComparison
                ? "Exact prescription fingerprint changed; field-level prescription changes are included in this report."
                : "Prescription fingerprint changed from the promoted baseline.",
            baseline.PrescriptionFingerprint,
            comparison.PrescriptionFingerprint);
        AddIfChanged(
            findings,
            "rule-pack-fingerprint.changed",
            CiRunBaselineFindingSeverity.Warning,
            "RulePackFingerprint",
            "Rule-pack fingerprint changed from the promoted baseline.",
            baseline.RulePackFingerprint,
            comparison.RulePackFingerprint);
        AddIfChanged(
            findings,
            "rule-pack-version.changed",
            CiRunBaselineFindingSeverity.Warning,
            "RulePackVersion",
            "Rule-pack version changed from the promoted baseline.",
            baseline.RulePackVersion,
            comparison.RulePackVersion);
        AddIfChanged(
            findings,
            "naming-dictionary-id.changed",
            CiRunBaselineFindingSeverity.Warning,
            "NamingDictionaryId",
            "Managed naming-dictionary id changed from the promoted baseline.",
            baseline.NamingDictionaryId,
            comparison.NamingDictionaryId);
        AddIfChanged(
            findings,
            "naming-dictionary-version.changed",
            CiRunBaselineFindingSeverity.Warning,
            "NamingDictionaryVersion",
            "Managed naming-dictionary version changed from the promoted baseline.",
            baseline.NamingDictionaryVersionId,
            comparison.NamingDictionaryVersionId);
        AddIfChanged(
            findings,
            "naming-dictionary-fingerprint.changed",
            CiRunBaselineFindingSeverity.Warning,
            "NamingDictionaryFingerprint",
            "Managed naming-dictionary fingerprint changed from the promoted baseline.",
            baseline.NamingDictionaryFingerprint,
            comparison.NamingDictionaryFingerprint);
        AddIfChanged(
            findings,
            "machine-profile-id.changed",
            CiRunBaselineFindingSeverity.Warning,
            "MachineProfileId",
            "Managed machine-profile id changed from the promoted baseline.",
            baseline.MachineProfileId,
            comparison.MachineProfileId);
        AddIfChanged(
            findings,
            "machine-profile-version.changed",
            CiRunBaselineFindingSeverity.Warning,
            "MachineProfileVersion",
            "Managed machine-profile version changed from the promoted baseline.",
            baseline.MachineProfileVersionId,
            comparison.MachineProfileVersionId);
        AddIfChanged(
            findings,
            "machine-profile-fingerprint.changed",
            CiRunBaselineFindingSeverity.Warning,
            "MachineProfileFingerprint",
            "Managed machine-profile fingerprint changed from the promoted baseline.",
            baseline.MachineProfileFingerprint,
            comparison.MachineProfileFingerprint);
        AddIfChanged(
            findings,
            "policy-set-id.changed",
            CiRunBaselineFindingSeverity.Warning,
            "PolicySetId",
            "Clinical policy-set id changed from the promoted baseline.",
            baseline.PolicySetId,
            comparison.PolicySetId);
        AddIfChanged(
            findings,
            "policy-set-version.changed",
            CiRunBaselineFindingSeverity.Warning,
            "PolicySetVersion",
            "Clinical policy-set version changed from the promoted baseline.",
            baseline.PolicySetVersionId,
            comparison.PolicySetVersionId);
        AddIfChanged(
            findings,
            "policy-set-fingerprint.changed",
            CiRunBaselineFindingSeverity.Warning,
            "PolicySetFingerprint",
            "Clinical policy-set fingerprint changed from the promoted baseline.",
            baseline.PolicySetFingerprint,
            comparison.PolicySetFingerprint);

        if (planChanges is not null)
        {
            foreach (var change in planChanges.Changes)
            {
                findings.Add(new CiRunBaselineFinding(
                    $"plan-change.{change.Type.ToString().ToLowerInvariant()}",
                    MapPlanChangeSeverity(change.Severity),
                    change.Subject,
                    change.Description,
                    change.BeforeValue,
                    change.AfterValue));
            }
        }

        return new CiRunBaselineComparisonReport(
            baseline.CaseId,
            baseline.BaselineRunId,
            comparison.Id,
            comparedAtUtc,
            baseline,
            comparison,
            usedPlanSnapshotComparison,
            planChanges,
            findings);
    }

    private static void AddIfChanged(
        ICollection<CiRunBaselineFinding> findings,
        string code,
        CiRunBaselineFindingSeverity severity,
        string subject,
        string message,
        string? baselineValue,
        string? comparisonValue)
    {
        if (!string.Equals(baselineValue, comparisonValue, StringComparison.Ordinal))
        {
            findings.Add(new CiRunBaselineFinding(code, severity, subject, message, baselineValue, comparisonValue));
        }
    }

    private static CiRunBaselineFindingSeverity MapPlanChangeSeverity(PlanChangeSeverity severity)
    {
        return severity switch
        {
            PlanChangeSeverity.Blocking => CiRunBaselineFindingSeverity.Blocking,
            PlanChangeSeverity.Warning => CiRunBaselineFindingSeverity.Warning,
            _ => CiRunBaselineFindingSeverity.Informational
        };
    }
}
