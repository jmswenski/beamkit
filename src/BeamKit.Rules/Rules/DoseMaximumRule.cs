using BeamKit.Core.Domain;
using BeamKit.Rules;

namespace BeamKit.Rules.Rules;

/// <summary>
/// Checks that a structure's maximum dose is less than or equal to a threshold.
/// </summary>
public sealed class DoseMaximumRule : DoseMetricThresholdRule
{
    /// <summary>
    /// Creates a maximum-dose rule for a structure.
    /// </summary>
    public DoseMaximumRule(
        string structureName,
        decimal maximumDoseGy,
        EvaluationStatus failureStatus = EvaluationStatus.Fail)
        : base(
            $"dose.max.{RuleText.Slug(structureName)}",
            $"{structureName} max dose <= {RuleText.FormatNumber(maximumDoseGy)} Gy",
            structureName,
            DoseMetricKeys.MaximumDoseGy,
            GoalComparison.LessThanOrEqual,
            maximumDoseGy,
            "Gy",
            failureStatus)
    {
    }
}
