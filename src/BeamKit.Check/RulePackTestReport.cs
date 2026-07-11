using System.Text.Json.Serialization;

namespace BeamKit.Check;

/// <summary>
/// Regression-test report for a rule pack.
/// </summary>
public sealed record RulePackTestReport
{
    /// <summary>
    /// Creates a rule-pack test report.
    /// </summary>
    public RulePackTestReport(string rulePackName, string rulePackVersion, DateTimeOffset generatedAtUtc, IEnumerable<RulePackTestResult> results)
        : this(rulePackName, rulePackVersion, generatedAtUtc, results?.ToArray() ?? throw new ArgumentNullException(nameof(results)))
    {
    }

    /// <summary>
    /// Creates a rule-pack test report from JSON.
    /// </summary>
    [JsonConstructor]
    public RulePackTestReport(string rulePackName, string rulePackVersion, DateTimeOffset generatedAtUtc, IReadOnlyList<RulePackTestResult> results)
    {
        RulePackName = CheckText.Required(rulePackName, nameof(rulePackName));
        RulePackVersion = CheckText.Required(rulePackVersion, nameof(rulePackVersion));
        GeneratedAtUtc = generatedAtUtc;
        Results = results?.ToArray() ?? throw new ArgumentNullException(nameof(results));
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
    /// UTC timestamp when the report was produced.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary>
    /// Test results.
    /// </summary>
    public IReadOnlyList<RulePackTestResult> Results { get; init; }

    /// <summary>
    /// Number of passed test cases.
    /// </summary>
    public int PassedCount => Results.Count(result => result.Passed);

    /// <summary>
    /// Number of failed test cases.
    /// </summary>
    public int FailedCount => Results.Count(result => !result.Passed);

    /// <summary>
    /// Indicates whether every test case passed.
    /// </summary>
    public bool Passed => FailedCount == 0;
}
