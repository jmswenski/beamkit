namespace BeamKit.Protocols;

/// <summary>
/// Controls strictness for RT-PX authoring and acceptance validation.
/// </summary>
public sealed record RadiotherapyProtocolValidationOptions
{
    /// <summary>
    /// Draft-friendly validation suitable for authoring and research examples.
    /// </summary>
    public static RadiotherapyProtocolValidationOptions Default { get; } = new();

    /// <summary>
    /// Strict validation preset for hospital acceptance or clinical-pilot review.
    /// </summary>
    public static RadiotherapyProtocolValidationOptions ClinicalAcceptance { get; } = new()
    {
        RequireApprovedStatus = true,
        RequireOwner = true,
        RequireDescription = true,
        RequireSourceDocument = true,
        RequireSourceDocumentHash = true,
        RequireApprovalReference = true,
        RequireApprovalRationale = true,
        RequireApprovalReviewDueDate = true,
        TreatMissingRequirementSourcesAsErrors = true
    };

    /// <summary>
    /// Requires the package status to be Approved.
    /// </summary>
    public bool RequireApprovedStatus { get; init; }

    /// <summary>
    /// Requires a protocol owner.
    /// </summary>
    public bool RequireOwner { get; init; }

    /// <summary>
    /// Requires a human-readable protocol description.
    /// </summary>
    public bool RequireDescription { get; init; }

    /// <summary>
    /// Requires source-document metadata.
    /// </summary>
    public bool RequireSourceDocument { get; init; }

    /// <summary>
    /// Requires a source-document content hash.
    /// </summary>
    public bool RequireSourceDocumentHash { get; init; }

    /// <summary>
    /// Requires approval metadata to cite a committee, policy, ticket, meeting, or source document.
    /// </summary>
    public bool RequireApprovalReference { get; init; }

    /// <summary>
    /// Requires approval metadata to explain why the package is accepted.
    /// </summary>
    public bool RequireApprovalRationale { get; init; }

    /// <summary>
    /// Requires an approval re-review due date.
    /// </summary>
    public bool RequireApprovalReviewDueDate { get; init; }

    /// <summary>
    /// Converts missing structure, prescription, constraint, plan-check, and workflow source references into errors.
    /// </summary>
    public bool TreatMissingRequirementSourcesAsErrors { get; init; }
}
