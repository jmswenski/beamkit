namespace BeamKit.Deliverability;

/// <summary>
/// Technique-specific minimum MU/degree constraint for arc delivery.
/// </summary>
public sealed record MonitorUnitsPerDegreeConstraint
{
    /// <summary>
    /// Creates a MU/degree constraint.
    /// </summary>
    public MonitorUnitsPerDegreeConstraint(
        decimal minMonitorUnitsPerDegree,
        string? machineId = null,
        string? energy = null,
        string? techniqueId = null,
        string? diseaseSite = null)
    {
        if (minMonitorUnitsPerDegree <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minMonitorUnitsPerDegree), minMonitorUnitsPerDegree, "Minimum MU/degree must be positive.");
        }

        MinMonitorUnitsPerDegree = minMonitorUnitsPerDegree;
        MachineId = Normalize(machineId);
        Energy = Normalize(energy);
        TechniqueId = Normalize(techniqueId);
        DiseaseSite = Normalize(diseaseSite);
    }

    /// <summary>
    /// Minimum MU/degree.
    /// </summary>
    public decimal MinMonitorUnitsPerDegree { get; init; }

    /// <summary>
    /// Optional machine selector.
    /// </summary>
    public string? MachineId { get; init; }

    /// <summary>
    /// Optional energy selector.
    /// </summary>
    public string? Energy { get; init; }

    /// <summary>
    /// Optional technique selector.
    /// </summary>
    public string? TechniqueId { get; init; }

    /// <summary>
    /// Optional disease-site selector.
    /// </summary>
    public string? DiseaseSite { get; init; }

    internal int Specificity =>
        Count(MachineId)
        + Count(Energy)
        + Count(TechniqueId)
        + Count(DiseaseSite);

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int Count(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? 0 : 1;
    }
}
