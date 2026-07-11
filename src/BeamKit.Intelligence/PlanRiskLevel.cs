namespace BeamKit.Intelligence;

/// <summary>
/// Predicted plan QA and review risk bucket.
/// </summary>
public enum PlanRiskLevel
{
    /// <summary>
    /// No major risk signals were identified.
    /// </summary>
    Low,

    /// <summary>
    /// Plan has risk signals that should be reviewed in normal QA.
    /// </summary>
    Elevated,

    /// <summary>
    /// Plan has risk signals that should receive focused dosimetry or physics attention.
    /// </summary>
    High,

    /// <summary>
    /// Plan has critical readiness, dosimetric, or deliverability signals.
    /// </summary>
    Critical
}
