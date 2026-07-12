namespace BeamKit.Protocols;

/// <summary>
/// Governance state for an RT-PX package.
/// </summary>
public enum ProtocolPackageStatus
{
    /// <summary>
    /// Protocol package is being authored and must not be promoted for clinical use.
    /// </summary>
    Draft,

    /// <summary>
    /// Protocol package is waiting for clinical, physics, or informatics review.
    /// </summary>
    InReview,

    /// <summary>
    /// Protocol package has local approval metadata.
    /// </summary>
    Approved,

    /// <summary>
    /// Protocol package is retained for traceability but should not be used for new cases.
    /// </summary>
    Retired
}

/// <summary>
/// Clinical importance of a protocol requirement.
/// </summary>
public enum ProtocolRequirementLevel
{
    /// <summary>
    /// Requirement is informational and should not block a plan gate.
    /// </summary>
    Informational,

    /// <summary>
    /// Requirement is expected but non-blocking.
    /// </summary>
    Recommended,

    /// <summary>
    /// Requirement is mandatory and should block promotion when it fails.
    /// </summary>
    Required
}

/// <summary>
/// Type of structure described by an RT-PX package.
/// </summary>
public enum ProtocolStructureRole
{
    /// <summary>
    /// Target volume.
    /// </summary>
    Target,

    /// <summary>
    /// Organ at risk.
    /// </summary>
    OrganAtRisk,

    /// <summary>
    /// External body contour.
    /// </summary>
    External,

    /// <summary>
    /// Optimization, ring, avoidance, or helper structure.
    /// </summary>
    PlanningHelper,

    /// <summary>
    /// Other protocol-specific structure.
    /// </summary>
    Other
}
