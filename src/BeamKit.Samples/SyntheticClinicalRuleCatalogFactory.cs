using BeamKit.Core.Domain;
using BeamKit.Templates;

namespace BeamKit.Samples;

/// <summary>
/// Creates synthetic clinical rule catalogs for tests, demos, and documentation.
/// </summary>
public static class SyntheticClinicalRuleCatalogFactory
{
    /// <summary>
    /// Creates a synthetic head-and-neck rule catalog with baseline and physician-specific rules.
    /// </summary>
    public static ClinicalRuleCatalog CreateHeadAndNeckCatalog()
    {
        return new ClinicalRuleCatalog(
            "Synthetic clinical rule catalog",
            new[]
            {
                SyntheticClinicalGoalTemplateSetFactory.CreateHeadAndNeckBaseline(),
                new ClinicalGoalTemplateSet(
                    "Synthetic head and neck physician addendum",
                    new[]
                    {
                        new ClinicalGoalTemplate(
                            "goal.parotid.mean",
                            "Parotid_L",
                            DoseMetricKeys.MeanDoseGy,
                            GoalComparison.LessThanOrEqual,
                            26m,
                            "Gy",
                            GoalSeverity.Warning,
                            "Left parotid mean dose review goal.",
                            "Synthetic physician preference",
                            "Documents physician-specific planning preference without changing code.",
                            new[] { "head-neck", "parotid", "physician-addendum" })
                    },
                    diseaseSite: "Head and Neck",
                    institution: "Synthetic",
                    physician: "Synthetic Physician",
                    version: "2026.1",
                    description: "Synthetic physician-specific head-and-neck addendum.",
                    owner: "Radiation Oncology",
                    approvedBy: "Synthetic Physics",
                    approvedOn: "2026-07-06",
                    tags: new[] { "head-neck", "physician-addendum" })
            },
            institution: "Synthetic",
            version: "2026.1",
            description: "Synthetic catalog showing how changing clinical rules can be stored outside application code.",
            owner: "Radiation Oncology",
            tags: new[] { "synthetic", "head-neck" });
    }
}
