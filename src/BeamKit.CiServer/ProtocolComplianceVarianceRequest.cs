namespace BeamKit.CiServer;

/// <summary>
/// Request to accept a variance for one compliance finding.
/// </summary>
public sealed record ProtocolComplianceVarianceRequest
{
    /// <summary>
    /// Finding id to mark as accepted by variance.
    /// </summary>
    public string? FindingId { get; init; }

    /// <summary>
    /// Reviewer accepting the variance.
    /// </summary>
    public string? AcceptedBy { get; init; }

    /// <summary>
    /// Clinical, physics, or protocol rationale.
    /// </summary>
    public string? Rationale { get; init; }
}
