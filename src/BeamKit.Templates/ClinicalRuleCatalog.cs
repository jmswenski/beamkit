using BeamKit.Core.Domain;
using BeamKit.Rules;

namespace BeamKit.Templates;

/// <summary>
/// Versioned library of clinical rule template sets.
/// </summary>
public sealed record ClinicalRuleCatalog
{
    /// <summary>
    /// Creates a clinical rule catalog.
    /// </summary>
    public ClinicalRuleCatalog(
        string name,
        IEnumerable<ClinicalGoalTemplateSet> templateSets,
        string? institution = null,
        string? version = null,
        string? description = null,
        string? owner = null,
        IEnumerable<string>? tags = null)
    {
        Name = TemplateText.Required(name, nameof(name));
        TemplateSets = templateSets?.ToArray() ?? throw new ArgumentNullException(nameof(templateSets));
        Institution = TemplateText.Optional(institution);
        Version = TemplateText.Optional(version);
        Description = TemplateText.Optional(description);
        Owner = TemplateText.Optional(owner);
        Tags = TemplateText.CleanTags(tags);

        if (TemplateSets.Count == 0)
        {
            throw new ArgumentException("At least one template set is required.", nameof(templateSets));
        }
    }

    /// <summary>
    /// Catalog name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Optional institution label.
    /// </summary>
    public string? Institution { get; init; }

    /// <summary>
    /// Optional catalog version.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Human-readable catalog summary.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Owning group responsible for maintaining the catalog.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Searchable catalog tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; }

    /// <summary>
    /// Template sets in the catalog.
    /// </summary>
    public IReadOnlyList<ClinicalGoalTemplateSet> TemplateSets { get; init; }

    /// <summary>
    /// Finds rule template sets that match the supplied query. Blank set fields act as catalog-level rules.
    /// </summary>
    public IReadOnlyList<ClinicalGoalTemplateSet> FindTemplateSets(ClinicalRuleCatalogQuery? query = null)
    {
        var normalized = (query ?? new ClinicalRuleCatalogQuery()).Normalize();
        var hasScopeFilter = normalized.DiseaseSite is not null
            || normalized.Institution is not null
            || normalized.Physician is not null;
        if (!hasScopeFilter && normalized.Tags.Count == 0)
        {
            return TemplateSets.ToArray();
        }

        return TemplateSets
            .Where(set => Matches(normalized.DiseaseSite, set.DiseaseSite))
            .Where(set => Matches(normalized.Institution, set.Institution ?? Institution))
            .Where(set => !hasScopeFilter || MatchesPhysician(normalized.Physician, set.Physician))
            .Where(set => ContainsAllTags(set, normalized.Tags))
            .ToArray();
    }

    /// <summary>
    /// Creates one executable rule set from all active goals in matching template sets.
    /// </summary>
    public PlanRuleSet ToRuleSet(ClinicalRuleCatalogQuery? query = null)
    {
        var selectedSets = FindTemplateSets(query);
        if (selectedSets.Count == 0)
        {
            throw new InvalidOperationException("Clinical rule catalog query did not match any template sets.");
        }

        var goals = selectedSets.SelectMany(set => set.ActiveGoals).ToArray();
        var duplicateGoalId = goals
            .GroupBy(goal => goal.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateGoalId is not null)
        {
            throw new InvalidOperationException($"Duplicate active clinical goal id '{duplicateGoalId}' in selected catalog rule sets.");
        }

        return new PlanRuleSet($"{Name} selected rules", goals.Select(ToTraceableRule));
    }

    private static IPlanRule ToTraceableRule(ClinicalGoalTemplate goal)
    {
        var clinicalGoal = goal.ToClinicalGoal();
        var failureStatus = clinicalGoal.Severity switch
        {
            Core.Domain.GoalSeverity.Advisory => EvaluationStatus.Warning,
            Core.Domain.GoalSeverity.Warning => EvaluationStatus.Warning,
            Core.Domain.GoalSeverity.Required => EvaluationStatus.Fail,
            _ => EvaluationStatus.Fail
        };

        return new DoseMetricThresholdRule(
            clinicalGoal.Id,
            goal.Description
                ?? $"{clinicalGoal.StructureName} {clinicalGoal.MetricKey} {FormatComparison(clinicalGoal.Comparison)} {FormatNumber(clinicalGoal.Threshold)} {clinicalGoal.Unit}",
            clinicalGoal.StructureName,
            clinicalGoal.MetricKey,
            clinicalGoal.Comparison,
            clinicalGoal.Threshold,
            clinicalGoal.Unit,
            failureStatus,
            goal.Reference,
            goal.Rationale,
            goal.RequirementId,
            goal.HazardIds,
            goal.ControlIds);
    }

    private static string FormatComparison(GoalComparison comparison)
    {
        return comparison switch
        {
            GoalComparison.LessThan => "<",
            GoalComparison.LessThanOrEqual => "<=",
            GoalComparison.GreaterThan => ">",
            GoalComparison.GreaterThanOrEqual => ">=",
            GoalComparison.Equal => "=",
            _ => comparison.ToString()
        };
    }

    private static string FormatNumber(decimal value)
    {
        return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static bool Matches(string? requested, string? candidate)
    {
        return requested is null
            || candidate is null
            || string.Equals(requested, candidate, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesPhysician(string? requested, string? candidate)
    {
        return requested is null
            ? candidate is null
            : candidate is null || string.Equals(requested, candidate, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsAllTags(ClinicalGoalTemplateSet set, IReadOnlyList<string> requestedTags)
    {
        return requestedTags.Count == 0
            || requestedTags.All(tag => set.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
    }
}
