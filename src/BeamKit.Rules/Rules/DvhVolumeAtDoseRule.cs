using BeamKit.Core.Domain;
using BeamKit.Rules;

namespace BeamKit.Rules.Rules;

/// <summary>
/// Checks a DVH volume-at-dose metric such as V20 Gy.
/// </summary>
public sealed class DvhVolumeAtDoseRule : DoseMetricThresholdRule
{
    /// <summary>
    /// Creates a DVH volume-at-dose rule.
    /// </summary>
    public DvhVolumeAtDoseRule(
        string structureName,
        decimal doseGy,
        GoalComparison comparison,
        decimal thresholdVolumePercent,
        EvaluationStatus failureStatus = EvaluationStatus.Fail)
        : base(
            $"dvh.v{RuleText.FormatToken(doseGy)}.{RuleText.Slug(structureName)}",
            $"{structureName} V{RuleText.FormatNumber(doseGy)} Gy {RuleText.FormatComparison(comparison)} {RuleText.FormatNumber(thresholdVolumePercent)}%",
            structureName,
            DoseMetricKeys.VolumeAtDoseGy(doseGy),
            comparison,
            thresholdVolumePercent,
            "%",
            failureStatus)
    {
    }
}
