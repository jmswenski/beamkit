using BeamKit.Core.Domain;
using BeamKit.Rules;

namespace BeamKit.Templates;

/// <summary>
/// Named collection of clinical goal templates for a disease site, institution, or physician.
/// </summary>
public sealed record ClinicalGoalTemplateSet
{
    /// <summary>
    /// Creates a clinical goal template set.
    /// </summary>
    public ClinicalGoalTemplateSet(
        string name,
        IEnumerable<ClinicalGoalTemplate> goals,
        string? diseaseSite = null,
        string? institution = null,
        string? physician = null,
        string? version = null,
        string? description = null,
        string? owner = null,
        string? approvedBy = null,
        string? approvedOn = null,
        IEnumerable<string>? tags = null)
    {
        Name = TemplateText.Required(name, nameof(name));
        Goals = goals?.ToArray() ?? throw new ArgumentNullException(nameof(goals));
        DiseaseSite = TemplateText.Optional(diseaseSite);
        Institution = TemplateText.Optional(institution);
        Physician = TemplateText.Optional(physician);
        Version = TemplateText.Optional(version);
        Description = TemplateText.Optional(description);
        Owner = TemplateText.Optional(owner);
        ApprovedBy = TemplateText.Optional(approvedBy);
        ApprovedOn = TemplateText.Optional(approvedOn);
        Tags = TemplateText.CleanTags(tags);
    }

    /// <summary>
    /// Template-set name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Optional disease-site label.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Optional institution label.
    /// </summary>
    public string? Institution { get; init; }

    /// <summary>
    /// Optional physician label.
    /// </summary>
    public string? Physician { get; init; }

    /// <summary>
    /// Optional template version.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Human-readable summary of the rule set.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Owning group responsible for maintaining this rule set.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Person or group that approved the current rule-set version.
    /// </summary>
    public string? ApprovedBy { get; init; }

    /// <summary>
    /// Free-form approval or review date, typically ISO 8601.
    /// </summary>
    public string? ApprovedOn { get; init; }

    /// <summary>
    /// Searchable tags used for catalog filtering.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; }

    /// <summary>
    /// Goal templates in the set.
    /// </summary>
    public IReadOnlyList<ClinicalGoalTemplate> Goals { get; init; }

    /// <summary>
    /// Active goal templates in the set.
    /// </summary>
    public IReadOnlyList<ClinicalGoalTemplate> ActiveGoals => Goals.Where(goal => goal.IsActive).ToArray();

    /// <summary>
    /// Converts active templates to core clinical goals.
    /// </summary>
    public IReadOnlyList<ClinicalGoal> ToClinicalGoals()
    {
        return ActiveGoals.Select(goal => goal.ToClinicalGoal()).ToArray();
    }

    /// <summary>
    /// Converts all templates to executable rules.
    /// </summary>
    public PlanRuleSet ToRuleSet()
    {
        return new PlanRuleSet(Name, ToClinicalGoals().Select(ClinicalGoalRuleFactory.FromClinicalGoal));
    }
}
