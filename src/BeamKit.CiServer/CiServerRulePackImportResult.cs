using BeamKit.Check;

namespace BeamKit.CiServer;

/// <summary>
/// Result returned after importing a managed rule-pack version.
/// </summary>
public sealed record CiServerRulePackImportResult
{
    /// <summary>
    /// Creates an import result.
    /// </summary>
    public CiServerRulePackImportResult(
        CiServerManagedRulePackVersionSummary version,
        RulePackValidationReport validation,
        RulePackTestReport? testReport,
        bool activated)
    {
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Validation = validation ?? throw new ArgumentNullException(nameof(validation));
        TestReport = testReport;
        Activated = activated;
    }

    /// <summary>
    /// Imported version summary.
    /// </summary>
    public CiServerManagedRulePackVersionSummary Version { get; init; }

    /// <summary>
    /// Validation report.
    /// </summary>
    public RulePackValidationReport Validation { get; init; }

    /// <summary>
    /// Regression-test report when tests were run during import.
    /// </summary>
    public RulePackTestReport? TestReport { get; init; }

    /// <summary>
    /// Indicates whether the imported version was promoted active.
    /// </summary>
    public bool Activated { get; init; }
}
