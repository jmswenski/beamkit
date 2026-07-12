namespace BeamKit.Protocols;

/// <summary>
/// Review and approval metadata for a computable RT-PX package.
/// </summary>
public sealed record ProtocolApproval
{
    /// <summary>
    /// Creates empty approval metadata for JSON deserialization.
    /// </summary>
    public ProtocolApproval()
    {
    }

    /// <summary>
    /// Creates protocol approval metadata.
    /// </summary>
    public ProtocolApproval(
        string? reviewedBy = null,
        string? approvedBy = null,
        DateOnly? effectiveDate = null,
        DateOnly? reviewDueDate = null,
        string? reference = null,
        string? rationale = null,
        string? changeTicket = null)
    {
        ReviewedBy = ProtocolText.Optional(reviewedBy);
        ApprovedBy = ProtocolText.Optional(approvedBy);
        EffectiveDate = effectiveDate;
        ReviewDueDate = reviewDueDate;
        Reference = ProtocolText.Optional(reference);
        Rationale = ProtocolText.Optional(rationale);
        ChangeTicket = ProtocolText.Optional(changeTicket);
    }

    /// <summary>
    /// Clinical, physics, or informatics reviewer.
    /// </summary>
    public string? ReviewedBy { get; init; }

    /// <summary>
    /// Approver for the computable protocol.
    /// </summary>
    public string? ApprovedBy { get; init; }

    /// <summary>
    /// Date when this package becomes effective.
    /// </summary>
    public DateOnly? EffectiveDate { get; init; }

    /// <summary>
    /// Date by which the computable protocol should be reviewed again.
    /// </summary>
    public DateOnly? ReviewDueDate { get; init; }

    /// <summary>
    /// Committee, policy, ticket, meeting, or document-control reference.
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>
    /// Rationale for local acceptance.
    /// </summary>
    public string? Rationale { get; init; }

    /// <summary>
    /// Optional change-control ticket or pull request.
    /// </summary>
    public string? ChangeTicket { get; init; }
}
