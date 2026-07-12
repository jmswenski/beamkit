namespace BeamKit.Protocols.Word;

/// <summary>
/// Authoring or parsing issue found in an RT-PX Word protocol document.
/// </summary>
public sealed record RtpxWordExtractionIssue(
    string Code,
    RtpxWordIssueSeverity Severity,
    string Message,
    string? Section = null,
    string? Anchor = null);
