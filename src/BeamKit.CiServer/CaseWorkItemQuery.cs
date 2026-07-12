namespace BeamKit.CiServer;

/// <summary>
/// Query options for hosted case work items.
/// </summary>
public sealed record CaseWorkItemQuery
{
    /// <summary>
    /// Maximum number of work items to return.
    /// </summary>
    public int Limit { get; init; } = 100;

    /// <summary>
    /// Optional status filter.
    /// </summary>
    public CaseWorkItemStatus? Status { get; init; }

    /// <summary>
    /// Optional case id filter.
    /// </summary>
    public string? CaseId { get; init; }

    /// <summary>
    /// Optional disease-site filter.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Optional staff id filter that matches either assigned dosimetrist or assigned physicist.
    /// </summary>
    public string? AssignedStaffId { get; init; }

    /// <summary>
    /// When true, returns only work items that count toward active workload.
    /// </summary>
    public bool ActiveOnly { get; init; }

    /// <summary>
    /// Limit clamped to a practical server-side range.
    /// </summary>
    public int ClampedLimit => Math.Clamp(Limit, 1, 500);
}
