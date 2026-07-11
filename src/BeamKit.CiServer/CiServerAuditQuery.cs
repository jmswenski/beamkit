namespace BeamKit.CiServer;

/// <summary>
/// Query for listing CI server audit events.
/// </summary>
public sealed record CiServerAuditQuery
{
    /// <summary>
    /// Maximum number of events to return.
    /// </summary>
    public int Limit { get; init; } = 100;

    /// <summary>
    /// Optional action filter.
    /// </summary>
    public string? Action { get; init; }

    /// <summary>
    /// Optional run id filter.
    /// </summary>
    public string? RunId { get; init; }

    /// <summary>
    /// Optional case id filter.
    /// </summary>
    public string? CaseId { get; init; }

    /// <summary>
    /// Clamped result limit.
    /// </summary>
    public int ClampedLimit => Math.Clamp(Limit, 1, 1_000);
}
