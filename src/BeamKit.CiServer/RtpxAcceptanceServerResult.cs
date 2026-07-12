using BeamKit.Protocols.Acceptance;
using BeamKit.Safety;

namespace BeamKit.CiServer;

/// <summary>
/// Result of accepting an RT-PX package through the CI server.
/// </summary>
public sealed record RtpxAcceptanceServerResult(
    CiServerRtpxAcceptanceSummary Acceptance,
    RtpxAcceptanceReport Report,
    CiServerRulePackImportResult? RulePackImport,
    CiServerManagedRulePackVersionSummary? PromotedVersion,
    ValidationEvidencePackage? SafetyEvidence,
    SafetyEvidenceReviewResult? SafetyReview);
