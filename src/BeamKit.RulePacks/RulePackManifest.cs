using System.Text.Json.Serialization;
using BeamKit.Check;
using BeamKit.Templates;

namespace BeamKit.RulePacks;

/// <summary>
/// JSON manifest that composes the lower-level files used by a BeamKit rule pack.
/// </summary>
public sealed record RulePackManifest
{
    /// <summary>
    /// Creates an empty manifest for JSON deserialization.
    /// </summary>
    public RulePackManifest()
    {
        Name = string.Empty;
        Version = string.Empty;
        ClinicalRuleCatalog = string.Empty;
        PlanCheckCatalog = string.Empty;
        Tags = Array.Empty<string>();
    }

    /// <summary>
    /// Creates a rule-pack manifest.
    /// </summary>
    public RulePackManifest(
        string name,
        string version,
        string clinicalRuleCatalog,
        string planCheckCatalog,
        string? owner = null,
        string? description = null,
        string? diseaseSite = null,
        IEnumerable<string>? tags = null,
        string? namingDictionary = null,
        string? machineProfile = null,
        ClinicalRuleCatalogQuery? clinicalRuleQuery = null,
        RulePackReadinessDefaults? readinessDefaults = null,
        RulePackApprovalMetadata? approval = null,
        string? schema = "../../../schemas/beamkit-rule-pack.schema.json")
    {
        Schema = RulePackText.Optional(schema);
        Name = RulePackText.Required(name, nameof(name));
        Version = RulePackText.Required(version, nameof(version));
        Owner = RulePackText.Optional(owner);
        Description = RulePackText.Optional(description);
        DiseaseSite = RulePackText.Optional(diseaseSite);
        Tags = RulePackText.CleanTags(tags);
        ClinicalRuleCatalog = RulePackText.Required(clinicalRuleCatalog, nameof(clinicalRuleCatalog));
        PlanCheckCatalog = RulePackText.Required(planCheckCatalog, nameof(planCheckCatalog));
        NamingDictionary = RulePackText.Optional(namingDictionary);
        MachineProfile = RulePackText.Optional(machineProfile);
        ClinicalRuleQuery = clinicalRuleQuery?.Normalize();
        ReadinessDefaults = readinessDefaults;
        Approval = approval;
    }

    /// <summary>
    /// Optional JSON schema URI.
    /// </summary>
    [JsonPropertyName("$schema")]
    public string? Schema { get; init; }

    /// <summary>
    /// Human-readable rule-pack name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Rule-pack authoring version.
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Owner responsible for maintaining the rule pack.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Disease-site label.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Searchable tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; }

    /// <summary>
    /// Path to the clinical rule catalog, relative to the manifest file.
    /// </summary>
    public string ClinicalRuleCatalog { get; init; }

    /// <summary>
    /// Path to the plan-check catalog, relative to the manifest file.
    /// </summary>
    public string PlanCheckCatalog { get; init; }

    /// <summary>
    /// Optional path to the naming dictionary, relative to the manifest file.
    /// </summary>
    public string? NamingDictionary { get; init; }

    /// <summary>
    /// Optional path to the machine profile, relative to the manifest file.
    /// </summary>
    public string? MachineProfile { get; init; }

    /// <summary>
    /// Query used to select executable clinical goals from the clinical rule catalog.
    /// </summary>
    public ClinicalRuleCatalogQuery? ClinicalRuleQuery { get; init; }

    /// <summary>
    /// Readiness defaults applied by check workflows when explicit evidence is not supplied.
    /// </summary>
    public RulePackReadinessDefaults? ReadinessDefaults { get; init; }

    /// <summary>
    /// Review and approval metadata for rule-pack governance.
    /// </summary>
    public RulePackApprovalMetadata? Approval { get; init; }

    /// <summary>
    /// Returns a normalized copy.
    /// </summary>
    public RulePackManifest Normalize()
    {
        return this with
        {
            Schema = RulePackText.Optional(Schema),
            Name = RulePackText.Required(Name, nameof(Name)),
            Version = RulePackText.Required(Version, nameof(Version)),
            Owner = RulePackText.Optional(Owner),
            Description = RulePackText.Optional(Description),
            DiseaseSite = RulePackText.Optional(DiseaseSite),
            Tags = RulePackText.CleanTags(Tags),
            ClinicalRuleCatalog = RulePackText.Required(ClinicalRuleCatalog, nameof(ClinicalRuleCatalog)),
            PlanCheckCatalog = RulePackText.Required(PlanCheckCatalog, nameof(PlanCheckCatalog)),
            NamingDictionary = RulePackText.Optional(NamingDictionary),
            MachineProfile = RulePackText.Optional(MachineProfile),
            ClinicalRuleQuery = ClinicalRuleQuery?.Normalize(),
            Approval = Approval is null
                ? null
                : new RulePackApprovalMetadata(
                    Approval.Status,
                    Approval.Institution,
                    Approval.PhysicianGroup,
                    Approval.ReviewedBy,
                    Approval.ApprovedBy,
                    Approval.EffectiveDate,
                    Approval.ReviewDueDate,
                    Approval.Reference,
                    Approval.Rationale,
                    Approval.ChangeTicket)
        };
    }
}
