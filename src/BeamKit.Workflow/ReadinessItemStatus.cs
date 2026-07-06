namespace BeamKit.Workflow;

/// <summary>
/// Status for one plan-readiness checklist item.
/// </summary>
public enum ReadinessItemStatus
{
    /// <summary>
    /// The item has not yet been completed.
    /// </summary>
    Pending,

    /// <summary>
    /// The item is complete.
    /// </summary>
    Complete,

    /// <summary>
    /// The item is blocked by another unresolved condition.
    /// </summary>
    Blocked,

    /// <summary>
    /// The item does not apply to this plan or workflow.
    /// </summary>
    NotApplicable
}
