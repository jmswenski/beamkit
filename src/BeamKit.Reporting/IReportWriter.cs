namespace BeamKit.Reporting;

/// <summary>
/// Writes a plan evaluation report in one output format.
/// </summary>
public interface IReportWriter
{
    /// <summary>
    /// Format written by this writer.
    /// </summary>
    ReportFormat Format { get; }

    /// <summary>
    /// Serializes a report.
    /// </summary>
    string Write(PlanEvaluationReport report);
}
