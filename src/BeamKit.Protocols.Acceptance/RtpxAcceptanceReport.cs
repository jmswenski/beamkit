using BeamKit.Protocols;

namespace BeamKit.Protocols.Acceptance;

/// <summary>
/// Result of accepting an RT-PX package for local institutional use.
/// </summary>
public sealed record RtpxAcceptanceReport(
    string PackagePath,
    string OutputDirectory,
    string Institution,
    DateTimeOffset AcceptedAtUtc,
    RadiotherapyProtocolPackage SourcePackage,
    RadiotherapyProtocolPackage LocalPackage,
    IReadOnlyList<RtpxStructureMappingResult> StructureMappings,
    IReadOnlyList<RtpxAcceptanceIssue> Issues,
    RtpxEsapiAcceptanceEvidence? EsapiEvidence,
    IReadOnlyList<string> Files)
{
    /// <summary>
    /// Number of blocking acceptance issues.
    /// </summary>
    public int ErrorCount => Issues.Count(issue => issue.Severity == RtpxAcceptanceIssueSeverity.Error);

    /// <summary>
    /// Number of non-blocking acceptance warnings.
    /// </summary>
    public int WarningCount => Issues.Count(issue => issue.Severity == RtpxAcceptanceIssueSeverity.Warning);

    /// <summary>
    /// Indicates whether the package was accepted without blocking issues.
    /// </summary>
    public bool IsAccepted => ErrorCount == 0;
}
