namespace BeamKit.CiServer;

/// <summary>
/// Detailed protocol compliance run with export artifacts.
/// </summary>
public sealed record ProtocolComplianceRunDetail
{
    /// <summary>
    /// Creates a detail response from a persisted run.
    /// </summary>
    public ProtocolComplianceRunDetail(ProtocolComplianceRunRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        Summary = new ProtocolComplianceRunSummary(record);
        ReportJson = record.ReportJson;
        MarkdownReport = record.MarkdownReport;
        PlanSnapshotJson = record.PlanSnapshotJson;
    }

    /// <summary>
    /// Run summary.
    /// </summary>
    public ProtocolComplianceRunSummary Summary { get; init; }

    /// <summary>
    /// Serialized protocol compliance report.
    /// </summary>
    public string ReportJson { get; init; }

    /// <summary>
    /// Markdown protocol compliance packet.
    /// </summary>
    public string MarkdownReport { get; init; }

    /// <summary>
    /// Vendor-neutral BeamKit plan snapshot JSON used for this run.
    /// </summary>
    public string PlanSnapshotJson { get; init; }
}
