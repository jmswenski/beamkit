namespace BeamKit.Calculations;

/// <summary>
/// Physical treatment dose and fraction count.
/// </summary>
public sealed record FractionationScheme
{
    /// <summary>
    /// Creates a fractionation scheme from total dose and number of fractions.
    /// </summary>
    public FractionationScheme(DoseValue totalDose, int fractions, string? label = null)
    {
        TotalDose = totalDose ?? throw new ArgumentNullException(nameof(totalDose));
        if (totalDose.Gy <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalDose), totalDose.Gy, "Total dose must be positive.");
        }

        if (fractions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fractions), fractions, "Fraction count must be positive.");
        }

        Fractions = fractions;
        Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
    }

    /// <summary>
    /// Optional label for the treatment course.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Total physical dose.
    /// </summary>
    public DoseValue TotalDose { get; init; }

    /// <summary>
    /// Number of treatment fractions.
    /// </summary>
    public int Fractions { get; init; }

    /// <summary>
    /// Total physical dose in Gy.
    /// </summary>
    public decimal TotalDoseGy => TotalDose.Gy;

    /// <summary>
    /// Total physical dose in cGy.
    /// </summary>
    public decimal TotalDoseCGy => TotalDose.CGy;

    /// <summary>
    /// Dose per fraction in Gy.
    /// </summary>
    public decimal DosePerFractionGy => TotalDoseGy / Fractions;

    /// <summary>
    /// Dose per fraction in cGy.
    /// </summary>
    public decimal DosePerFractionCGy => TotalDoseCGy / Fractions;

    /// <summary>
    /// Creates a scheme from total dose in Gy.
    /// </summary>
    public static FractionationScheme FromTotalDoseGy(decimal totalDoseGy, int fractions, string? label = null)
    {
        return new FractionationScheme(DoseValue.FromGy(totalDoseGy), fractions, label);
    }

    /// <summary>
    /// Creates a scheme from total dose in cGy.
    /// </summary>
    public static FractionationScheme FromTotalDoseCGy(decimal totalDoseCGy, int fractions, string? label = null)
    {
        return new FractionationScheme(DoseValue.FromCGy(totalDoseCGy), fractions, label);
    }

    /// <summary>
    /// Creates a scheme from dose per fraction in Gy.
    /// </summary>
    public static FractionationScheme FromDosePerFractionGy(decimal dosePerFractionGy, int fractions, string? label = null)
    {
        return FromTotalDoseGy(dosePerFractionGy * fractions, fractions, label);
    }

    /// <summary>
    /// Creates a scheme from dose per fraction in cGy.
    /// </summary>
    public static FractionationScheme FromDosePerFractionCGy(decimal dosePerFractionCGy, int fractions, string? label = null)
    {
        return FromTotalDoseCGy(dosePerFractionCGy * fractions, fractions, label);
    }
}
