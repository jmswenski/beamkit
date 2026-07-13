namespace BeamKit.Safety;

/// <summary>
/// Versioned registry of clinical hazards and safety controls for a BeamKit feature or deployment.
/// </summary>
public sealed record ClinicalSafetyRegistry
{
    /// <summary>
    /// Creates an empty registry for JSON deserialization.
    /// </summary>
    public ClinicalSafetyRegistry()
    {
        Id = string.Empty;
        Name = string.Empty;
        Version = string.Empty;
        Hazards = Array.Empty<ClinicalHazard>();
        Controls = Array.Empty<SafetyControl>();
    }

    /// <summary>
    /// Creates a clinical safety registry.
    /// </summary>
    public ClinicalSafetyRegistry(
        string id,
        string name,
        string version,
        IEnumerable<ClinicalHazard> hazards,
        IEnumerable<SafetyControl> controls,
        string? owner = null,
        string? description = null)
    {
        Id = SafetyText.Required(id, nameof(id));
        Name = SafetyText.Required(name, nameof(name));
        Version = SafetyText.Required(version, nameof(version));
        Owner = SafetyText.Optional(owner);
        Description = SafetyText.Optional(description);
        Hazards = hazards?.ToArray() ?? throw new ArgumentNullException(nameof(hazards));
        Controls = controls?.ToArray() ?? throw new ArgumentNullException(nameof(controls));
    }

    /// <summary>
    /// Stable registry id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Registry display name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Registry version.
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Registry owner.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Registry summary.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Hazards tracked by the registry.
    /// </summary>
    public IReadOnlyList<ClinicalHazard> Hazards { get; init; }

    /// <summary>
    /// Safety controls tracked by the registry.
    /// </summary>
    public IReadOnlyList<SafetyControl> Controls { get; init; }

    /// <summary>
    /// Hazards that still block acceptance.
    /// </summary>
    public IReadOnlyList<ClinicalHazard> BlockingHazards => Hazards.Where(hazard => hazard.BlocksAcceptance).ToArray();

    /// <summary>
    /// Finds a hazard by id.
    /// </summary>
    public ClinicalHazard? FindHazard(string id)
    {
        return Hazards.FirstOrDefault(hazard => string.Equals(hazard.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a safety control by id.
    /// </summary>
    public SafetyControl? FindControl(string id)
    {
        return Controls.FirstOrDefault(control => string.Equals(control.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
