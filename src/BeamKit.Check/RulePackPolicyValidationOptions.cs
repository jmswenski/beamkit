namespace BeamKit.Check;

/// <summary>
/// Controls how strict rule-pack policy validation should be.
/// </summary>
public sealed record RulePackPolicyValidationOptions
{
    /// <summary>
    /// Backward-compatible validation suitable for demos, local development, and research workflows.
    /// </summary>
    public static RulePackPolicyValidationOptions Default { get; } = new();

    /// <summary>
    /// Strict validation preset for clinical-pilot promotion gates.
    /// </summary>
    public static RulePackPolicyValidationOptions ClinicalPromotion { get; } = new()
    {
        RequireRulePackOwner = true,
        RequireRulePackDescription = true,
        RequireRulePackTags = true,
        RequireClinicalRuleDescription = true,
        RequireClinicalRuleReference = true,
        RequireClinicalRuleRationale = true,
        RequireClinicalRuleRequirementId = true,
        RequireClinicalRuleHazardLinks = true,
        RequireClinicalRuleControlLinks = true,
        RequirePlanCheckOwner = true,
        RequirePlanCheckReference = true,
        RequirePlanCheckRequirementId = true,
        RequirePlanCheckHazardLinks = true,
        RequirePlanCheckControlLinks = true,
        RequireNamingDictionary = true,
        RequireMachineProfile = true,
        RequireRequiredStructures = true
    };

    /// <summary>
    /// Requires a rule-pack owner.
    /// </summary>
    public bool RequireRulePackOwner { get; init; }

    /// <summary>
    /// Requires a human-readable rule-pack description.
    /// </summary>
    public bool RequireRulePackDescription { get; init; }

    /// <summary>
    /// Requires searchable rule-pack tags.
    /// </summary>
    public bool RequireRulePackTags { get; init; }

    /// <summary>
    /// Requires every executable clinical rule to have a description.
    /// </summary>
    public bool RequireClinicalRuleDescription { get; init; }

    /// <summary>
    /// Requires every executable clinical rule to cite its source policy or protocol.
    /// </summary>
    public bool RequireClinicalRuleReference { get; init; }

    /// <summary>
    /// Requires every executable clinical rule to include a rationale.
    /// </summary>
    public bool RequireClinicalRuleRationale { get; init; }

    /// <summary>
    /// Requires every executable clinical rule to link to a stable requirement id.
    /// </summary>
    public bool RequireClinicalRuleRequirementId { get; init; }

    /// <summary>
    /// Requires every executable clinical rule to link to at least one hazard id.
    /// </summary>
    public bool RequireClinicalRuleHazardLinks { get; init; }

    /// <summary>
    /// Requires every executable clinical rule to link to at least one safety-control id.
    /// </summary>
    public bool RequireClinicalRuleControlLinks { get; init; }

    /// <summary>
    /// Requires the plan-check catalog to declare an owner.
    /// </summary>
    public bool RequirePlanCheckOwner { get; init; }

    /// <summary>
    /// Requires every active plan check to cite its source policy or protocol.
    /// </summary>
    public bool RequirePlanCheckReference { get; init; }

    /// <summary>
    /// Requires every active plan check to link to a stable requirement id.
    /// </summary>
    public bool RequirePlanCheckRequirementId { get; init; }

    /// <summary>
    /// Requires every active plan check to link to at least one hazard id.
    /// </summary>
    public bool RequirePlanCheckHazardLinks { get; init; }

    /// <summary>
    /// Requires every active plan check to link to at least one safety-control id.
    /// </summary>
    public bool RequirePlanCheckControlLinks { get; init; }

    /// <summary>
    /// Requires a structure-name dictionary.
    /// </summary>
    public bool RequireNamingDictionary { get; init; }

    /// <summary>
    /// Requires a machine profile.
    /// </summary>
    public bool RequireMachineProfile { get; init; }

    /// <summary>
    /// Requires the naming dictionary to list required structures.
    /// </summary>
    public bool RequireRequiredStructures { get; init; }
}
