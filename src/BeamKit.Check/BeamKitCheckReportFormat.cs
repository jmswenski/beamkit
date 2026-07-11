namespace BeamKit.Check;

/// <summary>
/// Output formats supported by the BeamKit check report writer.
/// </summary>
public enum BeamKitCheckReportFormat
{
    /// <summary>
    /// Machine-readable JSON.
    /// </summary>
    Json,

    /// <summary>
    /// Markdown for terminals, pull requests, and issue comments.
    /// </summary>
    Markdown,

    /// <summary>
    /// Standalone HTML for clinical review packets.
    /// </summary>
    Html
}
