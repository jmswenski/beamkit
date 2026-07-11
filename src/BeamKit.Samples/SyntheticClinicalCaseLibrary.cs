using BeamKit.Core.Domain;
using BeamKit.Metrics;

namespace BeamKit.Samples;

/// <summary>
/// Provides synthetic, PHI-free clinical cases for examples and regression tests.
/// </summary>
public static class SyntheticClinicalCaseLibrary
{
    /// <summary>
    /// Returns all built-in synthetic cases.
    /// </summary>
    public static IReadOnlyList<SyntheticClinicalCase> All()
    {
        return new[]
        {
            HeadAndNeckBaseline(),
            HeadAndNeckCordFailure(),
            HeadAndNeckMissingStructure(),
            LungSbrtBaseline(),
            ProstateBaseline(),
            BrainSrsBaseline()
        };
    }

    /// <summary>
    /// Finds a built-in synthetic case by id.
    /// </summary>
    public static SyntheticClinicalCase Find(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Case id is required.", nameof(id));
        }

        return All().FirstOrDefault(clinicalCase => string.Equals(clinicalCase.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Synthetic case '{id}' was not found.");
    }

    /// <summary>
    /// Creates the passing synthetic head-and-neck baseline.
    /// </summary>
    public static SyntheticClinicalCase HeadAndNeckBaseline()
    {
        return new SyntheticClinicalCase(
            "head-neck-pass",
            "Head and neck baseline",
            "Head and Neck",
            "Passing synthetic VMAT head-and-neck plan with structure, dose, metric, and deliverability metadata.",
            SyntheticPlanFactory.CreateHeadAndNeckPlan(),
            expectedToPass: true);
    }

    /// <summary>
    /// Creates a head-and-neck case with a spinal-cord maximum dose failure.
    /// </summary>
    public static SyntheticClinicalCase HeadAndNeckCordFailure()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var statistics = plan.Dose!.Statistics
            .Select(stat => string.Equals(stat.StructureId, "CORD", StringComparison.OrdinalIgnoreCase)
                ? new DoseStatistics(stat.StructureId, WithMetric(stat.Metrics, DoseMetricKeys.MaximumDoseGy, 49.2m))
                : stat)
            .ToArray();

        return new SyntheticClinicalCase(
            "head-neck-cord-fail",
            "Head and neck cord failure",
            "Head and Neck",
            "Synthetic VMAT head-and-neck plan where spinal cord maximum dose exceeds the baseline limit.",
            plan with { Dose = plan.Dose with { Statistics = statistics } },
            expectedToPass: false,
            expectedFindings: new[] { "Spinal cord maximum dose exceeds 45 Gy." });
    }

