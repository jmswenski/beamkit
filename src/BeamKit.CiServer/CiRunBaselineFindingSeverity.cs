namespace BeamKit.CiServer;

/// <summary>
/// Severity assigned to a CI baseline comparison finding.
/// </summary>
public enum CiRunBaselineFindingSeverity
{
    /// <summary>
    /// Difference is informational only.
    /// </summary>
    Informational,

    /// <summary>
    /// Difference should be reviewed.
    /// </summary>
    Warning,

    /// <summary>
    /// Difference should block automatic release until reviewed.
    /// </summary>
    Blocking
}
