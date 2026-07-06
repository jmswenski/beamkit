namespace BeamKit.Core.Domain;

/// <summary>
/// Represents calculated dose metadata and per-structure statistics.
/// </summary>
public sealed record Dose
{
    /// <summary>
    /// Creates a dose object.
    /// </summary>
    public Dose(string id, DoseGrid grid, IEnumerable<DoseStatistics>? statistics = null)
    {
        Id = Guard.Required(id, nameof(id));
        Grid = grid ?? throw new ArgumentNullException(nameof(grid));
        Statistics = Guard.ToReadOnlyList(statistics);
    }

    /// <summary>
    /// Stable dose identifier.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Dose-grid spacing metadata.
    /// </summary>
    public DoseGrid Grid { get; init; }

    /// <summary>
    /// Per-structure dose statistics keyed by structure identifier.
    /// </summary>
    public IReadOnlyList<DoseStatistics> Statistics { get; init; }

    /// <summary>
    /// Finds dose statistics by structure identifier.
    /// </summary>
    public DoseStatistics? FindStatistics(string structureId)
    {
        return Statistics.FirstOrDefault(statistics =>
            string.Equals(statistics.StructureId, structureId, StringComparison.OrdinalIgnoreCase));
    }
}
