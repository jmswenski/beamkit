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
        IEnumerable<CiRunBaselineFinding>? findings = null)
    {
        CaseId = CiServerText.Required(caseId, nameof(caseId));
        BaselineRunId = CiServerText.Required(baselineRunId, nameof(baselineRunId));
        ComparisonRunId = CiServerText.Required(comparisonRunId, nameof(comparisonRunId));
        ComparedAtUtc = comparedAtUtc;
        Baseline = baseline ?? throw new ArgumentNullException(nameof(baseline));
        Comparison = comparison ?? throw new ArgumentNullException(nameof(comparison));
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
    public static CiRunBaselineComparisonReport Create(CiRunBaseline baseline, HostedCiRunSummary comparison, DateTimeOffset comparedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(comparison);

        if (!string.Equals(baseline.CaseId, comparison.CaseId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Baseline and comparison run must use the same case id.", nameof(comparison));
        }

        var findings = new List<CiRunBaselineFinding>();
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
            CiRunBaselineFindingSeverity.Blocking,
            "PlanFingerprint",
            "Plan fingerprint changed from the promoted baseline.",
            baseline.PlanFingerprint,
            comparison.PlanFingerprint);
        AddIfChanged(
            findings,
            "prescription-fingerprint.changed",
            CiRunBaselineFindingSeverity.Blocking,
            "PrescriptionFingerprint",
            "Prescription fingerprint changed from the promoted baseline.",
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

        return new CiRunBaselineComparisonReport(
            baseline.CaseId,
            baseline.BaselineRunId,
            comparison.Id,
            comparedAtUtc,
            baseline,
            comparison,
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
}
