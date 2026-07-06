using System.Globalization;
using BeamKit.Core.Domain;
using BeamKit.Deliverability;
using BeamKit.Metrics;

namespace BeamKit.PlanCheck;

/// <summary>
/// Evaluates configurable plan-check catalogs against BeamKit plans.
/// </summary>
public sealed class PlanCheckEngine
{
    private readonly PlanQualityMetricService metricService = new();
    private readonly DeliverabilityCheckService deliverabilityService = new();

    /// <summary>
    /// Evaluates a plan-check request.
    /// </summary>
    public PlanCheckReport Evaluate(PlanCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var results = request.Catalog.Checks
            .Where(check => check.IsActive)
            .SelectMany(check => EvaluateCheck(request, check))
            .ToArray();
        return new PlanCheckReport(request.Plan.Id, request.Catalog.Name, request.Catalog.Version, results);
    }

    private IEnumerable<PlanCheckResult> EvaluateCheck(PlanCheckRequest request, PlanCheckDefinition check)
    {
        try
        {
            return check.Type.ToLowerInvariant() switch
            {
                "dose-exists" => new[] { CheckDoseExists(request.Plan, check) },
                "beams-present" => new[] { CheckBeamsPresent(request.Plan, check) },
                "structure-exists" => new[] { CheckStructureExists(request.Plan, check) },
                "structure-not-empty" => new[] { CheckStructureNotEmpty(request.Plan, check) },
                "dose-grid-max-spacing" => new[] { CheckDoseGridSpacing(request.Plan, check) },
                "prescription-energy" => new[] { CheckPrescriptionEnergy(request.Plan, check) },
                "prescription-technique" => new[] { CheckPrescriptionTechnique(request.Plan, check) },
                "prescription-fractionation" => new[] { CheckPrescriptionFractionation(request.Plan, check) },
                "calculation-model" => new[] { CheckCalculationModel(request, check) },
                "beam-model" => new[] { CheckBeamModel(request, check) },
                "dose-metric" => new[] { CheckDoseMetric(request.Plan, check) },
                "target-coverage" => new[] { CheckTargetCoverage(request.Plan, check) },
                "plan-quality-metric" => new[] { CheckPlanQualityMetric(request.Plan, check) },
                "deliverability" => CheckDeliverability(request, check),
                _ => new[] { Result(check, PlanCheckStatus.NotEvaluable, $"Unsupported check type '{check.Type}'.") }
            };
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException)
        {
            return new[] { Result(check, PlanCheckStatus.NotEvaluable, ex.Message) };
        }
    }

    private static PlanCheckResult CheckDoseExists(Plan plan, PlanCheckDefinition check)
    {
        return plan.Dose is null
            ? Result(check, PlanCheckStatus.NotEvaluable, "Plan has no dose.")
            : Result(check, PlanCheckStatus.Pass, "Plan has dose.", new Dictionary<string, string> { ["doseId"] = plan.Dose.Id });
    }

    private static PlanCheckResult CheckBeamsPresent(Plan plan, PlanCheckDefinition check)
    {
        var count = plan.Beams.Count(beam => !beam.IsSetupField);
        return count == 0
            ? Result(check, PlanCheckStatus.NotEvaluable, "Plan has no treatment beams.")
            : Result(check, PlanCheckStatus.Pass, "Plan has treatment beams.", new Dictionary<string, string> { ["beamCount"] = count.ToString(CultureInfo.InvariantCulture) });
    }

    private static PlanCheckResult CheckStructureExists(Plan plan, PlanCheckDefinition check)
    {
        var structureName = ResolveStructureName(plan, RequiredParameter(check, "structureName"));
        var structure = plan.FindStructure(structureName);
        return structure is null
            ? Result(check, StatusForFailed(check), $"Structure '{structureName}' was not found.")
            : Result(check, PlanCheckStatus.Pass, $"Structure '{structure.Name}' exists.", StructureEvidence(structure));
    }

    private static PlanCheckResult CheckStructureNotEmpty(Plan plan, PlanCheckDefinition check)
    {
        var structureName = ResolveStructureName(plan, RequiredParameter(check, "structureName"));
        var structure = plan.FindStructure(structureName);
        if (structure is null)
        {
            return Result(check, PlanCheckStatus.NotEvaluable, $"Structure '{structureName}' was not found.");
        }

        return structure.IsEmpty
            ? Result(check, StatusForFailed(check), $"Structure '{structure.Name}' is empty.", StructureEvidence(structure))
            : Result(check, PlanCheckStatus.Pass, $"Structure '{structure.Name}' has contours.", StructureEvidence(structure));
    }

