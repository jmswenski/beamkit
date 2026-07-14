namespace BeamKit.CiServer;

/// <summary>
/// Severity for managed machine-profile review findings.
/// </summary>
public enum CiServerMachineProfileReviewSeverity
{
    /// <summary>
    /// Informational finding.
    /// </summary>
    Information,

    /// <summary>
    /// Non-blocking profile concern.
    /// </summary>
    Warning,

    /// <summary>
    /// Blocking profile error.
    /// </summary>
    Error
}
