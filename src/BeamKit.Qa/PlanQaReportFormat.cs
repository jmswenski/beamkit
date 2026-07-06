namespace BeamKit.Qa;

/// <summary>
/// Supported combined QA report formats.
/// </summary>
public enum PlanQaReportFormat
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
