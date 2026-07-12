namespace BeamKit.Protocols;

/// <summary>
/// Severity of a protocol authoring finding.
/// </summary>
public enum ProtocolValidationSeverity
{
    /// <summary>
    /// Informational finding.
    /// </summary>
    Info,

    /// <summary>
    /// Non-blocking issue that should be reviewed.
    /// </summary>
    Warning,

    /// <summary>
    /// Blocking authoring error.
    /// </summary>
    Error
}
