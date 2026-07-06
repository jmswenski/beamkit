using BeamKit.Core.Domain;

namespace BeamKit.Rules;

internal static class GoalComparisonEvaluator
{
    public static bool IsSatisfied(decimal observedValue, GoalComparison comparison, decimal threshold)
    {
        return comparison switch
        {
            GoalComparison.LessThan => observedValue < threshold,
            GoalComparison.LessThanOrEqual => observedValue <= threshold,
            GoalComparison.GreaterThan => observedValue > threshold,
            GoalComparison.GreaterThanOrEqual => observedValue >= threshold,
            GoalComparison.Equal => observedValue == threshold,
            _ => false
        };
    }
}
