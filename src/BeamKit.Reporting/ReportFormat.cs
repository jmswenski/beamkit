namespace BeamKit.Reporting;

/// <summary>
/// Supported report output formats.
/// </summary>
public enum ReportFormat
{
    /// <summary>
    /// JSON for automation and integrations.
    /// </summary>
    Json,

    /// <summary>
    /// Markdown for text workflows.
    /// </summary>
    Markdown,

    /// <summary>
    /// HTML for browser rendering.
    /// </summary>
    Html
}
