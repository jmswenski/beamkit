namespace BeamKit.Deliverability;

/// <summary>
/// Status for a deliverability check.
/// </summary>
public enum DeliverabilityStatus
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
    /// Required data was missing.
    /// </summary>
    NotEvaluable
}
