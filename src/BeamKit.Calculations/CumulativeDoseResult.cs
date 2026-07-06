namespace BeamKit.Calculations;

/// <summary>
/// Cumulative biological dose across multiple treatment courses.
/// </summary>
public sealed record CumulativeDoseResult
{
    /// <summary>
    /// Creates a cumulative dose result.
    /// </summary>
    public CumulativeDoseResult(decimal alphaBetaGy, IEnumerable<BiologicalDoseResult> components)
    {
        AlphaBetaGy = alphaBetaGy;
        Components = components?.ToArray() ?? throw new ArgumentNullException(nameof(components));
        if (Components.Count == 0)
        {
            throw new ArgumentException("At least one component is required.", nameof(components));
        }
    }

    /// <summary>
    /// Alpha/beta ratio in Gy.
    /// </summary>
    public decimal AlphaBetaGy { get; init; }

    /// <summary>
    /// Component biological dose calculations.
    /// </summary>
    public IReadOnlyList<BiologicalDoseResult> Components { get; init; }

    /// <summary>
    /// Sum of physical doses in Gy.
    /// </summary>
    public decimal TotalPhysicalDoseGy => Components.Sum(component => component.Fractionation.TotalDoseGy);

    /// <summary>
    /// Sum of BED values in Gy.
    /// </summary>
    public decimal TotalBedGy => Components.Sum(component => component.BedGy);

    /// <summary>
    /// Sum of EQD2 values in Gy.
    /// </summary>
    public decimal TotalEqd2Gy => Components.Sum(component => component.Eqd2Gy);
}
