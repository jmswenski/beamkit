using BeamKit.Core.Domain;

namespace BeamKit.Rules;

/// <summary>
/// Evaluates rule sets against plans.
/// </summary>
public sealed class RuleEngine
{
    /// <summary>
    /// Evaluates each rule in order and isolates unexpected rule exceptions as error results.
    /// </summary>
    public IReadOnlyList<EvaluationResult> Evaluate(Plan plan, PlanRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(ruleSet);

        var context = new PlanEvaluationContext(plan);
        var results = new List<EvaluationResult>(ruleSet.Rules.Count);

        foreach (var rule in ruleSet.Rules)
        {
            try
            {
                results.Add(rule.Evaluate(context));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                results.Add(EvaluationResult.Error(
                    rule.Id,
                    rule.Description,
                    $"Rule threw {ex.GetType().Name}: {ex.Message}"));
            }
        }

        return results;
    }
}
