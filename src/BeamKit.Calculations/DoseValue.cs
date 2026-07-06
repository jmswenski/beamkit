namespace BeamKit.Calculations;

/// <summary>
/// Physical dose value with unit conversion helpers.
/// </summary>
public sealed record DoseValue
{
    /// <summary>
    /// Creates a dose value.
    /// </summary>
    public DoseValue(decimal value, DoseUnit unit)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Dose cannot be negative.");
        }

        Value = value;
        Unit = unit;
    }

    /// <summary>
    /// Numeric value in the stated <see cref="Unit"/>.
    /// </summary>
    public decimal Value { get; init; }

    /// <summary>
    /// Unit for <see cref="Value"/>.
    /// </summary>
    public DoseUnit Unit { get; init; }

    /// <summary>
    /// Value converted to Gy.
    /// </summary>
    public decimal Gy => Unit == DoseUnit.Gy ? Value : Value / 100m;

    /// <summary>
    /// Value converted to cGy.
    /// </summary>
    public decimal CGy => Unit == DoseUnit.CGy ? Value : Value * 100m;

    /// <summary>
    /// Creates a dose value in Gy.
    /// </summary>
    public static DoseValue FromGy(decimal value)
    {
        return new DoseValue(value, DoseUnit.Gy);
    }

    /// <summary>
    /// Creates a dose value in cGy.
    /// </summary>
    public static DoseValue FromCGy(decimal value)
    {
        return new DoseValue(value, DoseUnit.CGy);
    }

    /// <summary>
    /// Converts this dose value to a requested unit.
    /// </summary>
    public DoseValue ConvertTo(DoseUnit unit)
    {
        return unit == DoseUnit.Gy ? FromGy(Gy) : FromCGy(CGy);
    }
}
