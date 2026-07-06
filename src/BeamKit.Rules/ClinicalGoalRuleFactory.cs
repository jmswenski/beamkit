using BeamKit.Core.Domain;

namespace BeamKit.Rules;

/// <summary>
/// Builds executable rules from clinical-goal definitions.
/// </summary>
public static class ClinicalGoalRuleFactory
{
    /// <summary>
    /// Converts a clinical goal into a dose-metric threshold rule.
    /// </summary>
    public static IPlanRule FromClinicalGoal(ClinicalGoal goal)
    {
        ArgumentNullException.ThrowIfNull(goal);

        var failureStatus = goal.Severity switch
        {
            GoalSeverity.Advisory => EvaluationStatus.Warning,
            GoalSeverity.Warning => EvaluationStatus.Warning,
            GoalSeverity.Required => EvaluationStatus.Fail,
            _ => EvaluationStatus.Fail
        };

        return new DoseMetricThresholdRule(
            goal.Id,
            $"{goal.StructureName} {goal.MetricKey} {RuleText.FormatComparison(goal.Comparison)} {RuleText.FormatNumber(goal.Threshold)} {goal.Unit}",
            goal.StructureName,
            goal.MetricKey,
            goal.Comparison,
            goal.Threshold,
            goal.Unit,
            failureStatus);
    }
}
