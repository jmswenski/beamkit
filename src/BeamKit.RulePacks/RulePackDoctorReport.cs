using BeamKit.Check;

namespace BeamKit.RulePacks;

/// <summary>
/// Rule-pack authoring health report.
/// </summary>
public sealed record RulePackDoctorReport
{
    /// <summary>
    /// Creates a rule-pack doctor report.
    /// </summary>
    public RulePackDoctorReport(
        string manifestPath,
        string name,
        string version,
        string fingerprint,
        RulePackValidationReport validation,
        IEnumerable<RulePackDoctorIssue> issues)
    {
        ManifestPath = RulePackText.Required(manifestPath, nameof(manifestPath));
        Name = RulePackText.Required(name, nameof(name));
        Version = RulePackText.Required(version, nameof(version));
        Fingerprint = RulePackText.Required(fingerprint, nameof(fingerprint));
        Validation = validation ?? throw new ArgumentNullException(nameof(validation));
        Issues = issues?.ToArray() ?? throw new ArgumentNullException(nameof(issues));
    }

    /// <summary>
    /// Manifest path that was inspected.
    /// </summary>
    public string ManifestPath { get; init; }

    /// <summary>
    /// Rule-pack name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Rule-pack version.
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Executable rule-pack fingerprint.
    /// </summary>
    public string Fingerprint { get; init; }

    /// <summary>
    /// Policy-as-code validation report.
    /// </summary>
    public RulePackValidationReport Validation { get; init; }

    /// <summary>
    /// Authoring and governance findings.
    /// </summary>
    public IReadOnlyList<RulePackDoctorIssue> Issues { get; init; }

    /// <summary>
    /// Number of error findings.
    /// </summary>
    public int ErrorCount => Issues.Count(issue => issue.Severity == RulePackDoctorIssueSeverity.Error) + Validation.ErrorCount;

    /// <summary>
    /// Number of warning findings.
    /// </summary>
    public int WarningCount => Issues.Count(issue => issue.Severity == RulePackDoctorIssueSeverity.Warning) + Validation.WarningCount;

    /// <summary>
    /// Indicates whether the rule pack has no blocking doctor or validation findings.
    /// </summary>
    public bool IsHealthy => ErrorCount == 0;
}
