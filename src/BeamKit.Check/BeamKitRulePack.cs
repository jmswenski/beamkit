using BeamKit.Deliverability;
using BeamKit.Naming;
using BeamKit.PlanCheck;
using BeamKit.Rules;
using BeamKit.Templates;

namespace BeamKit.Check;

/// <summary>
/// Versioned clinical automation bundle used by the flagship BeamKit check workflow.
/// </summary>
public sealed record BeamKitRulePack
{
    /// <summary>
    /// Creates a rule pack from loaded vendor-neutral catalogs.
    /// </summary>
    public BeamKitRulePack(
        string name,
        string version,
        PlanRuleSet clinicalRuleSet,
        PlanCheckCatalog planCheckCatalog,
        StructureNameDictionary? namingDictionary = null,
        MachineConstraintProfile? machineProfile = null,
        RulePackReadinessDefaults? readinessDefaults = null,
        ClinicalRuleCatalogQuery? clinicalRuleQuery = null,
        string? owner = null,
        string? description = null,
        string? diseaseSite = null,
        IEnumerable<string>? tags = null)
    {
        Name = CheckText.Required(name, nameof(name));
        Version = CheckText.Required(version, nameof(version));
        ClinicalRuleSet = clinicalRuleSet ?? throw new ArgumentNullException(nameof(clinicalRuleSet));
        PlanCheckCatalog = planCheckCatalog ?? throw new ArgumentNullException(nameof(planCheckCatalog));
        NamingDictionary = namingDictionary;
        MachineProfile = machineProfile;
        ReadinessDefaults = readinessDefaults ?? new RulePackReadinessDefaults();
        ClinicalRuleQuery = clinicalRuleQuery?.Normalize() ?? new ClinicalRuleCatalogQuery();
        Owner = CheckText.Optional(owner);
        Description = CheckText.Optional(description);
        DiseaseSite = CheckText.Optional(diseaseSite);
        Tags = CheckText.CleanTags(tags);
    }

    /// <summary>
    /// Human-readable rule-pack name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Rule-pack version.
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Optional owner responsible for maintaining the rule pack.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Optional rule-pack description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional disease-site label.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Searchable tags associated with the rule pack.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; }

    /// <summary>
    /// Executable clinical goal rules selected from the clinical rule catalog.
    /// </summary>
    public PlanRuleSet ClinicalRuleSet { get; init; }

    /// <summary>
    /// Configurable plan checks for prescription, structures, dose, metrics, and deliverability.
    /// </summary>
    public PlanCheckCatalog PlanCheckCatalog { get; init; }

    /// <summary>
    /// Optional structure-name dictionary.
    /// </summary>
    public StructureNameDictionary? NamingDictionary { get; init; }

    /// <summary>
    /// Optional machine and delivery constraints.
    /// </summary>
    public MachineConstraintProfile? MachineProfile { get; init; }

    /// <summary>
    /// Default readiness evidence applied by command-line or service callers.
    /// </summary>
    public RulePackReadinessDefaults ReadinessDefaults { get; init; }

    /// <summary>
    /// Clinical rule catalog query used to select executable goals.
    /// </summary>
    public ClinicalRuleCatalogQuery ClinicalRuleQuery { get; init; }
}
