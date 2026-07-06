namespace BeamKit.PlanCheck;

/// <summary>
/// Severity applied when a plan check does not pass.
/// </summary>
public enum PlanCheckSeverity
{
    /// <summary>
    /// Informational check.
    /// </summary>
    Info,

    /// <summary>
    /// Non-blocking warning.
    /// </summary>
    Warning,

    /// <summary>
    /// Blocking failure.
    /// </summary>
    Failure
}
