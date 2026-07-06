using BeamKit.Core.Domain;
using BeamKit.Rules;

namespace BeamKit.Rules.Rules;

/// <summary>
/// Checks a DVH dose-at-volume metric such as D95%.
/// </summary>
public sealed class DvhDoseAtVolumeRule : DoseMetricThresholdRule
{
    /// <summary>
    /// Creates a DVH dose-at-volume rule.
    /// </summary>
    public DvhDoseAtVolumeRule(
        string structureName,
        decimal volumePercent,
        GoalComparison comparison,
        decimal thresholdDoseGy,
        EvaluationStatus failureStatus = EvaluationStatus.Fail)
        : base(
            $"dvh.d{RuleText.FormatToken(volumePercent)}.{RuleText.Slug(structureName)}",
            $"{structureName} D{RuleText.FormatNumber(volumePercent)}% {RuleText.FormatComparison(comparison)} {RuleText.FormatNumber(thresholdDoseGy)} Gy",
            structureName,
            DoseMetricKeys.DoseAtVolumePercent(volumePercent),
            comparison,
            thresholdDoseGy,
            "Gy",
            failureStatus)
    {
    }
}
