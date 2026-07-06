namespace BeamKit.Core.Domain;

/// <summary>
/// Vendor-neutral beam control point used for deliverability checks.
/// </summary>
public sealed record BeamControlPoint
{
    /// <summary>
    /// Creates a beam control point.
    /// </summary>
    public BeamControlPoint(
        int index,
        decimal? gantryAngleDegrees = null,
        decimal? cumulativeMetersetWeight = null,
        BeamJawPositions? jawPositions = null)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Control point index cannot be negative.");
        }

        if (cumulativeMetersetWeight is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(cumulativeMetersetWeight), cumulativeMetersetWeight, "Cumulative meterset weight must be between 0 and 1.");
        }

        Index = index;
        GantryAngleDegrees = gantryAngleDegrees;
        CumulativeMetersetWeight = cumulativeMetersetWeight;
        JawPositions = jawPositions;
    }

    /// <summary>
    /// Zero-based control point index.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Gantry angle in degrees, when available.
    /// </summary>
    public decimal? GantryAngleDegrees { get; init; }

    /// <summary>
    /// Cumulative meterset weight from 0 to 1, when available.
    /// </summary>
    public decimal? CumulativeMetersetWeight { get; init; }

    /// <summary>
    /// Jaw-defined field geometry, when available.
    /// </summary>
    public BeamJawPositions? JawPositions { get; init; }
}
