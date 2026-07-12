namespace BeamKit.CiServer;

/// <summary>
/// Top-level protocol compliance outcome.
/// </summary>
public enum ProtocolComplianceStatus
{
    /// <summary>
    /// All protocol checks passed or blocking findings have accepted variances.
    /// </summary>
    Pass,

    /// <summary>
    /// Only non-blocking warnings remain unresolved.
    /// </summary>
    Warning,

    /// <summary>
    /// At least one blocking evaluated failure remains unresolved.
    /// </summary>
    Fail,

    /// <summary>
    /// Required data was missing for at least one blocking requirement and no evaluated failures remain unresolved.
    /// </summary>
    NotEvaluable
}
