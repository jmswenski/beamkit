namespace BeamKit.CiServer;

/// <summary>
/// Accepted clinical or physics variance for one protocol compliance finding.
/// </summary>
public sealed record ProtocolComplianceVariance
{
    /// <summary>
    /// Creates a compliance variance.
    /// </summary>
    public ProtocolComplianceVariance(
        string findingId,
        string acceptedBy,
        DateTimeOffset acceptedAtUtc,
        string rationale)
    {
        FindingId = CiServerText.Required(findingId, nameof(findingId));
        AcceptedBy = CiServerText.Required(acceptedBy, nameof(acceptedBy));
        AcceptedAtUtc = acceptedAtUtc;
        Rationale = CiServerText.Required(rationale, nameof(rationale));
    }

    /// <summary>
    /// Finding id covered by this variance.
    /// </summary>
    public string FindingId { get; init; }

    /// <summary>
    /// Reviewer who accepted the variance.
    /// </summary>
    public string AcceptedBy { get; init; }

    /// <summary>
    /// UTC timestamp when the variance was accepted.
    /// </summary>
    public DateTimeOffset AcceptedAtUtc { get; init; }

    /// <summary>
    /// Clinical, physics, or protocol rationale.
    /// </summary>
    public string Rationale { get; init; }
}
