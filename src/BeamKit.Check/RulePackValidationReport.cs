using System.Text.Json.Serialization;

namespace BeamKit.Check;

/// <summary>
/// Policy-as-code validation report for one rule pack.
/// </summary>
public sealed record RulePackValidationReport
{
    /// <summary>
    /// Creates a validation report.
    /// </summary>
    public RulePackValidationReport(string rulePackName, string rulePackVersion, string fingerprint, IEnumerable<RulePackPolicyIssue> issues)
        : this(rulePackName, rulePackVersion, fingerprint, issues?.ToArray() ?? throw new ArgumentNullException(nameof(issues)))
    {
    }

    /// <summary>
    /// Creates a validation report from JSON.
    /// </summary>
    [JsonConstructor]
    public RulePackValidationReport(string rulePackName, string rulePackVersion, string fingerprint, IReadOnlyList<RulePackPolicyIssue> issues)
    {
        RulePackName = CheckText.Required(rulePackName, nameof(rulePackName));
        RulePackVersion = CheckText.Required(rulePackVersion, nameof(rulePackVersion));
        Fingerprint = CheckText.Required(fingerprint, nameof(fingerprint));
        Issues = issues?.ToArray() ?? throw new ArgumentNullException(nameof(issues));
    }

    /// <summary>
    /// Rule-pack name.
    /// </summary>
    public string RulePackName { get; init; }

    /// <summary>
    /// Rule-pack version.
    /// </summary>
    public string RulePackVersion { get; init; }

    /// <summary>
    /// Deterministic fingerprint of the policy bundle.
    /// </summary>
    public string Fingerprint { get; init; }

    /// <summary>
    /// Validation issues.
    /// </summary>
    public IReadOnlyList<RulePackPolicyIssue> Issues { get; init; }

    /// <summary>
    /// Number of informational findings.
    /// </summary>
    public int InfoCount => Issues.Count(issue => issue.Severity == PolicyIssueSeverity.Info);

    /// <summary>
    /// Number of warning findings.
    /// </summary>
    public int WarningCount => Issues.Count(issue => issue.Severity == PolicyIssueSeverity.Warning);

    /// <summary>
    /// Number of blocking findings.
    /// </summary>
    public int ErrorCount => Issues.Count(issue => issue.Severity == PolicyIssueSeverity.Error);

    /// <summary>
    /// Indicates whether the rule pack has no blocking policy issues.
    /// </summary>
    public bool IsValid => ErrorCount == 0;
}
