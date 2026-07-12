namespace BeamKit.Safety;

/// <summary>
/// Safety case for a BeamKit feature, workflow, deployment, or integration.
/// </summary>
public sealed record ClinicalSafetyCase
{
    /// <summary>
    /// Creates a clinical safety case.
    /// </summary>
    public ClinicalSafetyCase(
        string id,
        string title,
        ClinicalUseClassification intendedUse,
        IEnumerable<ClinicalHazard> hazards,
        SafetyControlChecklist controlChecklist,
        IEnumerable<ValidationEvidencePackage> evidencePackages,
        string? owner = null,
        string? scope = null)
    {
        Id = SafetyText.Required(id, nameof(id));
        Title = SafetyText.Required(title, nameof(title));
        IntendedUse = intendedUse;
        Owner = SafetyText.Optional(owner);
        Scope = SafetyText.Optional(scope);
        Hazards = hazards?.ToArray() ?? throw new ArgumentNullException(nameof(hazards));
        ControlChecklist = controlChecklist ?? throw new ArgumentNullException(nameof(controlChecklist));
        EvidencePackages = evidencePackages?.ToArray() ?? throw new ArgumentNullException(nameof(evidencePackages));
    }

    /// <summary>
    /// Safety case id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Safety case title.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Intended use covered by this safety case.
    /// </summary>
    public ClinicalUseClassification IntendedUse { get; init; }

    /// <summary>
    /// Safety case owner.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Safety case scope.
    /// </summary>
    public string? Scope { get; init; }

    /// <summary>
    /// Hazards included in the case.
    /// </summary>
    public IReadOnlyList<ClinicalHazard> Hazards { get; init; }

    /// <summary>
    /// Required safety controls.
    /// </summary>
    public SafetyControlChecklist ControlChecklist { get; init; }

    /// <summary>
    /// Evidence packages supporting the case.
    /// </summary>
    public IReadOnlyList<ValidationEvidencePackage> EvidencePackages { get; init; }

    /// <summary>
    /// Hazards that still block acceptance.
    /// </summary>
    public IReadOnlyList<ClinicalHazard> BlockingHazards => Hazards.Where(hazard => hazard.BlocksAcceptance).ToArray();

    /// <summary>
    /// Indicates whether the case has no blocking hazards, a complete checklist, and at least one evidence package.
    /// </summary>
    public bool IsAcceptable => BlockingHazards.Count == 0 && ControlChecklist.IsComplete && EvidencePackages.Count > 0;
}
