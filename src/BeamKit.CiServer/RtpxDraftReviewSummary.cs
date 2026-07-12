using BeamKit.Check;
using BeamKit.Safety;

namespace BeamKit.CiServer;

/// <summary>
/// Dashboard-ready summary of an RT-PX draft awaiting review.
/// </summary>
public sealed record RtpxDraftReviewSummary(
    CiServerRtpxAcceptanceSummary Acceptance,
    CiServerManagedRulePackVersionSummary? Version,
    RulePackValidationReport? Validation,
    RulePackTestReport? TestReport,
    ValidationEvidencePackage? SafetyEvidence,
    RtpxProtocolDiffReport ProtocolDiff)
{
    /// <summary>
    /// Indicates whether the draft has all server-side evidence needed for promotion.
    /// </summary>
    public bool IsPromotable =>
        Acceptance.Accepted
        && Version?.IsValid == true
        && Version.TestPassed == true
        && SafetyEvidence is not null;
}
