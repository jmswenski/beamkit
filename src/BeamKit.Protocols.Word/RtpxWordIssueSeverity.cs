namespace BeamKit.Protocols.Word;

/// <summary>
/// Severity for issues detected while translating a Word protocol into RT-PX.
/// </summary>
public enum RtpxWordIssueSeverity
{
    /// <summary>
    /// Informational note.
    /// </summary>
    Info,

    /// <summary>
    /// Non-blocking authoring issue.
    /// </summary>
    Warning,

    /// <summary>
    /// Blocking issue that prevents reliable RT-PX extraction.
    /// </summary>
    Error
}
