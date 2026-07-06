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
        string? version = null)
    {
        Name = TemplateText.Required(name, nameof(name));
        Goals = goals?.ToArray() ?? throw new ArgumentNullException(nameof(goals));
        DiseaseSite = string.IsNullOrWhiteSpace(diseaseSite) ? null : diseaseSite.Trim();
        Institution = string.IsNullOrWhiteSpace(institution) ? null : institution.Trim();
        Physician = string.IsNullOrWhiteSpace(physician) ? null : physician.Trim();
        Version = string.IsNullOrWhiteSpace(version) ? null : version.Trim();
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
    /// Goal templates in the set.
    /// </summary>
    public IReadOnlyList<ClinicalGoalTemplate> Goals { get; init; }

    /// <summary>
    /// Converts all templates to core clinical goals.
    /// </summary>
    public IReadOnlyList<ClinicalGoal> ToClinicalGoals()
    {
        return Goals.Select(goal => goal.ToClinicalGoal()).ToArray();
    }

    /// <summary>
    /// Converts all templates to executable rules.
    /// </summary>
    public PlanRuleSet ToRuleSet()
    {
        return new PlanRuleSet(Name, ToClinicalGoals().Select(ClinicalGoalRuleFactory.FromClinicalGoal));
    }
}
