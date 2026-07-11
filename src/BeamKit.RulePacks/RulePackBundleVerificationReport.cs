namespace BeamKit.RulePacks;

/// <summary>
/// Integrity report for a rule-pack release bundle.
/// </summary>
public sealed record RulePackBundleVerificationReport
{
    /// <summary>
    /// Creates a bundle verification report.
    /// </summary>
    public RulePackBundleVerificationReport(
        string rulePackName,
        string rulePackVersion,
        string rulePackFingerprint,
        string bundleFingerprint,
        string computedBundleFingerprint,
        IEnumerable<RulePackBundleIssue> issues)
    {
        RulePackName = RulePackText.Required(rulePackName, nameof(rulePackName));
        RulePackVersion = RulePackText.Required(rulePackVersion, nameof(rulePackVersion));
        RulePackFingerprint = RulePackText.Required(rulePackFingerprint, nameof(rulePackFingerprint));
        BundleFingerprint = RulePackText.Required(bundleFingerprint, nameof(bundleFingerprint));
        ComputedBundleFingerprint = RulePackText.Required(computedBundleFingerprint, nameof(computedBundleFingerprint));
        Issues = issues?.ToArray() ?? throw new ArgumentNullException(nameof(issues));
    }

    /// <summary>
    /// Rule-pack name.
    /// </summary>
    public string RulePackName { get; init; }

    /// <summary>
    /// Rule-pack authoring version.
    /// </summary>
    public string RulePackVersion { get; init; }

    /// <summary>
    /// Stored executable rule-pack fingerprint.
    /// </summary>
    public string RulePackFingerprint { get; init; }

    /// <summary>
    /// Stored bundle fingerprint.
    /// </summary>
    public string BundleFingerprint { get; init; }

    /// <summary>
    /// Recomputed bundle fingerprint.
    /// </summary>
    public string ComputedBundleFingerprint { get; init; }

    /// <summary>
    /// Verification findings.
    /// </summary>
    public IReadOnlyList<RulePackBundleIssue> Issues { get; init; }

    /// <summary>
    /// Number of blocking verification issues.
    /// </summary>
    public int ErrorCount => Issues.Count(issue => issue.Severity == RulePackDoctorIssueSeverity.Error);

    /// <summary>
    /// Number of warning verification issues.
    /// </summary>
    public int WarningCount => Issues.Count(issue => issue.Severity == RulePackDoctorIssueSeverity.Warning);

    /// <summary>
    /// Indicates whether the bundle is internally consistent.
    /// </summary>
    public bool IsValid => ErrorCount == 0;
}
