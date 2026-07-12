namespace BeamKit.Protocols;

/// <summary>
/// Prescription intent represented by an RT-PX package.
/// </summary>
public sealed record ProtocolPrescription
{
    /// <summary>
    /// Creates an empty prescription requirement for JSON deserialization.
    /// </summary>
    public ProtocolPrescription()
    {
        Id = string.Empty;
        Target = string.Empty;
    }

    /// <summary>
    /// Creates a prescription requirement.
    /// </summary>
    public ProtocolPrescription(
        string id,
        string target,
        decimal totalDoseGy,
        int fractionCount,
        decimal? dosePerFractionGy = null,
        string? technique = null,
        string? energy = null,
        ProtocolRequirementLevel level = ProtocolRequirementLevel.Required,
        string? description = null,
        ProtocolSourceReference? source = null)
    {
        Id = ProtocolText.Required(id, nameof(id));
        Target = ProtocolText.Required(target, nameof(target));
        TotalDoseGy = totalDoseGy;
        FractionCount = fractionCount;
        DosePerFractionGy = dosePerFractionGy;
        Technique = ProtocolText.Optional(technique);
        Energy = ProtocolText.Optional(energy);
        Level = level;
        Description = ProtocolText.Optional(description);
        Source = source;
    }

    /// <summary>
    /// Stable prescription id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Target structure name for this prescription phase.
    /// </summary>
    public string Target { get; init; }

    /// <summary>
    /// Total prescribed dose in Gy.
    /// </summary>
    public decimal TotalDoseGy { get; init; }

    /// <summary>
    /// Number of fractions.
    /// </summary>
    public int FractionCount { get; init; }

    /// <summary>
    /// Optional expected dose per fraction in Gy.
    /// </summary>
    public decimal? DosePerFractionGy { get; init; }

    /// <summary>
    /// Optional expected technique label such as VMAT, IMRT, SBRT, or 3D.
    /// </summary>
    public string? Technique { get; init; }

    /// <summary>
    /// Optional expected energy label.
    /// </summary>
    public string? Energy { get; init; }

    /// <summary>
    /// Requirement level.
    /// </summary>
    public ProtocolRequirementLevel Level { get; init; }

    /// <summary>
    /// Human-readable prescription note.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Source-document reference.
    /// </summary>
    public ProtocolSourceReference? Source { get; init; }

    /// <summary>
    /// Effective dose per fraction.
    /// </summary>
    public decimal ComputedDosePerFractionGy => DosePerFractionGy ?? (FractionCount == 0 ? 0m : TotalDoseGy / FractionCount);
}
