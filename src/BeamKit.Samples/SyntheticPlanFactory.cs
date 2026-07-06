using BeamKit.Core.Domain;
using BeamKit.Metrics;

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
            intent: "Definitive",
            requestedEnergy: "6X",
            requestedTechniqueId: "VMAT");

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
                        [DoseMetricKeys.DoseAtVolumePercent(95m)] = 67.4m,
                        [DoseMetricKeys.DoseAtVolumePercent(98m)] = 66.2m,
                        [DoseMetricKeys.DoseAtVolumePercent(2m)] = 72.78m,
                        [DoseMetricKeys.VolumeAtDoseGy(66.5m)] = 97.8m,
                        [DoseMetricKeys.VolumeAtDoseGy(70m)] = 91.2m,
                        [PlanMetricKeys.VolumeAtPrescriptionPercent(95m)] = 97.8m,
                        [PlanMetricKeys.VolumeAtPrescriptionPercent(100m)] = 91.2m,
                        [PlanMetricKeys.VolumeAtPrescriptionPercentCc(100m)] = 149.75m
                    }),
                new DoseStatistics(
                    "BODY",
                    new Dictionary<string, decimal>
                    {
                        [PlanMetricKeys.VolumeAtPrescriptionPercentCc(100m)] = 165.6m,
                        [PlanMetricKeys.VolumeAtPrescriptionPercentCc(50m)] = 453.9m
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
            },
            calculationModel: "SyntheticAAA",
            calculationModelVersion: "16.1");

        var beams = new[]
        {
            new Beam(
                "B1",
                "Arc 1",
                "Photon VMAT",
                "6X",
                monitorUnits: 431.2m,
                treatmentUnitId: "SYN-LINAC",
                techniqueId: "VMAT",
                controlPoints: CreateSyntheticArcControlPoints(0m, 180m),
                beamModelId: "SYN-AAA-6X",
                jawTrackingEnabled: true),
            new Beam(
                "B2",
                "Arc 2",
                "Photon VMAT",
                "6X",
                monitorUnits: 417.9m,
                treatmentUnitId: "SYN-LINAC",
                techniqueId: "VMAT",
                controlPoints: CreateSyntheticArcControlPoints(180m, 0m),
                beamModelId: "SYN-AAA-6X",
                jawTrackingEnabled: true)
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

    private static IReadOnlyList<BeamControlPoint> CreateSyntheticArcControlPoints(decimal startGantryDegrees, decimal stopGantryDegrees)
    {
        return new[]
        {
            new BeamControlPoint(0, startGantryDegrees, 0m, new BeamJawPositions(-5m, 5m, -6m, 6m)),
            new BeamControlPoint(1, MidAngle(startGantryDegrees, stopGantryDegrees), 0.5m, new BeamJawPositions(-5m, 5m, -6m, 6m)),
            new BeamControlPoint(2, stopGantryDegrees, 1m, new BeamJawPositions(-5m, 5m, -6m, 6m))
        };
    }

    private static decimal MidAngle(decimal startGantryDegrees, decimal stopGantryDegrees)
    {
        return startGantryDegrees <= stopGantryDegrees
            ? (startGantryDegrees + stopGantryDegrees) / 2m
            : ((startGantryDegrees + stopGantryDegrees + 360m) / 2m) % 360m;
    }
}
