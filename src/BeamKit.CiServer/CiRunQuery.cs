using BeamKit.Check;

namespace BeamKit.CiServer;

/// <summary>
/// Query filters for hosted BeamKit CI run history.
/// </summary>
public sealed record CiRunQuery
{
    /// <summary>
    /// Maximum number of runs to return.
    /// </summary>
    public int Limit { get; init; } = 50;

    /// <summary>
    /// Optional status filter.
    /// </summary>
    public BeamKitCheckStatus? Status { get; init; }

    /// <summary>
    /// Optional synthetic case id filter.
    /// </summary>
    public string? SyntheticCaseId { get; init; }

    /// <summary>
    /// Optional branch filter.
    /// </summary>
    public string? Branch { get; init; }

    /// <summary>
    /// Optional inclusive lower bound for creation time.
    /// </summary>
    public DateTimeOffset? CreatedFromUtc { get; init; }

    /// <summary>
    /// Optional inclusive upper bound for creation time.
    /// </summary>
    public DateTimeOffset? CreatedToUtc { get; init; }

    /// <summary>
    /// Clamped limit suitable for local storage queries.
    /// </summary>
    public int ClampedLimit => Math.Clamp(Limit, 1, 500);
}
