namespace BeamKit.Workflow;

/// <summary>
/// Evaluated readiness state for a plan.
/// </summary>
public sealed record PlanReadinessState
{
    /// <summary>
    /// Creates plan-readiness state.
    /// </summary>
    public PlanReadinessState(string planId, IEnumerable<ReadinessItem> items)
    {
        if (string.IsNullOrWhiteSpace(planId))
        {
            throw new ArgumentException("Value is required.", nameof(planId));
        }

        PlanId = planId.Trim();
        Items = items?.ToArray() ?? throw new ArgumentNullException(nameof(items));
    }

    /// <summary>
    /// Identifier of the evaluated plan.
    /// </summary>
    public string PlanId { get; init; }

    /// <summary>
    /// Checklist items in workflow order.
    /// </summary>
    public IReadOnlyList<ReadinessItem> Items { get; init; }

    /// <summary>
    /// Indicates whether all readiness items are complete or not applicable.
    /// </summary>
    public bool IsReady => Items.All(item =>
        item.Status is ReadinessItemStatus.Complete or ReadinessItemStatus.NotApplicable);

    /// <summary>
    /// Pending or blocked items that still need attention.
    /// </summary>
    public IReadOnlyList<ReadinessItem> OutstandingItems => Items
        .Where(item => item.Status is ReadinessItemStatus.Pending or ReadinessItemStatus.Blocked)
        .ToArray();
}
