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
    /// Acknowledged protocol diff change ids.
    /// </summary>
    public IReadOnlyList<string> AcknowledgedDiffChangeIds => Acceptance.AcknowledgedDiffChangeIds;

    /// <summary>
    /// Diff changes that require acknowledgement before approval.
    /// </summary>
    public IReadOnlyList<RtpxProtocolDiffChange> AcknowledgementRequiredChanges =>
        ProtocolDiff.Changes
            .Where(change => !string.Equals(change.Severity, "Info", StringComparison.OrdinalIgnoreCase))
            .ToArray();

    /// <summary>
    /// Diff changes still waiting for acknowledgement.
    /// </summary>
    public IReadOnlyList<RtpxProtocolDiffChange> PendingAcknowledgementChanges =>
        AcknowledgementRequiredChanges
            .Where(change => !AcknowledgedDiffChangeIds.Contains(change.Id, StringComparer.OrdinalIgnoreCase))
            .ToArray();

    /// <summary>
    /// Indicates whether all review-relevant protocol diff items have been acknowledged.
    /// </summary>
    public bool IsDiffAcknowledged => PendingAcknowledgementChanges.Count == 0;

    /// <summary>
    /// Indicates whether the draft can be approved for promotion.
    /// </summary>
    public bool IsApprovable =>
        Acceptance.Accepted
        && Version?.IsValid == true
        && SafetyEvidence is not null
        && IsDiffAcknowledged
        && (Acceptance.ReviewStatus is RtpxDraftReviewStatus.Draft
            or RtpxDraftReviewStatus.InReview
            or RtpxDraftReviewStatus.ChangesRequested);

    /// <summary>
    /// Indicates whether the draft has all server-side evidence needed for promotion.
    /// </summary>
    public bool IsPromotable =>
        Acceptance.Accepted
        && Version?.IsValid == true
        && Version.TestPassed == true
        && SafetyEvidence is not null
        && IsDiffAcknowledged
        && Acceptance.ReviewStatus == RtpxDraftReviewStatus.Approved;
}
