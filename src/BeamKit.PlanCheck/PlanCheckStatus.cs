namespace BeamKit.PlanCheck;

/// <summary>
/// Status for one plan-check result.
/// </summary>
public enum PlanCheckStatus
{
    /// <summary>
    /// Check passed.
    /// </summary>
    Pass,

    /// <summary>
    /// Check produced a non-blocking concern.
    /// </summary>
    Warning,

    /// <summary>
    /// Check failed.
    /// </summary>
    Fail,

    /// <summary>
    /// Check could not be evaluated because required data was missing.
    /// </summary>
    NotEvaluable
}
