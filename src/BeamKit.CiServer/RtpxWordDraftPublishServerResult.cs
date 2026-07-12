using BeamKit.Protocols.Word;
using BeamKit.Safety;

namespace BeamKit.CiServer;

/// <summary>
/// Result returned after publishing a Word-authored RT-PX protocol as a draft.
/// </summary>
public sealed record RtpxWordDraftPublishServerResult(
    string Id,
    DateTimeOffset CreatedAtUtc,
    string SourceFileName,
    string SourceFingerprint,
    RtpxWordExtractionReport Extraction,
    CiServerRtpxAcceptanceSummary? Acceptance,
    CiServerRulePackImportResult? RulePackImport,
    ValidationEvidencePackage? SafetyEvidence,
    SafetyEvidenceReviewResult? SafetyReview,
    RtpxProtocolDiffReport? ProtocolDiff,
    string? DashboardUrl)
{
    /// <summary>
    /// Indicates whether extraction, validation, and draft acceptance succeeded.
    /// </summary>
    public bool Published => Acceptance?.Accepted == true && RulePackImport is not null;

    /// <summary>
    /// Indicates whether Word extraction and RT-PX validation succeeded.
    /// </summary>
    public bool IsValid => Extraction.IsValid;
}
