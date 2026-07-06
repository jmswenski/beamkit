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
                    "Gy",
                    description: "PTV D95 coverage objective.",
                    reference: "Synthetic institutional head-and-neck baseline",
                    rationale: "Documents target coverage expectations in a machine-readable rule template.",
                    tags: new[] { "target", "coverage", "head-neck" }),
                new ClinicalGoalTemplate(
                    "goal.cord.max",
                    "SpinalCord",
                    DoseMetricKeys.MaximumDoseGy,
                    GoalComparison.LessThanOrEqual,
                    45m,
                    "Gy",
                    description: "Spinal cord maximum dose limit.",
                    reference: "Synthetic institutional head-and-neck baseline",
                    rationale: "Captures a required serial-organ constraint for automated QA.",
                    tags: new[] { "oar", "cord", "head-neck" }),
                new ClinicalGoalTemplate(
                    "goal.heart.mean",
                    "Heart",
                    DoseMetricKeys.MeanDoseGy,
                    GoalComparison.LessThanOrEqual,
                    10m,
                    "Gy",
                    GoalSeverity.Warning,
                    description: "Heart mean dose review goal.",
                    reference: "Synthetic institutional head-and-neck baseline",
                    rationale: "Shows how lower-priority review goals can be represented as warnings.",
                    tags: new[] { "oar", "heart" }),
                new ClinicalGoalTemplate(
                    "goal.lungr.v20",
                    "Lung_R",
                    DoseMetricKeys.VolumeAtDoseGy(20m),
                    GoalComparison.LessThanOrEqual,
                    30m,
                    "%",
                    description: "Right lung V20 review limit.",
                    reference: "Synthetic institutional head-and-neck baseline",
                    rationale: "Demonstrates dose-volume rule template metadata.",
                    tags: new[] { "oar", "lung" }),
                new ClinicalGoalTemplate(
                    "goal.lungl.v20",
                    "Lung_L",
                    DoseMetricKeys.VolumeAtDoseGy(20m),
                    GoalComparison.LessThanOrEqual,
                    30m,
                    "%",
                    description: "Left lung V20 review limit.",
                    reference: "Synthetic institutional head-and-neck baseline",
                    rationale: "Demonstrates dose-volume rule template metadata.",
                    tags: new[] { "oar", "lung" })
            },
            diseaseSite: "Head and Neck",
            institution: "Synthetic",
            version: "2026.1",
            description: "Synthetic disease-site baseline for head-and-neck plan QA.",
            owner: "Radiation Oncology",
            approvedBy: "Synthetic Physics",
            approvedOn: "2026-07-06",
            tags: new[] { "head-neck", "baseline" });
    }
}
