namespace BeamKit.Structures;

/// <summary>
/// Defines one derived ring around a source target structure.
/// </summary>
public sealed record RingDefinition
{
    /// <summary>
    /// Creates a ring definition.
    /// </summary>
    public RingDefinition(int index, decimal innerMarginCm, decimal thicknessCm)
    {
        if (index <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Ring index must be positive.");
        }

        if (innerMarginCm < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(innerMarginCm), innerMarginCm, "Inner margin must be non-negative.");
        }

        if (thicknessCm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thicknessCm), thicknessCm, "Ring thickness must be positive.");
        }

        Index = index;
        InnerMarginCm = innerMarginCm;
        ThicknessCm = thicknessCm;
    }

    /// <summary>
    /// One-based ring number used in the generated structure name.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Gap from the source structure to the inner ring boundary, in centimeters.
    /// </summary>
    public decimal InnerMarginCm { get; init; }

    /// <summary>
    /// Ring thickness, in centimeters.
    /// </summary>
    public decimal ThicknessCm { get; init; }

    /// <summary>
    /// Margin from the source structure to the outer ring boundary, in centimeters.
    /// </summary>
    public decimal OuterMarginCm => InnerMarginCm + ThicknessCm;

    /// <summary>
    /// Gap from the source structure to the inner ring boundary, in millimeters.
    /// </summary>
    public decimal InnerMarginMm => InnerMarginCm * 10m;

    /// <summary>
    /// Ring thickness, in millimeters.
    /// </summary>
    public decimal ThicknessMm => ThicknessCm * 10m;

    /// <summary>
    /// Margin from the source structure to the outer ring boundary, in millimeters.
    /// </summary>
    public decimal OuterMarginMm => OuterMarginCm * 10m;
}
