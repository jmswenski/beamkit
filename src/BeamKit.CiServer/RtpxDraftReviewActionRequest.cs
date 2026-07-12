namespace BeamKit.CiServer;

/// <summary>
/// Review action request for an RT-PX draft.
/// </summary>
public sealed record RtpxDraftReviewActionRequest
{
    /// <summary>
    /// Reviewer or approver label.
    /// </summary>
    public string? ReviewedBy { get; init; }

    /// <summary>
    /// Optional review note.
    /// </summary>
    public string? Note { get; init; }

    /// <summary>
    /// Optional diff change ids acknowledged by the reviewer.
    /// </summary>
    public IReadOnlyList<string>? DiffChangeIds { get; init; }
}
