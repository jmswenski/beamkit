namespace BeamKit.Dvh;

/// <summary>
/// Cumulative dose-volume histogram curve for one structure.
/// </summary>
public sealed record DvhCurve
{
    /// <summary>
    /// Creates a DVH curve.
    /// </summary>
    public DvhCurve(string structureId, IEnumerable<DvhPoint> points)
    {
        if (string.IsNullOrWhiteSpace(structureId))
        {
            throw new ArgumentException("Structure id is required.", nameof(structureId));
        }

        StructureId = structureId.Trim();
        Points = points?.OrderBy(point => point.DoseGy).ToArray() ?? throw new ArgumentNullException(nameof(points));
        if (Points.Count == 0)
        {
            throw new ArgumentException("A DVH curve requires at least one point.", nameof(points));
        }
    }

    /// <summary>
    /// Identifier of the structure represented by the curve.
    /// </summary>
    public string StructureId { get; init; }

    /// <summary>
    /// Cumulative DVH points ordered by dose.
    /// </summary>
    public IReadOnlyList<DvhPoint> Points { get; init; }
}