    private static PlanCheckResult CheckDoseGridSpacing(Plan plan, PlanCheckDefinition check)
    {
        if (plan.Dose is null)
        {
            return Result(check, PlanCheckStatus.NotEvaluable, "Plan has no dose grid.");
        }

        var maxSpacingMm = DecimalParameter(check, "maxSpacingMm");
        var observed = plan.Dose.Grid.MaxSpacingMm;
        var passes = observed <= maxSpacingMm;
        return Result(
            check,
            passes ? PlanCheckStatus.Pass : StatusForFailed(check),
            passes ? "Dose grid spacing is within the configured limit." : "Dose grid spacing exceeds the configured limit.",
            new Dictionary<string, string>
            {
                ["observedMm"] = Format(observed),
                ["expectedMm"] = Format(maxSpacingMm)
            });
    }

    private static PlanCheckResult CheckPrescriptionEnergy(Plan plan, PlanCheckDefinition check)
    {
        var requestedEnergy = plan.Prescription.RequestedEnergy;
        if (string.IsNullOrWhiteSpace(requestedEnergy))
        {
            return Result(check, PlanCheckStatus.NotEvaluable, "Prescription requested energy is not available.");
        }

        var treatmentBeams = TreatmentBeams(plan);
        if (treatmentBeams.Length == 0)
        {
            return Result(check, PlanCheckStatus.NotEvaluable, "Plan has no treatment beams.");
        }

        var actualEnergies = treatmentBeams
            .Select(beam => beam.Energy)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var passes = actualEnergies.All(energy => string.Equals(energy, requestedEnergy, StringComparison.OrdinalIgnoreCase));
        return Result(
            check,
            passes ? PlanCheckStatus.Pass : StatusForFailed(check),
            passes ? "Beam energies match the prescription requested energy." : "One or more beam energies do not match the prescription requested energy.",
            new Dictionary<string, string>
            {
                ["requestedEnergy"] = requestedEnergy,
                ["actualEnergies"] = string.Join(", ", actualEnergies)
            });
    }

