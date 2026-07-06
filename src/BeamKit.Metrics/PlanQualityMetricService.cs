using BeamKit.Core.Domain;

namespace BeamKit.Metrics;

/// <summary>
/// Evaluates standardized metric expressions and plan-quality summaries from existing plan dose statistics.
/// </summary>
public sealed class PlanQualityMetricService
{
    /// <summary>
    /// Evaluates a metric expression for a structure.
    /// </summary>
    public MetricEvaluationResult Evaluate(Plan plan, string structureName, string expressionText)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var expression = DvhMetricExpression.Parse(expressionText);
        var structure = plan.FindStructure(structureName);
        if (structure is null)
        {
            return new MetricEvaluationResult(expression, structureName, null, null, false, $"Structure '{structureName}' was not found.");
        }

        if (expression.Kind == DvhMetricKind.Volume)
        {
            return new MetricEvaluationResult(expression, structure.Name, structure.VolumeCc, "cc", true, "Structure volume evaluated.");
        }

        if (expression.Kind is DvhMetricKind.ConformityIndex or DvhMetricKind.GradientIndex or DvhMetricKind.HomogeneityIndex or DvhMetricKind.R50)
        {
            var metrics = CalculateTargetMetrics(plan, structure.Name);
            var planQualityValue = expression.Kind switch
            {
                DvhMetricKind.ConformityIndex => metrics.ConformityIndex,
                DvhMetricKind.GradientIndex => metrics.GradientIndex,
                DvhMetricKind.HomogeneityIndex => metrics.HomogeneityIndex,
                DvhMetricKind.R50 => metrics.R50,
                _ => null
            };

            return planQualityValue.HasValue
                ? new MetricEvaluationResult(expression, structure.Name, planQualityValue, null, true, $"Plan-quality metric '{expression.Text}' evaluated.")
                : new MetricEvaluationResult(expression, structure.Name, null, null, false, $"Plan-quality metric '{expression.Text}' was not available for '{structure.Name}'.");
        }

        var key = expression.ToDoseMetricKey();
        if (key is null)
        {
            return new MetricEvaluationResult(expression, structure.Name, null, null, false, $"Metric '{expression.Text}' requires plan-quality summary evaluation.");
        }

        var statistics = plan.FindDoseStatistics(structure.Id);
        if (statistics is null)
        {
            return new MetricEvaluationResult(expression, structure.Name, null, null, false, $"Dose statistics for '{structure.Name}' were not found.");
        }

        var value = statistics.GetMetric(key);
        return value.HasValue
            ? new MetricEvaluationResult(expression, structure.Name, value, UnitFor(expression), true, $"Metric '{expression.Text}' evaluated.")
            : new MetricEvaluationResult(expression, structure.Name, null, UnitFor(expression), false, $"Dose metric '{key}' was not available for '{structure.Name}'.");
    }

    /// <summary>
    /// Calculates common target plan-quality metrics from available dose statistics.
    /// </summary>
    public PlanQualityMetrics CalculateTargetMetrics(Plan plan, string? targetStructureName = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var targetName = targetStructureName ?? plan.Prescription.TargetStructureId;
        var target = plan.FindStructure(targetName)
            ?? throw new InvalidOperationException($"Target structure '{targetName}' was not found.");
        var targetStats = plan.FindDoseStatistics(target.Id)
            ?? throw new InvalidOperationException($"Dose statistics for target structure '{target.Name}' were not found.");
        var body = plan.Structures.FirstOrDefault(structure => structure.Type == StructureType.External)
            ?? plan.FindStructure("BODY");
        var bodyStats = body is null ? null : plan.FindDoseStatistics(body.Id);
        var prescriptionDoseGy = plan.Prescription.TotalDoseGy;

        var d95 = targetStats.GetMetric(DoseMetricKeys.DoseAtVolumePercent(95m));
        var d98 = targetStats.GetMetric(DoseMetricKeys.DoseAtVolumePercent(98m));
        var d2 = targetStats.GetMetric(DoseMetricKeys.DoseAtVolumePercent(2m));
        var v95Percent = targetStats.GetMetric(PlanMetricKeys.VolumeAtPrescriptionPercent(95m))
            ?? targetStats.GetMetric(DoseMetricKeys.VolumeAtDoseGy(prescriptionDoseGy * 0.95m));
        var v100Percent = targetStats.GetMetric(PlanMetricKeys.VolumeAtPrescriptionPercent(100m))
            ?? targetStats.GetMetric(DoseMetricKeys.VolumeAtDoseGy(prescriptionDoseGy));
        var prescriptionIsodoseVolumeCc = bodyStats?.GetMetric(PlanMetricKeys.VolumeAtPrescriptionPercentCc(100m))
            ?? bodyStats?.GetMetric(PlanMetricKeys.VolumeAtDoseGyCc(prescriptionDoseGy));
        var halfPrescriptionVolumeCc = bodyStats?.GetMetric(PlanMetricKeys.VolumeAtPrescriptionPercentCc(50m))
            ?? bodyStats?.GetMetric(PlanMetricKeys.VolumeAtDoseGyCc(prescriptionDoseGy * 0.5m));
        var targetCoveredCc = targetStats.GetMetric(PlanMetricKeys.VolumeAtPrescriptionPercentCc(100m))
            ?? (v100Percent.HasValue ? target.VolumeCc * v100Percent.Value / 100m : null);

        decimal? conformityIndex = prescriptionIsodoseVolumeCc.HasValue && target.VolumeCc > 0
            ? prescriptionIsodoseVolumeCc.Value / target.VolumeCc
            : null;
        decimal? gradientIndex = prescriptionIsodoseVolumeCc is > 0m && halfPrescriptionVolumeCc.HasValue
            ? halfPrescriptionVolumeCc.Value / prescriptionIsodoseVolumeCc.Value
            : null;
        decimal? r50 = target.VolumeCc > 0 && halfPrescriptionVolumeCc.HasValue
            ? halfPrescriptionVolumeCc.Value / target.VolumeCc
            : null;
        decimal? homogeneityIndex = d2.HasValue && d98.HasValue && prescriptionDoseGy > 0
            ? (d2.Value - d98.Value) / prescriptionDoseGy
            : null;

        // Prefer Paddick when target-covered volume and prescription isodose volume are present.
        if (targetCoveredCc.HasValue && prescriptionIsodoseVolumeCc is > 0m && target.VolumeCc > 0)
        {
            conformityIndex = targetCoveredCc.Value * targetCoveredCc.Value / (target.VolumeCc * prescriptionIsodoseVolumeCc.Value);
        }

        return new PlanQualityMetrics(
            target.Name,
            prescriptionDoseGy,
            target.VolumeCc,
            d95,
            d98,
            d2,
            targetStats.GetMetric(DoseMetricKeys.MaximumDoseGy),
            targetStats.GetMetric(DoseMetricKeys.MeanDoseGy),
            v95Percent,
            v100Percent,
            conformityIndex,
            gradientIndex,
            homogeneityIndex,
            r50);
    }

    private static string UnitFor(DvhMetricExpression expression)
    {
        return expression.Kind switch
        {
            DvhMetricKind.VolumeAtDose => "%",
            DvhMetricKind.DoseAtVolume or DvhMetricKind.MaximumDose or DvhMetricKind.MeanDose or DvhMetricKind.MinimumDose => "Gy",
            _ => string.Empty
        };
    }
}
