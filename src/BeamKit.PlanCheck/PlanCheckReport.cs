using System.Text.Json.Serialization;

namespace BeamKit.PlanCheck;

/// <summary>
/// Result report for a plan-check run.
/// </summary>
public sealed record PlanCheckReport
{
    /// <summary>
    /// Creates a plan-check report.
    /// </summary>
    public PlanCheckReport(string planId, string catalogName, string catalogVersion, IEnumerable<PlanCheckResult> results)
        : this(planId, catalogName, catalogVersion, results?.ToArray() ?? throw new ArgumentNullException(nameof(results)))
    {
    }

    /// <summary>
    /// Creates a plan-check report from JSON.
    /// </summary>
    [JsonConstructor]
    public PlanCheckReport(string planId, string catalogName, string catalogVersion, IReadOnlyList<PlanCheckResult> results)
    {
        PlanId = PlanCheckText.Required(planId, nameof(planId));
        CatalogName = PlanCheckText.Required(catalogName, nameof(catalogName));
        CatalogVersion = PlanCheckText.Required(catalogVersion, nameof(catalogVersion));
        Results = results?.ToArray() ?? throw new ArgumentNullException(nameof(results));
    }

    /// <summary>
    /// Evaluated plan id.
    /// </summary>
    public string PlanId { get; init; }

    /// <summary>
    /// Catalog name.
    /// </summary>
    public string CatalogName { get; init; }

    /// <summary>
    /// Catalog version.
    /// </summary>
    public string CatalogVersion { get; init; }

    /// <summary>
    /// Check results.
    /// </summary>
    public IReadOnlyList<PlanCheckResult> Results { get; init; }

    /// <summary>
    /// Number of passing checks.
    /// </summary>
    public int PassCount => Results.Count(result => result.Status == PlanCheckStatus.Pass);

    /// <summary>
    /// Number of warning checks.
    /// </summary>
    public int WarningCount => Results.Count(result => result.Status == PlanCheckStatus.Warning);

    /// <summary>
    /// Number of failing checks.
    /// </summary>
    public int FailCount => Results.Count(result => result.Status == PlanCheckStatus.Fail);

    /// <summary>
    /// Number of not-evaluable checks.
    /// </summary>
    public int NotEvaluableCount => Results.Count(result => result.Status == PlanCheckStatus.NotEvaluable);

    /// <summary>
    /// Indicates whether blocking issues exist.
    /// </summary>
    public bool HasBlockingIssues => Results.Any(result => result.Status is PlanCheckStatus.Fail or PlanCheckStatus.NotEvaluable);
}
