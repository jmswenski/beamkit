namespace BeamKit.Core.Domain;

/// <summary>
/// Describes dose-grid spacing in millimeters.
/// </summary>
public sealed record DoseGrid
{
    /// <summary>
    /// Creates dose-grid spacing metadata.
    /// </summary>
    public DoseGrid(decimal spacingXMm, decimal spacingYMm, decimal spacingZMm)
    {
        if (spacingXMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spacingXMm), spacingXMm, "Spacing must be positive.");
        }

        if (spacingYMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spacingYMm), spacingYMm, "Spacing must be positive.");
        }

        if (spacingZMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spacingZMm), spacingZMm, "Spacing must be positive.");
        }

        SpacingXMm = spacingXMm;
        SpacingYMm = spacingYMm;
        SpacingZMm = spacingZMm;
    }

    /// <summary>
    /// X-axis spacing in millimeters.
    /// </summary>
    public decimal SpacingXMm { get; init; }

    /// <summary>
    /// Y-axis spacing in millimeters.
    /// </summary>
    public decimal SpacingYMm { get; init; }

    /// <summary>
    /// Z-axis spacing in millimeters.
    /// </summary>
    public decimal SpacingZMm { get; init; }

    /// <summary>
    /// Largest spacing dimension in millimeters.
    /// </summary>
    public decimal MaxSpacingMm => Math.Max(SpacingXMm, Math.Max(SpacingYMm, SpacingZMm));
}
