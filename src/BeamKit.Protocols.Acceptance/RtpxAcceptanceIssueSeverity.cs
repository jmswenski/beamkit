namespace BeamKit.Protocols.Acceptance;

/// <summary>
/// Severity for hospital-side RT-PX acceptance findings.
/// </summary>
public enum RtpxAcceptanceIssueSeverity
{
    /// <summary>
    /// Informational evidence.
    /// </summary>
    Info,

    /// <summary>
    /// Non-blocking acceptance concern.
    /// </summary>
    Warning,

    /// <summary>
    /// Blocking acceptance issue.
    /// </summary>
    Error
}
