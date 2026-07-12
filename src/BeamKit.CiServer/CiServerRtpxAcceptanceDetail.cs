namespace BeamKit.CiServer;

/// <summary>
/// Detailed RT-PX acceptance record with serialized report and safety evidence.
/// </summary>
public sealed record CiServerRtpxAcceptanceDetail
{
    /// <summary>
    /// Creates a detail response from a stored RT-PX acceptance record.
    /// </summary>
    public CiServerRtpxAcceptanceDetail(CiServerRtpxAcceptanceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        Summary = new CiServerRtpxAcceptanceSummary(record);
        PackagePath = record.PackagePath;
        ReportJson = record.ReportJson;
        SafetyEvidenceJson = record.SafetyEvidenceJson;
    }

    /// <summary>
    /// Acceptance summary.
    /// </summary>
    public CiServerRtpxAcceptanceSummary Summary { get; init; }

    /// <summary>
    /// Server-local package path used for acceptance.
    /// </summary>
    public string PackagePath { get; init; }

    /// <summary>
    /// Serialized RT-PX acceptance report.
    /// </summary>
    public string ReportJson { get; init; }

    /// <summary>
    /// Serialized safety evidence generated for the imported rule-pack version.
    /// </summary>
    public string? SafetyEvidenceJson { get; init; }
}
