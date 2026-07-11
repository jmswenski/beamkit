namespace BeamKit.Intelligence;

/// <summary>
/// Severity for a predictive intelligence signal.
/// </summary>
public enum PredictiveSignalSeverity
{
    /// <summary>
    /// Informational signal.
    /// </summary>
    Info,

    /// <summary>
    /// Low-severity signal.
    /// </summary>
    Low,

    /// <summary>
    /// Medium-severity signal.
    /// </summary>
    Medium,

    /// <summary>
    /// High-severity signal.
    /// </summary>
    High,

    /// <summary>
    /// Critical signal that should be addressed before clinical handoff.
    /// </summary>
    Critical
}
