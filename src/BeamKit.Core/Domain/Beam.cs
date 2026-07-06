namespace BeamKit.Core.Domain;

/// <summary>
/// Represents a treatment beam or arc in a plan.
/// </summary>
public sealed record Beam
{
    /// <summary>
    /// Creates a beam definition.
    /// </summary>
    public Beam(
        string id,
        string name,
        string modality,
        string energy,
        decimal? gantryAngleDegrees = null,
        decimal? monitorUnits = null)
    {
        Id = Guard.Required(id, nameof(id));
        Name = Guard.Required(name, nameof(name));
        Modality = Guard.Required(modality, nameof(modality));
        Energy = Guard.Required(energy, nameof(energy));
        GantryAngleDegrees = gantryAngleDegrees;
        MonitorUnits = monitorUnits;
    }

    /// <summary>
    /// Stable beam identifier.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Human-readable beam name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Beam modality, such as photon VMAT.
    /// </summary>
    public string Modality { get; init; }

    /// <summary>
    /// Beam energy label, such as 6X.
    /// </summary>
    public string Energy { get; init; }

    /// <summary>
    /// Gantry angle in degrees when the beam has a single nominal angle.
    /// </summary>
    public decimal? GantryAngleDegrees { get; init; }

    /// <summary>
    /// Monitor units assigned to the beam, when available.
    /// </summary>
    public decimal? MonitorUnits { get; init; }
}
