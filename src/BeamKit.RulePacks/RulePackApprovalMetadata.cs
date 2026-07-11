namespace BeamKit.RulePacks;

/// <summary>
/// Governance metadata used to review and approve a BeamKit rule-pack manifest.
/// </summary>
public sealed record RulePackApprovalMetadata
{
    /// <summary>
    /// Creates approval metadata for a rule pack.
    /// </summary>
    public RulePackApprovalMetadata(
        string? status = null,
        string? institution = null,
        string? physicianGroup = null,
        string? reviewedBy = null,
        string? approvedBy = null,
        DateOnly? effectiveDate = null,
        DateOnly? reviewDueDate = null,
        string? reference = null,
        string? rationale = null,
        string? changeTicket = null)
    {
        Status = RulePackText.Optional(status);
        Institution = RulePackText.Optional(institution);
        PhysicianGroup = RulePackText.Optional(physicianGroup);
        ReviewedBy = RulePackText.Optional(reviewedBy);
        ApprovedBy = RulePackText.Optional(approvedBy);
        EffectiveDate = effectiveDate;
        ReviewDueDate = reviewDueDate;
        Reference = RulePackText.Optional(reference);
        Rationale = RulePackText.Optional(rationale);
        ChangeTicket = RulePackText.Optional(changeTicket);
    }

    /// <summary>
    /// Governance state, for example Draft, InReview, Approved, or Retired.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Institution that owns the rule pack.
    /// </summary>
    public string? Institution { get; init; }

    /// <summary>
    /// Physician group, service line, or disease-site committee covered by the rule pack.
    /// </summary>
    public string? PhysicianGroup { get; init; }

    /// <summary>
    /// Actor or group that reviewed the rule pack.
    /// </summary>
    public string? ReviewedBy { get; init; }

    /// <summary>
    /// Actor or group that approved the rule pack.
    /// </summary>
    public string? ApprovedBy { get; init; }

    /// <summary>
    /// Date when the rule pack should become effective.
    /// </summary>
    public DateOnly? EffectiveDate { get; init; }

    /// <summary>
    /// Date when the rule pack should be reviewed again.
    /// </summary>
    public DateOnly? ReviewDueDate { get; init; }

    /// <summary>
    /// Protocol, policy, meeting, email, ticket, or source document reference.
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>
    /// Human-readable explanation for why the policy exists.
    /// </summary>
    public string? Rationale { get; init; }

    /// <summary>
    /// Optional change-control ticket, pull request, or approval record.
    /// </summary>
    public string? ChangeTicket { get; init; }
}
