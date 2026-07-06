using BeamKit.Core.Domain;
using BeamKit.Rules;

namespace BeamKit.Rules.Rules;

/// <summary>
/// Checks that a structure's mean dose is less than or equal to a threshold.
/// </summary>
public sealed class DoseMeanRule : DoseMetricThresholdRule
{
    /// <summary>
    /// Creates a mean-dose rule for a structure.
    /// </summary>
    public DoseMeanRule(
        string structureName,
        decimal maximumMeanDoseGy,
        EvaluationStatus failureStatus = EvaluationStatus.Fail)
        : base(
            $"dose.mean.{RuleText.Slug(structureName)}",
            $"{structureName} mean dose <= {RuleText.FormatNumber(maximumMeanDoseGy)} Gy",
            structureName,
            DoseMetricKeys.MeanDoseGy,
            GoalComparison.LessThanOrEqual,
            maximumMeanDoseGy,
            "Gy",
            failureStatus)
    {
    }
}