    /// <summary>
    /// Creates a head-and-neck case with a missing required left lung.
    /// </summary>
    public static SyntheticClinicalCase HeadAndNeckMissingStructure()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        return new SyntheticClinicalCase(
            "head-neck-missing-structure",
            "Head and neck missing structure",
            "Head and Neck",
            "Synthetic VMAT head-and-neck plan missing a required left lung structure.",
            plan with
            {
                Structures = plan.Structures.Where(structure => !string.Equals(structure.Id, "LUNG_L", StringComparison.OrdinalIgnoreCase)).ToArray()
            },
            expectedToPass: false,
            expectedFindings: new[] { "Lung_L is missing from the structure set." });
    }

    /// <summary>
    /// Creates a compact synthetic lung SBRT case.
    /// </summary>
    public static SyntheticClinicalCase LungSbrtBaseline()
    {
        return new SyntheticClinicalCase(
            "lung-sbrt-pass",
            "Lung SBRT baseline",
            "Lung",
            "Synthetic lung SBRT plan for future disease-site rule packs.",
            CreateCompactPlan("LUNG-SBRT-SYN-001", "Lung", 50m, 5, "PTV_5000", 42m),
            expectedToPass: true);
    }

    /// <summary>
    /// Creates a compact synthetic prostate case.
    /// </summary>
    public static SyntheticClinicalCase ProstateBaseline()
    {
        return new SyntheticClinicalCase(
            "prostate-pass",
            "Prostate baseline",
            "Prostate",
            "Synthetic prostate VMAT plan for future disease-site rule packs.",
            CreateCompactPlan("PROSTATE-SYN-001", "Prostate", 70m, 28, "PTV_7000", 96m),
            expectedToPass: true);
    }

    /// <summary>
    /// Creates a compact synthetic brain SRS case.
    /// </summary>
    public static SyntheticClinicalCase BrainSrsBaseline()
    {
        return new SyntheticClinicalCase(
            "brain-srs-pass",
            "Brain SRS baseline",
            "Brain",
            "Synthetic brain SRS plan for future disease-site rule packs.",
            CreateCompactPlan("BRAIN-SRS-SYN-001", "Brain", 21m, 1, "PTV_2100", 9m),
            expectedToPass: true);
    }

    private static Plan CreateCompactPlan(string planId, string diseaseSite, decimal totalDoseGy, int fractions, string targetId, decimal targetVolumeCc)
    {
        var patient = new Patient($"SYN-{diseaseSite.ToUpperInvariant()}-001", "Synthetic Patient");
        var prescription = new Prescription(totalDoseGy, fractions, targetId, isSigned: true, intent: "Definitive", requestedEnergy: "6X", requestedTechniqueId: "VMAT");
        var structures = new[]
        {
            new Structure("BODY", "Body", StructureType.External, 18_000m),
            new Structure(targetId, targetId, StructureType.Target, targetVolumeCc),
            new Structure("OAR_1", "SyntheticOar", StructureType.OrganAtRisk, 110m)
        };
        var dose = new Dose(
            $"{planId}.Dose",
            new DoseGrid(2.5m, 2.5m, 2.5m),
            new[]
            {
                new DoseStatistics(
                    targetId,
                    new Dictionary<string, decimal>
                    {
                        [DoseMetricKeys.MaximumDoseGy] = totalDoseGy * 1.06m,
                        [DoseMetricKeys.MeanDoseGy] = totalDoseGy,
                        [DoseMetricKeys.DoseAtVolumePercent(95m)] = totalDoseGy * 0.96m,
                        [DoseMetricKeys.DoseAtVolumePercent(98m)] = totalDoseGy * 0.94m,
                        [DoseMetricKeys.DoseAtVolumePercent(2m)] = totalDoseGy * 1.04m,
                        [PlanMetricKeys.VolumeAtPrescriptionPercent(95m)] = 96m,
                        [PlanMetricKeys.VolumeAtPrescriptionPercent(100m)] = 90m,
                        [PlanMetricKeys.VolumeAtPrescriptionPercentCc(100m)] = targetVolumeCc * 0.9m
                    }),
                new DoseStatistics(
                    "BODY",
                    new Dictionary<string, decimal>
                    {
                        [PlanMetricKeys.VolumeAtPrescriptionPercentCc(100m)] = targetVolumeCc * 1.25m,
                        [PlanMetricKeys.VolumeAtPrescriptionPercentCc(50m)] = targetVolumeCc * 3m
                    }),
                new DoseStatistics("OAR_1", new Dictionary<string, decimal> { [DoseMetricKeys.MaximumDoseGy] = totalDoseGy * 0.35m })
            },
            "SyntheticAAA",
            "16.1");
        var beams = new[]
        {
            new Beam(
                "B1",
                "Arc 1",
                "Photon VMAT",
                "6X",
                monitorUnits: 240m,
                treatmentUnitId: "SYN-LINAC",
                techniqueId: "VMAT",
                controlPoints: new[]
                {
                    new BeamControlPoint(0, 0m, 0m, new BeamJawPositions(-4m, 4m, -4m, 4m)),
                    new BeamControlPoint(1, 180m, 1m, new BeamJawPositions(-4m, 4m, -4m, 4m))
                },
                beamModelId: "SYN-AAA-6X",
                jawTrackingEnabled: true)
        };

        return new Plan(planId, patient, "C1", prescription, structures, dose, beams, diseaseSite: diseaseSite);
    }

    private static IReadOnlyDictionary<string, decimal> WithMetric(IReadOnlyDictionary<string, decimal> metrics, string key, decimal value)
    {
        var copy = metrics.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        copy[key] = value;
        return copy;
    }
}
