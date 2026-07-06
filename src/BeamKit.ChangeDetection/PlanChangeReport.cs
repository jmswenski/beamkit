namespace BeamKit.ChangeDetection;

/// <summary>
/// Result of comparing two BeamKit plans.
/// </summary>
public sealed record PlanChangeReport
{
    /// <summary>
    /// Creates a plan change report.
    /// </summary>
    public PlanChangeReport(string baselinePlanId, string comparisonPlanId, IEnumerable<PlanChange>? changes = null)
    {
        BaselinePlanId = ChangeDetectionText.Required(baselinePlanId, nameof(baselinePlanId));
        ComparisonPlanId = ChangeDetectionText.Required(comparisonPlanId, nameof(comparisonPlanId));
        Changes = changes?.ToArray() ?? Array.Empty<PlanChange>();
    }

    /// <summary>
    /// Baseline plan identifier.
    /// </summary>
    public string BaselinePlanId { get; init; }

    /// <summary>
    /// Comparison plan identifier.
    /// </summary>
    public string ComparisonPlanId { get; init; }

    /// <summary>
    /// Detected changes.
    /// </summary>
    public IReadOnlyList<PlanChange> Changes { get; init; }

    /// <summary>
    /// Indicates whether any detected change should block downstream workflow.
    /// </summary>
    public bool HasBlockingChanges => Changes.Any(change => change.Severity == PlanChangeSeverity.Blocking);
}
