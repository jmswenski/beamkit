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
        decimal? monitorUnits = null,
        string? treatmentUnitId = null,
        string? techniqueId = null,
        bool isSetupField = false,
        IEnumerable<BeamControlPoint>? controlPoints = null,
        string? beamModelId = null,
        bool? jawTrackingEnabled = null)
    {
        Id = Guard.Required(id, nameof(id));
        Name = Guard.Required(name, nameof(name));
        Modality = Guard.Required(modality, nameof(modality));
        Energy = Guard.Required(energy, nameof(energy));
        GantryAngleDegrees = gantryAngleDegrees;
        MonitorUnits = monitorUnits;
        TreatmentUnitId = string.IsNullOrWhiteSpace(treatmentUnitId) ? null : treatmentUnitId.Trim();
        TechniqueId = string.IsNullOrWhiteSpace(techniqueId) ? null : techniqueId.Trim();
        IsSetupField = isSetupField;
        ControlPoints = Guard.ToReadOnlyList(controlPoints);
        BeamModelId = string.IsNullOrWhiteSpace(beamModelId) ? null : beamModelId.Trim();
        JawTrackingEnabled = jawTrackingEnabled;
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

    /// <summary>
    /// Treatment unit identifier, when available.
    /// </summary>
    public string? TreatmentUnitId { get; init; }

    /// <summary>
    /// Beam technique identifier, such as VMAT, IMRT, DCA, or setup.
    /// </summary>
    public string? TechniqueId { get; init; }

    /// <summary>
    /// Indicates whether this beam is a setup/imaging field rather than a treatment field.
    /// </summary>
    public bool IsSetupField { get; init; }

    /// <summary>
    /// Vendor-neutral control-point geometry and meterset samples.
    /// </summary>
    public IReadOnlyList<BeamControlPoint> ControlPoints { get; init; }

    /// <summary>
    /// Beam model identifier used for dose calculation or machine commissioning, when available.
    /// </summary>
    public string? BeamModelId { get; init; }

    /// <summary>
    /// Indicates whether jaw tracking was enabled for this beam, when available.
    /// </summary>
    public bool? JawTrackingEnabled { get; init; }

    /// <summary>
    /// Indicates whether the energy label appears to describe a flattening-filter-free beam.
    /// </summary>
    public bool IsFlatteningFilterFree =>
        Energy.Contains("FFF", StringComparison.OrdinalIgnoreCase)
        || Energy.Contains("F-", StringComparison.OrdinalIgnoreCase);
}
