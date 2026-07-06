using BeamKit.Core.Domain;

namespace BeamKit.Rules;

/// <summary>
/// A named collection of rules evaluated together.
/// </summary>
public sealed record PlanRuleSet
{
    /// <summary>
    /// Creates a named rule set.
    /// </summary>
    public PlanRuleSet(string name, IEnumerable<IPlanRule> rules)
    {
        Name = RuleText.Required(name, nameof(name));
        Rules = rules?.ToArray() ?? throw new ArgumentNullException(nameof(rules));
    }

    /// <summary>
    /// Rule-set display name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Ordered rules in the set.
    /// </summary>
    public IReadOnlyList<IPlanRule> Rules { get; init; }

    /// <summary>
    /// Creates a rule set from the clinical goals embedded in a plan.
    /// </summary>
    public static PlanRuleSet FromClinicalGoals(Plan plan, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return new PlanRuleSet(
            name ?? $"{plan.Id} clinical goals",
            plan.ClinicalGoals.Select(ClinicalGoalRuleFactory.FromClinicalGoal));
    }
}
