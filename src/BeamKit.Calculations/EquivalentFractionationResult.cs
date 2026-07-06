namespace BeamKit.Calculations;

/// <summary>
/// Physical dose required to match a target EQD2 in a requested number of fractions.
/// </summary>
public sealed record EquivalentFractionationResult
{
    /// <summary>
    /// Creates an equivalent fractionation result.
    /// </summary>
    public EquivalentFractionationResult(FractionationScheme fractionation, decimal alphaBetaGy, decimal targetEqd2Gy)
    {
        Fractionation = fractionation ?? throw new ArgumentNullException(nameof(fractionation));
        AlphaBetaGy = alphaBetaGy;
        TargetEqd2Gy = targetEqd2Gy;
    }

    /// <summary>
    /// Equivalent physical fractionation.
    /// </summary>
    public FractionationScheme Fractionation { get; init; }

    /// <summary>
    /// Alpha/beta ratio in Gy.
    /// </summary>
    public decimal AlphaBetaGy { get; init; }

    /// <summary>
    /// Target EQD2 matched by the equivalent fractionation.
    /// </summary>
    public decimal TargetEqd2Gy { get; init; }
}
