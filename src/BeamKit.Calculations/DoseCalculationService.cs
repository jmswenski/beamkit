namespace BeamKit.Calculations;

/// <summary>
/// Performs common physical and biological dose calculations.
/// </summary>
public sealed class DoseCalculationService
{
    /// <summary>
    /// Calculates BED and EQD2 for one fractionation scheme.
    /// </summary>
    public BiologicalDoseResult Calculate(FractionationScheme fractionation, decimal alphaBetaGy)
    {
        ArgumentNullException.ThrowIfNull(fractionation);
        ValidateAlphaBeta(alphaBetaGy);

        var bedGy = CalculateBedGy(fractionation.TotalDoseGy, fractionation.Fractions, alphaBetaGy);
        var eqd2Gy = CalculateEqd2Gy(fractionation.TotalDoseGy, fractionation.Fractions, alphaBetaGy);
        return new BiologicalDoseResult(fractionation, alphaBetaGy, bedGy, eqd2Gy);
    }

    /// <summary>
    /// Calculates biologically effective dose in Gy.
    /// </summary>
    public decimal CalculateBedGy(decimal totalDoseGy, int fractions, decimal alphaBetaGy)
    {
        var fractionation = FractionationScheme.FromTotalDoseGy(totalDoseGy, fractions);
        ValidateAlphaBeta(alphaBetaGy);
        return fractionation.TotalDoseGy * (1m + (fractionation.DosePerFractionGy / alphaBetaGy));
    }

    /// <summary>
    /// Calculates equivalent dose in 2 Gy fractions.
    /// </summary>
    public decimal CalculateEqd2Gy(decimal totalDoseGy, int fractions, decimal alphaBetaGy)
    {
        var bedGy = CalculateBedGy(totalDoseGy, fractions, alphaBetaGy);
        return bedGy / (1m + (2m / alphaBetaGy));
    }

    /// <summary>
    /// Calculates the physical total dose required to produce a target EQD2 in the requested number of fractions.
    /// </summary>
    public decimal CalculateTotalDoseForEqd2Gy(decimal targetEqd2Gy, int fractions, decimal alphaBetaGy)
    {
        if (targetEqd2Gy <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetEqd2Gy), targetEqd2Gy, "Target EQD2 must be positive.");
        }

        if (fractions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fractions), fractions, "Fraction count must be positive.");
        }

        ValidateAlphaBeta(alphaBetaGy);

        var discriminant = (double)((alphaBetaGy * alphaBetaGy) + (4m * targetEqd2Gy * (alphaBetaGy + 2m) / fractions));
        var doseGy = fractions * ((decimal)Math.Sqrt(discriminant) - alphaBetaGy) / 2m;
        return doseGy;
    }

    /// <summary>
    /// Calculates an equivalent physical dose in a requested number of fractions.
    /// </summary>
    public EquivalentFractionationResult CalculateEquivalentFractionation(
        FractionationScheme source,
        int targetFractions,
        decimal alphaBetaGy,
        string? label = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var sourceEqd2Gy = Calculate(source, alphaBetaGy).Eqd2Gy;
        var totalDoseGy = CalculateTotalDoseForEqd2Gy(sourceEqd2Gy, targetFractions, alphaBetaGy);
        var fractionation = FractionationScheme.FromTotalDoseGy(totalDoseGy, targetFractions, label);
        return new EquivalentFractionationResult(fractionation, alphaBetaGy, sourceEqd2Gy);
    }

    /// <summary>
    /// Calculates cumulative physical dose, BED, and EQD2 across multiple courses.
    /// </summary>
    public CumulativeDoseResult CalculateCumulative(IEnumerable<FractionationScheme> courses, decimal alphaBetaGy)
    {
        ArgumentNullException.ThrowIfNull(courses);
        ValidateAlphaBeta(alphaBetaGy);

        var components = courses.Select(course => Calculate(course, alphaBetaGy)).ToArray();
        return new CumulativeDoseResult(alphaBetaGy, components);
    }

    private static void ValidateAlphaBeta(decimal alphaBetaGy)
    {
        if (alphaBetaGy <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(alphaBetaGy), alphaBetaGy, "Alpha/beta must be positive.");
        }
    }
}