    private static PlanCheckResult CheckPrescriptionTechnique(Plan plan, PlanCheckDefinition check)
    {
        var requestedTechnique = plan.Prescription.RequestedTechniqueId;
        if (string.IsNullOrWhiteSpace(requestedTechnique))
        {
            return Result(check, PlanCheckStatus.NotEvaluable, "Prescription requested technique is not available.");
        }

        var treatmentBeams = TreatmentBeams(plan);
        if (treatmentBeams.Length == 0)
        {
            return Result(check, PlanCheckStatus.NotEvaluable, "Plan has no treatment beams.");
        }

        if (treatmentBeams.Any(beam => string.IsNullOrWhiteSpace(beam.TechniqueId)))
        {
            return Result(check, PlanCheckStatus.NotEvaluable, "One or more treatment beams are missing technique metadata.");
        }

        var actualTechniques = treatmentBeams
            .Select(beam => beam.TechniqueId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var passes = actualTechniques.All(technique => string.Equals(technique, requestedTechnique, StringComparison.OrdinalIgnoreCase));
        return Result(
            check,
            passes ? PlanCheckStatus.Pass : StatusForFailed(check),
            passes ? "Beam techniques match the prescription requested technique." : "One or more beam techniques do not match the prescription requested technique.",
            new Dictionary<string, string>
            {
                ["requestedTechnique"] = requestedTechnique,
                ["actualTechniques"] = string.Join(", ", actualTechniques)
            });
    }

    private static PlanCheckResult CheckPrescriptionFractionation(Plan plan, PlanCheckDefinition check)
    {
        var toleranceGy = OptionalDecimalParameter(check, "toleranceGy") ?? 0.01m;
        var expectedTotalDoseGy = OptionalDecimalParameter(check, "totalDoseGy");
        var expectedFractionCount = OptionalIntParameter(check, "fractionCount");
        var expectedDosePerFractionGy = OptionalDecimalParameter(check, "dosePerFractionGy");
        if (!expectedTotalDoseGy.HasValue && !expectedFractionCount.HasValue && !expectedDosePerFractionGy.HasValue)
        {
            return Result(check, PlanCheckStatus.NotEvaluable, "Prescription fractionation check requires at least one expected value.");
        }

        var failures = new List<string>();
        if (expectedTotalDoseGy.HasValue && Math.Abs(plan.Prescription.TotalDoseGy - expectedTotalDoseGy.Value) > toleranceGy)
        {
            failures.Add("total dose");
        }

        if (expectedFractionCount.HasValue && plan.Prescription.FractionCount != expectedFractionCount.Value)
        {
            failures.Add("fraction count");
        }

        if (expectedDosePerFractionGy.HasValue && Math.Abs(plan.Prescription.DosePerFractionGy - expectedDosePerFractionGy.Value) > toleranceGy)
        {
            failures.Add("dose per fraction");
        }

        return Result(
            check,
            failures.Count == 0 ? PlanCheckStatus.Pass : StatusForFailed(check),
            failures.Count == 0
                ? "Prescription fractionation matches configured expectations."
                : $"Prescription fractionation mismatch: {string.Join(", ", failures)}.",
            new Dictionary<string, string>
            {
                ["actualTotalDoseGy"] = Format(plan.Prescription.TotalDoseGy),
                ["actualFractionCount"] = plan.Prescription.FractionCount.ToString(CultureInfo.InvariantCulture),
                ["actualDosePerFractionGy"] = Format(plan.Prescription.DosePerFractionGy),
                ["expectedTotalDoseGy"] = expectedTotalDoseGy.HasValue ? Format(expectedTotalDoseGy.Value) : string.Empty,
                ["expectedFractionCount"] = expectedFractionCount.HasValue ? expectedFractionCount.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                ["expectedDosePerFractionGy"] = expectedDosePerFractionGy.HasValue ? Format(expectedDosePerFractionGy.Value) : string.Empty
            });
    }

    private static PlanCheckResult CheckCalculationModel(PlanCheckRequest request, PlanCheckDefinition check)
    {
        if (request.MachineProfile is null)
        {
            return Result(check, PlanCheckStatus.NotEvaluable, "Calculation model check requires a machine profile.");
        }

        if (request.MachineProfile.CalculationModel is null && request.MachineProfile.CalculationModelVersion is null)
        {
            return Result(check, PlanCheckStatus.NotEvaluable, "Machine profile does not specify calculation model expectations.");
        }

        if (request.Plan.Dose is null)
        {
            return Result(check, PlanCheckStatus.NotEvaluable, "Plan has no dose calculation metadata.");
        }

        var failures = new List<string>();
        if (request.MachineProfile.CalculationModel is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Plan.Dose.CalculationModel))
            {
                return Result(check, PlanCheckStatus.NotEvaluable, "Plan dose is missing calculation model metadata.");
            }

            if (!string.Equals(request.Plan.Dose.CalculationModel, request.MachineProfile.CalculationModel, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("model");
            }
        }

        if (request.MachineProfile.CalculationModelVersion is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Plan.Dose.CalculationModelVersion))
            {
                return Result(check, PlanCheckStatus.NotEvaluable, "Plan dose is missing calculation model version metadata.");
            }

            if (!string.Equals(request.Plan.Dose.CalculationModelVersion, request.MachineProfile.CalculationModelVersion, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("version");
            }
        }

