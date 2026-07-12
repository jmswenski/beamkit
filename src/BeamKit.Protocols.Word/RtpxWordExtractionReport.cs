using BeamKit.Protocols;

namespace BeamKit.Protocols.Word;

/// <summary>
/// Result of extracting computable RT-PX protocol intent from a Word document.
/// </summary>
public sealed record RtpxWordExtractionReport(
    string SourcePath,
    RadiotherapyProtocolPackage? Package,
    IReadOnlyList<RtpxWordExtractionIssue> Issues,
    ProtocolValidationReport? Validation)
{
    /// <summary>
    /// Count of blocking Word extraction issues.
    /// </summary>
    public int ErrorCount => Issues.Count(issue => issue.Severity == RtpxWordIssueSeverity.Error);

    /// <summary>
    /// Count of non-blocking Word extraction issues.
    /// </summary>
    public int WarningCount => Issues.Count(issue => issue.Severity == RtpxWordIssueSeverity.Warning);

    /// <summary>
    /// Indicates whether extraction and RT-PX validation both succeeded.
    /// </summary>
    public bool IsValid => Package is not null && ErrorCount == 0 && (Validation?.IsValid ?? false);
}
