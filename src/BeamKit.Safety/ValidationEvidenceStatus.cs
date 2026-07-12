namespace BeamKit.Safety;

/// <summary>
/// Result status for a validation evidence item.
/// </summary>
public enum ValidationEvidenceStatus
{
    /// <summary>
    /// Evidence has not been executed or reviewed.
    /// </summary>
    NotRun,

    /// <summary>
    /// Evidence passed without blocking findings.
    /// </summary>
    Pass,

    /// <summary>
    /// Evidence has non-blocking findings.
    /// </summary>
    Warning,

    /// <summary>
    /// Evidence failed or has blocking findings.
    /// </summary>
    Fail
}
