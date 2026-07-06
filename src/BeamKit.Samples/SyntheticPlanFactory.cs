using BeamKit.Core.Domain;

namespace BeamKit.Samples;

/// <summary>
/// Creates synthetic plans for tests, demos, and documentation.
/// </summary>
public static class SyntheticPlanFactory
{
    /// <summary>
    /// Creates a synthetic head-and-neck plan with no patient data.
    /// </summary>
    public static Plan CreateHeadAndNeckPlan()
    {
        var patient = new Patient("SYN-0001", "Synthetic Patient");
        var prescription = new Prescription(
            totalDoseGy: 70m,
            fractionCount: 35,
            targetStructureId: "PTV_7000",
            isSigned: true,
            intent: "Definitive");

        var structures = new[]
        {
            new Structure("BODY", "Body", StructureType.External, 31_500m),
            new Structure("PTV_7000", "PTV_7000", StructureType.Target, 164.2m),
            new Structure("CORD", "SpinalCord", StructureType.OrganAtRisk, 42.1m),
            new Structure("HEART", "Heart", StructureType.OrganAtRisk, 611.4m),
            new Structure("LUNG_R", "Lung_R", StructureType.OrganAtRisk, 1_820.5m),
            new Structure("LUNG_L", "Lung_L", StructureType.OrganAtRisk, 1_655.3m)
        };

        var dose = new Dose(
            "DOSE-001",
            new DoseGrid(2.5m, 2.5m, 2.5m),
            new[]
            {
                new DoseStatistics(
                    "PTV_7000",
                    new Dictionary<string, decimal>
                    {
                        [DoseMetricKeys.MaximumDoseGy] = 74.1m,
                        [DoseMetricKeys.MeanDoseGy] = 70.2m,
                        [DoseMetricKeys.DoseAtVolumePercent(95m)] = 67.4m
                    }),
                new DoseStatistics(
                    "CORD",
                    new Dictionary<string, decimal>
                    {
                        [DoseMetricKeys.MaximumDoseGy] = 42.3m,
                        [DoseMetricKeys.MeanDoseGy] = 18.2m
                    }),
                new DoseStatistics(
                    "HEART",
                    new Dictionary<string, decimal>
                    {
                        [DoseMetricKeys.MaximumDoseGy] = 19.8m,
                        [DoseMetricKeys.MeanDoseGy] = 8.4m
                    }),
                new DoseStatistics(
                    "LUNG_R",
                    new Dictionary<string, decimal>
                    {
                        [DoseMetricKeys.MeanDoseGy] = 9.1m,
                        [DoseMetricKeys.VolumeAtDoseGy(20m)] = 18.6m
                    }),
                new DoseStatistics(
                    "LUNG_L",
                    new Dictionary<string, decimal>
                    {
                        [DoseMetricKeys.MeanDoseGy] = 8.7m,
                        [DoseMetricKeys.VolumeAtDoseGy(20m)] = 17.2m
                    })
            });

        var beams = new[]
        {
            new Beam("B1", "Arc 1", "Photon VMAT", "6X", monitorUnits: 431.2m),
            new Beam("B2", "Arc 2", "Photon VMAT", "6X", monitorUnits: 417.9m)
        };

        var clinicalGoals = new[]
        {
            new ClinicalGoal(
                "goal.ptv.d95",
                "PTV_7000",
                DoseMetricKeys.DoseAtVolumePercent(95m),
                GoalComparison.GreaterThanOrEqual,
                66.5m,
                "Gy"),
            new ClinicalGoal(
                "goal.cord.max",
                "SpinalCord",
                DoseMetricKeys.MaximumDoseGy,
                GoalComparison.LessThanOrEqual,
                45m,
                "Gy"),
            new ClinicalGoal(
                "goal.heart.mean",
                "Heart",
                DoseMetricKeys.MeanDoseGy,
                GoalComparison.LessThanOrEqual,
                10m,
                "Gy")
        };

        return new Plan(
            "HN-SYN-001",
            patient,
            "C1",
            prescription,
            structures,
            dose,
            beams,
            clinicalGoals,
            "Head and Neck");
    }
}
