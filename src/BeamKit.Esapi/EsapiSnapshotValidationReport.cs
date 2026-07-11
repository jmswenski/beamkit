namespace BeamKit.Esapi;

/// <summary>
/// Validation report for a read-only ESAPI snapshot.
/// </summary>
public sealed record EsapiSnapshotValidationReport
{
    /// <summary>
    /// Creates a validation report.
    /// </summary>
    public EsapiSnapshotValidationReport(string planId, IEnumerable<EsapiSnapshotValidationIssue> issues)
    {
        PlanId = EsapiText.Required(planId, nameof(planId));
        Issues = issues?.ToArray() ?? throw new ArgumentNullException(nameof(issues));
    }

    /// <summary>
    /// Plan id from the snapshot.
    /// </summary>
    public string PlanId { get; init; }

    /// <summary>
    /// Validation issues found in the snapshot.
    /// </summary>
    public IReadOnlyList<EsapiSnapshotValidationIssue> Issues { get; init; }

    /// <summary>
    /// Number of informational issues.
    /// </summary>
    public int InfoCount => Issues.Count(issue => issue.Severity == EsapiSnapshotIssueSeverity.Info);

    /// <summary>
    /// Number of warning issues.
    /// </summary>
    public int WarningCount => Issues.Count(issue => issue.Severity == EsapiSnapshotIssueSeverity.Warning);

    /// <summary>
    /// Number of error issues.
    /// </summary>
    public int ErrorCount => Issues.Count(issue => issue.Severity == EsapiSnapshotIssueSeverity.Error);

    /// <summary>
    /// Indicates whether any validation issue should block trusted downstream use.
    /// </summary>
    public bool HasErrors => ErrorCount > 0;
}
