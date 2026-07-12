namespace BeamKit.CiServer;

/// <summary>
/// Durable review state for an RT-PX draft.
/// </summary>
public enum RtpxDraftReviewStatus
{
    /// <summary>
    /// Draft was created and is waiting for review.
    /// </summary>
    Draft,

    /// <summary>
    /// Draft is actively being reviewed.
    /// </summary>
    InReview,

    /// <summary>
    /// Reviewer requested changes before approval.
    /// </summary>
    ChangesRequested,

    /// <summary>
    /// Draft was rejected.
    /// </summary>
    Rejected,

    /// <summary>
    /// Draft was approved for promotion.
    /// </summary>
    Approved,

    /// <summary>
    /// Draft was promoted active.
    /// </summary>
    Promoted
}