        return Result(
            check,
            failures.Count == 0 ? PlanCheckStatus.Pass : StatusForFailed(check),
            failures.Count == 0 ? "Dose calculation model matches profile." : $"Dose calculation {string.Join(" and ", failures)} does not match profile.",
            new Dictionary<string, string>
            {
                ["actualModel"] = request.Plan.Dose.CalculationModel ?? string.Empty,
                ["expectedModel"] = request.MachineProfile.CalculationModel ?? string.Empty,
                ["actualVersion"] = request.Plan.Dose.CalculationModelVersion ?? string.Empty,
                ["expectedVersion"] = request.MachineProfile.CalculationModelVersion ?? string.Empty
            });
    }

    private static PlanCheckResult CheckBeamModel(PlanCheckRequest request, PlanCheckDefinition check)
    {
        if (request.MachineProfile is null)
        {
            return Result(check, PlanCheckStatus.NotEvaluable, "Beam model check requires a machine profile.");
        }

        var allowed = request.MachineProfile.AllowedBeamModelIds
            .Concat(string.IsNullOrWhiteSpace(request.MachineProfile.BeamModelId) ? Array.Empty<string>() : new[] { request.MachineProfile.BeamModelId })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (allowed.Length == 0)
        {
            return Result(check, PlanCheckStatus.NotEvaluable, "Machine profile does not specify beam model expectations.");
        }

        var treatmentBeams = TreatmentBeams(request.Plan);
        if (treatmentBeams.Length == 0)
        {
            return Result(check, PlanCheckStatus.NotEvaluable, "Plan has no treatment beams.");
        }

        if (treatmentBeams.Any(beam => string.IsNullOrWhiteSpace(beam.BeamModelId)))
        {
            return Result(check, PlanCheckStatus.NotEvaluable, "One or more treatment beams are missing beam model metadata.");
        }

        var actual = treatmentBeams
            .Select(beam => beam.BeamModelId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var passes = actual.All(beamModelId => allowed.Contains(beamModelId, StringComparer.OrdinalIgnoreCase));
        return Result(
            check,
            passes ? PlanCheckStatus.Pass : StatusForFailed(check),
            passes ? "Beam models are allowed by profile." : "One or more beam models are not allowed by profile.",
            new Dictionary<string, string>
            {
                ["actualBeamModels"] = string.Join(", ", actual),
                ["allowedBeamModels"] = string.Join(", ", allowed)
            });
    }

    private PlanCheckResult CheckDoseMetric(Plan plan, PlanCheckDefinition check)
    {
        var structureName = ResolveStructureName(plan, RequiredParameter(check, "structureName"));
        var metric = RequiredParameter(check, "metric");
        var comparison = Enum.Parse<GoalComparison>(RequiredParameter(check, "comparison"), ignoreCase: true);
        var threshold = DecimalParameter(check, "threshold");
        var result = metricService.Evaluate(plan, structureName, metric);
        if (!result.IsEvaluable || !result.Value.HasValue)
        {
            return Result(check, PlanCheckStatus.NotEvaluable, result.Message);
        }

        var passes = Compare(result.Value.Value, comparison, threshold);
        return Result(
            check,
            passes ? PlanCheckStatus.Pass : StatusForFailed(check),
            passes ? $"Metric '{metric}' passed." : $"Metric '{metric}' did not meet the configured threshold.",
            new Dictionary<string, string>
            {
                ["structure"] = result.StructureName ?? structureName,
                ["metric"] = metric,
                ["observed"] = Format(result.Value.Value),
                ["comparison"] = comparison.ToString(),
                ["threshold"] = Format(threshold),
                ["unit"] = result.Unit ?? Parameter(check, "unit") ?? string.Empty
            });
    }

    private PlanCheckResult CheckTargetCoverage(Plan plan, PlanCheckDefinition check)
    {
        var metric = RequiredParameter(check, "metric");
        var minPercentPrescription = DecimalParameter(check, "minPercentPrescription");
        var result = metricService.Evaluate(plan, plan.Prescription.TargetStructureId, metric);
        if (!result.IsEvaluable || !result.Value.HasValue)
        {
            return Result(check, PlanCheckStatus.NotEvaluable, result.Message);
        }

        var percentPrescription = result.Value.Value / plan.Prescription.TotalDoseGy * 100m;
        var passes = percentPrescription >= minPercentPrescription;
        return Result(
            check,
            passes ? PlanCheckStatus.Pass : StatusForFailed(check),
            passes ? "Target coverage is within the configured limit." : "Target coverage is below the configured limit.",
            new Dictionary<string, string>
            {
                ["metric"] = metric,
                ["observedGy"] = Format(result.Value.Value),
                ["observedPercentPrescription"] = Format(percentPrescription),
                ["minimumPercentPrescription"] = Format(minPercentPrescription)
            });
    }

    private PlanCheckResult CheckPlanQualityMetric(Plan plan, PlanCheckDefinition check)
    {
        var metric = RequiredParameter(check, "metric").ToUpperInvariant();
        var comparison = Enum.Parse<GoalComparison>(RequiredParameter(check, "comparison"), ignoreCase: true);
        var threshold = DecimalParameter(check, "threshold");
        var metrics = metricService.CalculateTargetMetrics(plan);
        var observed = metric switch
        {
            "CI" => metrics.ConformityIndex,
            "GI" => metrics.GradientIndex,
            "HI" => metrics.HomogeneityIndex,
            "R50" => metrics.R50,
            _ => throw new InvalidOperationException($"Unsupported plan-quality metric '{metric}'.")
        };

        if (!observed.HasValue)
        {
            return Result(check, PlanCheckStatus.NotEvaluable, $"Plan-quality metric '{metric}' was not available.");
        }

        var passes = Compare(observed.Value, comparison, threshold);
        return Result(
            check,
            passes ? PlanCheckStatus.Pass : StatusForFailed(check),
            passes ? $"Plan-quality metric '{metric}' passed." : $"Plan-quality metric '{metric}' did not meet the configured threshold.",
            new Dictionary<string, string>
            {
                ["metric"] = metric,
                ["observed"] = Format(observed.Value),
                ["comparison"] = comparison.ToString(),
                ["threshold"] = Format(threshold)
            });
    }

    private IEnumerable<PlanCheckResult> CheckDeliverability(PlanCheckRequest request, PlanCheckDefinition check)
    {
        if (request.MachineProfile is null)
        {
            return new[] { Result(check, PlanCheckStatus.NotEvaluable, "Deliverability check requires a machine profile.") };
        }

        return deliverabilityService.Evaluate(request.Plan, request.MachineProfile)
            .Select(result => Result(
                check,
                ToPlanCheckStatus(result.Status, check),
                result.Message,
                new Dictionary<string, string>
                {
                    ["deliverabilityCheckId"] = result.CheckId,
                    ["beamId"] = result.BeamId ?? string.Empty,
                    ["controlPointIndex"] = result.ControlPointIndex?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    ["observed"] = result.ObservedValue?.ToString("0.###", CultureInfo.InvariantCulture) ?? string.Empty,
                    ["expected"] = result.ExpectedValue?.ToString("0.###", CultureInfo.InvariantCulture) ?? string.Empty,
                    ["unit"] = result.Unit ?? string.Empty
                }));
    }

    private static PlanCheckStatus ToPlanCheckStatus(DeliverabilityStatus status, PlanCheckDefinition check)
    {
        return status switch
        {
            DeliverabilityStatus.Pass => PlanCheckStatus.Pass,
            DeliverabilityStatus.Warning => PlanCheckStatus.Warning,
            DeliverabilityStatus.Fail => StatusForFailed(check),
            DeliverabilityStatus.NotEvaluable => PlanCheckStatus.NotEvaluable,
            _ => PlanCheckStatus.NotEvaluable
        };
    }

    private static bool Compare(decimal observed, GoalComparison comparison, decimal threshold)
    {
        return comparison switch
        {
            GoalComparison.LessThan => observed < threshold,
            GoalComparison.LessThanOrEqual => observed <= threshold,
            GoalComparison.GreaterThan => observed > threshold,
            GoalComparison.GreaterThanOrEqual => observed >= threshold,
            GoalComparison.Equal => observed == threshold,
            _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Unsupported comparison.")
        };
    }

    private static PlanCheckStatus StatusForFailed(PlanCheckDefinition check)
    {
        return check.Severity switch
        {
            PlanCheckSeverity.Info => PlanCheckStatus.Warning,
            PlanCheckSeverity.Warning => PlanCheckStatus.Warning,
            PlanCheckSeverity.Failure => PlanCheckStatus.Fail,
            _ => PlanCheckStatus.Fail
        };
    }

    private static string ResolveStructureName(Plan plan, string value)
    {
        return string.Equals(value, "$target", StringComparison.OrdinalIgnoreCase)
            ? plan.Prescription.TargetStructureId
            : value;
    }

    private static string RequiredParameter(PlanCheckDefinition check, string key)
    {
        return check.Parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Check '{check.Id}' requires parameter '{key}'.");
    }

    private static string? Parameter(PlanCheckDefinition check, string key)
    {
        return check.Parameters.TryGetValue(key, out var value) ? value : null;
    }

    private static decimal DecimalParameter(PlanCheckDefinition check, string key)
    {
        return decimal.Parse(RequiredParameter(check, key), NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private static decimal? OptionalDecimalParameter(PlanCheckDefinition check, string key)
    {
        return check.Parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture)
            : null;
    }

    private static int? OptionalIntParameter(PlanCheckDefinition check, string key)
    {
        return check.Parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture)
            : null;
    }

    private static Beam[] TreatmentBeams(Plan plan)
    {
        return plan.Beams.Where(beam => !beam.IsSetupField).ToArray();
    }

    private static IReadOnlyDictionary<string, string> StructureEvidence(Structure structure)
    {
        return new Dictionary<string, string>
        {
            ["structureId"] = structure.Id,
            ["structureName"] = structure.Name,
            ["volumeCc"] = Format(structure.VolumeCc),
            ["hasContours"] = structure.HasContours.ToString(CultureInfo.InvariantCulture)
        };
    }

    private static PlanCheckResult Result(
        PlanCheckDefinition check,
        PlanCheckStatus status,
        string message,
        IReadOnlyDictionary<string, string>? evidence = null)
    {
        return new PlanCheckResult(check.Id, check.Title, status, check.Severity, message, check.Reference, evidence);
    }

    private static string Format(decimal value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
