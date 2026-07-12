namespace BeamKit.CiServer;

/// <summary>
/// API-safe summary of an RT-PX package acceptance record.
/// </summary>
public sealed record CiServerRtpxAcceptanceSummary
{
    /// <summary>
    /// Creates a summary from a stored RT-PX acceptance record.
    /// </summary>
    public CiServerRtpxAcceptanceSummary(CiServerRtpxAcceptanceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        Id = record.Id;
        CreatedAtUtc = record.CreatedAtUtc;
        Institution = record.Institution;
        Accepted = record.Accepted;
        Promoted = record.Promoted;
        RulePackId = record.RulePackId;
        VersionId = record.VersionId;
        SourceProtocolId = record.SourceProtocolId;
        SourceProtocolName = record.SourceProtocolName;
        SourceProtocolVersion = record.SourceProtocolVersion;
        LocalProtocolId = record.LocalProtocolId;
        PackageFingerprint = record.PackageFingerprint;
        InstitutionProfileFingerprint = record.InstitutionProfileFingerprint;
        EsapiSnapshotFingerprint = record.EsapiSnapshotFingerprint;
        HasEsapiEvidence = record.HasEsapiEvidence;
        ErrorCount = record.ErrorCount;
        WarningCount = record.WarningCount;
        OutputDirectory = record.OutputDirectory;
        ReviewStatus = record.ReviewStatus;
        ReviewUpdatedAtUtc = record.ReviewUpdatedAtUtc;
        ReviewedBy = record.ReviewedBy;
        ReviewNote = record.ReviewNote;
        ApprovedBy = record.ApprovedBy;
        ApprovedAtUtc = record.ApprovedAtUtc;
        ApprovalNote = record.ApprovalNote;
        RejectedBy = record.RejectedBy;
        RejectedAtUtc = record.RejectedAtUtc;
        RejectionNote = record.RejectionNote;
        DiffAcknowledgedBy = record.DiffAcknowledgedBy;
        DiffAcknowledgedAtUtc = record.DiffAcknowledgedAtUtc;
        AcknowledgedDiffChangeIds = record.AcknowledgedDiffChangeIds;
    }

    /// <summary>
    /// Acceptance record id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// UTC timestamp when the record was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Institution that accepted or reviewed the package.
    /// </summary>
    public string Institution { get; init; }

    /// <summary>
    /// Indicates whether the package was accepted without blocking errors.
    /// </summary>
    public bool Accepted { get; init; }

    /// <summary>
    /// Indicates whether the generated rule-pack version is active.
    /// </summary>
    public bool Promoted { get; init; }

    /// <summary>
    /// Managed rule-pack id created from the accepted package.
    /// </summary>
    public string? RulePackId { get; init; }

    /// <summary>
    /// Managed rule-pack version id created from the accepted package.
    /// </summary>
    public string? VersionId { get; init; }

    /// <summary>
    /// Source RT-PX protocol id.
    /// </summary>
    public string SourceProtocolId { get; init; }

    /// <summary>
    /// Source RT-PX protocol name.
    /// </summary>
    public string SourceProtocolName { get; init; }

    /// <summary>
    /// Source RT-PX protocol version.
    /// </summary>
    public string SourceProtocolVersion { get; init; }

    /// <summary>
    /// Local accepted RT-PX protocol id.
    /// </summary>
    public string LocalProtocolId { get; init; }

    /// <summary>
    /// Fingerprint of the incoming package bytes.
    /// </summary>
    public string PackageFingerprint { get; init; }

    /// <summary>
    /// Fingerprint of the institution profile JSON.
    /// </summary>
    public string InstitutionProfileFingerprint { get; init; }

    /// <summary>
    /// Fingerprint of the optional ESAPI snapshot JSON.
    /// </summary>
    public string? EsapiSnapshotFingerprint { get; init; }

    /// <summary>
    /// Indicates whether ESAPI evidence was evaluated.
    /// </summary>
    public bool HasEsapiEvidence { get; init; }

    /// <summary>
    /// Number of blocking acceptance issues.
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Number of non-blocking acceptance warnings.
    /// </summary>
    public int WarningCount { get; init; }

    /// <summary>
    /// Server-local directory containing acceptance artifacts.
    /// </summary>
    public string OutputDirectory { get; init; }

    /// <summary>
    /// Durable review state for this draft.
    /// </summary>
    public RtpxDraftReviewStatus ReviewStatus { get; init; }

    /// <summary>
    /// UTC timestamp when review state last changed.
    /// </summary>
    public DateTimeOffset? ReviewUpdatedAtUtc { get; init; }

    /// <summary>
    /// Reviewer who most recently changed review state.
    /// </summary>
    public string? ReviewedBy { get; init; }

    /// <summary>
    /// Most recent review note or decision rationale.
    /// </summary>
    public string? ReviewNote { get; init; }

    /// <summary>
    /// Approver who approved the draft for promotion.
    /// </summary>
    public string? ApprovedBy { get; init; }

    /// <summary>
    /// UTC timestamp when the draft was approved.
    /// </summary>
    public DateTimeOffset? ApprovedAtUtc { get; init; }

    /// <summary>
    /// Approval rationale.
    /// </summary>
    public string? ApprovalNote { get; init; }

    /// <summary>
    /// Reviewer who rejected the draft.
    /// </summary>
    public string? RejectedBy { get; init; }

    /// <summary>
    /// UTC timestamp when the draft was rejected.
    /// </summary>
    public DateTimeOffset? RejectedAtUtc { get; init; }

    /// <summary>
    /// Rejection rationale.
    /// </summary>
    public string? RejectionNote { get; init; }

    /// <summary>
    /// Reviewer who most recently acknowledged protocol diff items.
    /// </summary>
    public string? DiffAcknowledgedBy { get; init; }

    /// <summary>
    /// UTC timestamp when protocol diff acknowledgement last changed.
    /// </summary>
    public DateTimeOffset? DiffAcknowledgedAtUtc { get; init; }

    /// <summary>
    /// Acknowledged protocol diff change ids.
    /// </summary>
    public IReadOnlyList<string> AcknowledgedDiffChangeIds { get; init; }
}
