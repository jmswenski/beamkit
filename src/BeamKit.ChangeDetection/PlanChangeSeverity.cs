namespace BeamKit.ChangeDetection;

/// <summary>
/// Clinical workflow severity assigned to a detected plan change.
/// </summary>
public enum PlanChangeSeverity
{
    /// <summary>
    /// Change is informational and generally does not block downstream workflow.
    /// </summary>
    Informational,

    /// <summary>
    /// Change should be reviewed before downstream workflow continues.
    /// </summary>
    Warning,

    /// <summary>
    /// Change should invalidate readiness or approval state until reviewed.
    /// </summary>
    Blocking
}
