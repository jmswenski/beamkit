namespace BeamKit.Safety;

/// <summary>
/// Tracked clinical safety hazard for BeamKit software behavior or deployment use.
/// </summary>
public sealed record ClinicalHazard
{
    /// <summary>
    /// Creates an empty hazard for JSON deserialization.
    /// </summary>
    public ClinicalHazard()
    {
        Id = string.Empty;
        Title = string.Empty;
        HazardousSituation = string.Empty;
        PotentialHarm = string.Empty;
        Severity = SafetySeverity.Major;
        Probability = SafetyProbability.Occasional;
        ResidualRisk = SafetyRiskLevel.High;
        Status = HazardStatus.Open;
        ControlIds = Array.Empty<string>();
        EvidenceIds = Array.Empty<string>();
    }

    /// <summary>
    /// Creates a clinical hazard.
    /// </summary>
    public ClinicalHazard(
        string id,
        string title,
        string hazardousSituation,
        string potentialHarm,
        SafetySeverity severity,
        SafetyProbability probability,
        SafetyRiskLevel residualRisk,
        HazardStatus status = HazardStatus.Open,
        string? owner = null,
        IEnumerable<string>? controlIds = null,
        IEnumerable<string>? evidenceIds = null)
    {
        Id = SafetyText.Required(id, nameof(id));
        Title = SafetyText.Required(title, nameof(title));
        HazardousSituation = SafetyText.Required(hazardousSituation, nameof(hazardousSituation));
        PotentialHarm = SafetyText.Required(potentialHarm, nameof(potentialHarm));
        Severity = severity;
        Probability = probability;
        ResidualRisk = residualRisk;
        Status = status;
        Owner = SafetyText.Optional(owner);
        ControlIds = SafetyText.CleanList(controlIds);
        EvidenceIds = SafetyText.CleanList(evidenceIds);
    }

    /// <summary>
    /// Stable hazard id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Hazard title.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Situation where the hazard can occur.
    /// </summary>
    public string HazardousSituation { get; init; }

    /// <summary>
    /// Potential harm if the hazard is not controlled.
    /// </summary>
    public string PotentialHarm { get; init; }

    /// <summary>
    /// Harm severity.
    /// </summary>
    public SafetySeverity Severity { get; init; }

    /// <summary>
    /// Expected likelihood before or after controls, depending on local risk-method convention.
    /// </summary>
    public SafetyProbability Probability { get; init; }

    /// <summary>
    /// Residual risk after listed controls.
    /// </summary>
    public SafetyRiskLevel ResidualRisk { get; init; }

    /// <summary>
    /// Hazard lifecycle status.
    /// </summary>
    public HazardStatus Status { get; init; }

    /// <summary>
    /// Hazard owner.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Linked safety control ids.
    /// </summary>
    public IReadOnlyList<string> ControlIds { get; init; }

    /// <summary>
    /// Linked validation evidence ids.
    /// </summary>
    public IReadOnlyList<string> EvidenceIds { get; init; }

    /// <summary>
    /// Indicates whether the hazard blocks clinical acceptance.
    /// </summary>
    public bool BlocksAcceptance => Status == HazardStatus.Open || ResidualRisk == SafetyRiskLevel.Unacceptable;
}
