using BeamKit.Protocols;

namespace BeamKit.Protocols.Acceptance;

/// <summary>
/// Institution-specific RT-PX acceptance profile.
/// </summary>
public sealed record RtpxInstitutionProfile
{
    /// <summary>
    /// Creates an empty profile for JSON deserialization.
    /// </summary>
    public RtpxInstitutionProfile()
    {
        Institution = string.Empty;
        StructureMappings = Array.Empty<RtpxStructureMapping>();
        Tags = Array.Empty<string>();
        RequireExplicitStructureMappings = true;
    }

    /// <summary>
    /// Creates an institution profile.
    /// </summary>
    public RtpxInstitutionProfile(
        string institution,
        IEnumerable<RtpxStructureMapping>? structureMappings = null,
        bool requireExplicitStructureMappings = true,
        string? acceptedBy = null,
        DateOnly? effectiveDate = null,
        string? reviewedBy = null,
        DateOnly? reviewDueDate = null,
        string? localPolicyReference = null,
        string? rationale = null,
        string? changeTicket = null,
        string? owner = null,
        IEnumerable<string>? tags = null)
    {
        Institution = AcceptanceText.Required(institution, nameof(institution));
        StructureMappings = structureMappings?.ToArray() ?? Array.Empty<RtpxStructureMapping>();
        RequireExplicitStructureMappings = requireExplicitStructureMappings;
        AcceptedBy = AcceptanceText.Optional(acceptedBy);
        EffectiveDate = effectiveDate;
        ReviewedBy = AcceptanceText.Optional(reviewedBy);
        ReviewDueDate = reviewDueDate;
        LocalPolicyReference = AcceptanceText.Optional(localPolicyReference);
        Rationale = AcceptanceText.Optional(rationale);
        ChangeTicket = AcceptanceText.Optional(changeTicket);
        Owner = AcceptanceText.Optional(owner);
        Tags = AcceptanceText.CleanList(tags);
    }

    /// <summary>
    /// Institution or clinic name.
    /// </summary>
    public string Institution { get; init; }

    /// <summary>
    /// Structure mappings from protocol names to local names.
    /// </summary>
    public IReadOnlyList<RtpxStructureMapping> StructureMappings { get; init; }

    /// <summary>
    /// Indicates whether every protocol structure requires an explicit local mapping.
    /// </summary>
    public bool RequireExplicitStructureMappings { get; init; }

    /// <summary>
    /// Local approver or accepting owner.
    /// </summary>
    public string? AcceptedBy { get; init; }

    /// <summary>
    /// Date when local acceptance becomes effective.
    /// </summary>
    public DateOnly? EffectiveDate { get; init; }

    /// <summary>
    /// Local reviewer.
    /// </summary>
    public string? ReviewedBy { get; init; }

    /// <summary>
    /// Next review date.
    /// </summary>
    public DateOnly? ReviewDueDate { get; init; }

    /// <summary>
    /// Local policy, committee, meeting, or document-control reference.
    /// </summary>
    public string? LocalPolicyReference { get; init; }

    /// <summary>
    /// Rationale for local acceptance.
    /// </summary>
    public string? Rationale { get; init; }

    /// <summary>
    /// Local change-control ticket or pull request.
    /// </summary>
    public string? ChangeTicket { get; init; }

    /// <summary>
    /// Owner applied to accepted local packages.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Local routing or governance tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; }

    /// <summary>
    /// Creates local approval metadata when the profile has enough local acceptance data.
    /// </summary>
    public ProtocolApproval? CreateApproval()
    {
        if (string.IsNullOrWhiteSpace(AcceptedBy) && string.IsNullOrWhiteSpace(ReviewedBy) && !EffectiveDate.HasValue)
        {
            return null;
        }

        return new ProtocolApproval(
            ReviewedBy,
            AcceptedBy,
            EffectiveDate,
            ReviewDueDate,
            LocalPolicyReference,
            Rationale,
            ChangeTicket);
    }
}
