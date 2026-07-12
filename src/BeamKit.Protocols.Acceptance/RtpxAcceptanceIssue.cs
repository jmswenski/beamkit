namespace BeamKit.Protocols.Acceptance;

/// <summary>
/// One hospital-side acceptance issue.
/// </summary>
public sealed record RtpxAcceptanceIssue(
    string Code,
    RtpxAcceptanceIssueSeverity Severity,
    string Message,
    string? Subject = null);
