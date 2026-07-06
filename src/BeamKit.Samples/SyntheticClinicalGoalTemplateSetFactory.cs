using BeamKit.Core.Domain;
using BeamKit.Templates;

namespace BeamKit.Samples;

/// <summary>
/// Creates synthetic clinical goal templates for tests, demos, and documentation.
/// </summary>
public static class SyntheticClinicalGoalTemplateSetFactory
{
    /// <summary>
    /// Creates a baseline synthetic head-and-neck clinical goal template set.
    /// </summary>
    public static ClinicalGoalTemplateSet CreateHeadAndNeckBaseline()
    {
        return new ClinicalGoalTemplateSet(
            "Synthetic head and neck baseline",
            new[]
            {
                new ClinicalGoalTemplate(
                    "goal.ptv.d95",
                    "PTV_7000",
                    DoseMetricKeys.DoseAtVolumePercent(95m),
                    GoalComparison.GreaterThanOrEqual,
                    66.5m,
                    "Gy"),
                new ClinicalGoalTemplate(
                    "goal.cord.max",
                    "SpinalCord",
                    DoseMetricKeys.MaximumDoseGy,
                    GoalComparison.LessThanOrEqual,
                    45m,
                    "Gy"),
                new ClinicalGoalTemplate(
                    "goal.heart.mean",
                    "Heart",
                    DoseMetricKeys.MeanDoseGy,
                    GoalComparison.LessThanOrEqual,
                    10m,
                    "Gy",
                    GoalSeverity.Warning),
                new ClinicalGoalTemplate(
                    "goal.lungr.v20",
                    "Lung_R",
                    DoseMetricKeys.VolumeAtDoseGy(20m),
                    GoalComparison.LessThanOrEqual,
                    30m,
                    "%"),
                new ClinicalGoalTemplate(
                    "goal.lungl.v20",
                    "Lung_L",
                    DoseMetricKeys.VolumeAtDoseGy(20m),
                    GoalComparison.LessThanOrEqual,
                    30m,
                    "%")
            },
            diseaseSite: "Head and Neck",
            institution: "Synthetic",
            version: "2026.1");
    }
}
