namespace BeamKit.Core.Domain;

/// <summary>
/// Jaw positions in centimeters relative to isocenter.
/// </summary>
public sealed record BeamJawPositions
{
    /// <summary>
    /// Creates jaw positions.
    /// </summary>
    public BeamJawPositions(decimal x1Cm, decimal x2Cm, decimal y1Cm, decimal y2Cm)
    {
        if (x2Cm <= x1Cm)
        {
            throw new ArgumentException("X2 must be greater than X1.", nameof(x2Cm));
        }

        if (y2Cm <= y1Cm)
        {
            throw new ArgumentException("Y2 must be greater than Y1.", nameof(y2Cm));
        }

        X1Cm = x1Cm;
        X2Cm = x2Cm;
        Y1Cm = y1Cm;
        Y2Cm = y2Cm;
    }

    /// <summary>
    /// Negative X jaw coordinate in centimeters.
    /// </summary>
    public decimal X1Cm { get; init; }

    /// <summary>
    /// Positive X jaw coordinate in centimeters.
    /// </summary>
    public decimal X2Cm { get; init; }

    /// <summary>
    /// Negative Y jaw coordinate in centimeters.
    /// </summary>
    public decimal Y1Cm { get; init; }

    /// <summary>
    /// Positive Y jaw coordinate in centimeters.
    /// </summary>
    public decimal Y2Cm { get; init; }

    /// <summary>
    /// Field width in centimeters.
    /// </summary>
    public decimal WidthCm => X2Cm - X1Cm;

    /// <summary>
    /// Field length in centimeters.
    /// </summary>
    public decimal LengthCm => Y2Cm - Y1Cm;

    /// <summary>
    /// Largest jaw-defined field dimension in centimeters.
    /// </summary>
    public decimal LargestDimensionCm => Math.Max(WidthCm, LengthCm);
}
