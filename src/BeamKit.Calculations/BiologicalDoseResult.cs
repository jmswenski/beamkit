namespace BeamKit.Calculations;

/// <summary>
/// BED and EQD2 calculation for one fractionation scheme.
/// </summary>
public sealed record BiologicalDoseResult
{
    /// <summary>
    /// Creates a biological dose result.
    /// </summary>
    public BiologicalDoseResult(FractionationScheme fractionation, decimal alphaBetaGy, decimal bedGy, decimal eqd2Gy)
    {
        Fractionation = fractionation ?? throw new ArgumentNullException(nameof(fractionation));
        AlphaBetaGy = alphaBetaGy;
        BedGy = bedGy;
        Eqd2Gy = eqd2Gy;
    }

    /// <summary>
    /// Source fractionation.
    /// </summary>
    public FractionationScheme Fractionation { get; init; }

    /// <summary>
    /// Alpha/beta ratio in Gy.
    /// </summary>
    public decimal AlphaBetaGy { get; init; }

    /// <summary>
    /// Biologically effective dose in Gy.
    /// </summary>
    public decimal BedGy { get; init; }

    /// <summary>
    /// Equivalent dose in 2 Gy fractions.
    /// </summary>
    public decimal Eqd2Gy { get; init; }
}
