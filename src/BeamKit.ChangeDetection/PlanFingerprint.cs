using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BeamKit.Core.Domain;

namespace BeamKit.ChangeDetection;

/// <summary>
/// Computes deterministic fingerprints for vendor-neutral plan snapshots.
/// </summary>
public static class PlanFingerprint
{
    /// <summary>
    /// Computes a SHA-256 fingerprint over release-relevant plan fields.
    /// </summary>
    public static string Compute(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var builder = new StringBuilder();
        AppendLine(builder, "schema", "BeamKit.PlanFingerprint.v1");
        AppendLine(builder, "plan.id", plan.Id);
        AppendLine(builder, "plan.courseId", plan.CourseId);
        AppendLine(builder, "plan.patientId", plan.Patient.Id);
        AppendLine(builder, "plan.diseaseSite", plan.DiseaseSite);
        AppendPrescription(builder, "plan.prescription", plan.Prescription);
        AppendStructures(builder, plan.Structures);
        AppendDose(builder, plan.Dose);
        AppendBeams(builder, plan.Beams);
        AppendClinicalGoals(builder, plan.ClinicalGoals);

        return Hash(builder.ToString());
    }

    /// <summary>
    /// Computes a SHA-256 fingerprint over prescription fields only.
    /// </summary>
    public static string Compute(Prescription prescription)
    {
        ArgumentNullException.ThrowIfNull(prescription);

        var builder = new StringBuilder();
        AppendLine(builder, "schema", "BeamKit.PrescriptionFingerprint.v1");
        AppendPrescription(builder, "prescription", prescription);
        return Hash(builder.ToString());
    }

    private static void AppendPrescription(StringBuilder builder, string prefix, Prescription prescription)
    {
        AppendLine(builder, $"{prefix}.totalDoseGy", prescription.TotalDoseGy);
        AppendLine(builder, $"{prefix}.fractionCount", prescription.FractionCount);
        AppendLine(builder, $"{prefix}.targetStructureId", prescription.TargetStructureId);
        AppendLine(builder, $"{prefix}.isSigned", prescription.IsSigned);
        AppendLine(builder, $"{prefix}.intent", prescription.Intent);
        AppendLine(builder, $"{prefix}.requestedEnergy", prescription.RequestedEnergy);
        AppendLine(builder, $"{prefix}.requestedTechniqueId", prescription.RequestedTechniqueId);
    }

    private static void AppendStructures(StringBuilder builder, IReadOnlyList<Structure> structures)
    {
        AppendLine(builder, "structures.count", structures.Count);
        foreach (var structure in structures.OrderBy(structure => structure.Id, StringComparer.OrdinalIgnoreCase))
        {
            var prefix = $"structure.{Normalize(structure.Id)}";
            AppendLine(builder, $"{prefix}.name", structure.Name);
            AppendLine(builder, $"{prefix}.type", structure.Type);
            AppendLine(builder, $"{prefix}.volumeCc", structure.VolumeCc);
            AppendLine(builder, $"{prefix}.hasContours", structure.HasContours);
        }
    }

    private static void AppendDose(StringBuilder builder, Dose? dose)
    {
        AppendLine(builder, "dose.present", dose is not null);
        if (dose is null)
        {
            return;
        }

        AppendLine(builder, "dose.id", dose.Id);
        AppendLine(builder, "dose.grid.spacingXMm", dose.Grid.SpacingXMm);
        AppendLine(builder, "dose.grid.spacingYMm", dose.Grid.SpacingYMm);
        AppendLine(builder, "dose.grid.spacingZMm", dose.Grid.SpacingZMm);
        AppendLine(builder, "dose.calculationModel", dose.CalculationModel);
        AppendLine(builder, "dose.calculationModelVersion", dose.CalculationModelVersion);
        AppendLine(builder, "dose.statistics.count", dose.Statistics.Count);

        foreach (var statistics in dose.Statistics.OrderBy(statistics => statistics.StructureId, StringComparer.OrdinalIgnoreCase))
        {
            var prefix = $"dose.statistics.{Normalize(statistics.StructureId)}";
            AppendLine(builder, $"{prefix}.metrics.count", statistics.Metrics.Count);
            foreach (var metric in statistics.Metrics.OrderBy(metric => metric.Key, StringComparer.OrdinalIgnoreCase))
            {
                AppendLine(builder, $"{prefix}.metric.{Normalize(metric.Key)}", metric.Value);
            }
        }
    }

    private static void AppendBeams(StringBuilder builder, IReadOnlyList<Beam> beams)
    {
        AppendLine(builder, "beams.count", beams.Count);
        foreach (var beam in beams.OrderBy(beam => beam.Id, StringComparer.OrdinalIgnoreCase))
        {
            var prefix = $"beam.{Normalize(beam.Id)}";
            AppendLine(builder, $"{prefix}.name", beam.Name);
            AppendLine(builder, $"{prefix}.modality", beam.Modality);
            AppendLine(builder, $"{prefix}.energy", beam.Energy);
            AppendLine(builder, $"{prefix}.gantryAngleDegrees", beam.GantryAngleDegrees);
            AppendLine(builder, $"{prefix}.monitorUnits", beam.MonitorUnits);
            AppendLine(builder, $"{prefix}.treatmentUnitId", beam.TreatmentUnitId);
            AppendLine(builder, $"{prefix}.techniqueId", beam.TechniqueId);
            AppendLine(builder, $"{prefix}.isSetupField", beam.IsSetupField);
            AppendLine(builder, $"{prefix}.beamModelId", beam.BeamModelId);
            AppendLine(builder, $"{prefix}.jawTrackingEnabled", beam.JawTrackingEnabled);
            AppendLine(builder, $"{prefix}.controlPoints.count", beam.ControlPoints.Count);

            foreach (var controlPoint in beam.ControlPoints.OrderBy(controlPoint => controlPoint.Index))
            {
                var controlPointPrefix = $"{prefix}.controlPoint.{controlPoint.Index.ToString(CultureInfo.InvariantCulture)}";
                AppendLine(builder, $"{controlPointPrefix}.gantryAngleDegrees", controlPoint.GantryAngleDegrees);
                AppendLine(builder, $"{controlPointPrefix}.cumulativeMetersetWeight", controlPoint.CumulativeMetersetWeight);
                AppendJawPositions(builder, controlPointPrefix, controlPoint.JawPositions);
            }
        }
    }

    private static void AppendJawPositions(StringBuilder builder, string prefix, BeamJawPositions? jawPositions)
    {
        AppendLine(builder, $"{prefix}.jaws.present", jawPositions is not null);
        if (jawPositions is null)
        {
            return;
        }

        AppendLine(builder, $"{prefix}.jaws.x1Cm", jawPositions.X1Cm);
        AppendLine(builder, $"{prefix}.jaws.x2Cm", jawPositions.X2Cm);
        AppendLine(builder, $"{prefix}.jaws.y1Cm", jawPositions.Y1Cm);
        AppendLine(builder, $"{prefix}.jaws.y2Cm", jawPositions.Y2Cm);
    }

    private static void AppendClinicalGoals(StringBuilder builder, IReadOnlyList<ClinicalGoal> goals)
    {
        AppendLine(builder, "clinicalGoals.count", goals.Count);
        foreach (var goal in goals.OrderBy(goal => goal.Id, StringComparer.OrdinalIgnoreCase))
        {
            var prefix = $"clinicalGoal.{Normalize(goal.Id)}";
            AppendLine(builder, $"{prefix}.structureName", goal.StructureName);
            AppendLine(builder, $"{prefix}.metricKey", goal.MetricKey);
            AppendLine(builder, $"{prefix}.comparison", goal.Comparison);
            AppendLine(builder, $"{prefix}.threshold", goal.Threshold);
            AppendLine(builder, $"{prefix}.unit", goal.Unit);
            AppendLine(builder, $"{prefix}.severity", goal.Severity);
        }
    }

    private static void AppendLine(StringBuilder builder, string key, string? value)
    {
        builder.Append(key);
        builder.Append('=');
        builder.Append(Normalize(value));
        builder.Append('\n');
    }

    private static void AppendLine(StringBuilder builder, string key, decimal? value)
    {
        AppendLine(builder, key, value.HasValue ? value.Value.ToString("G29", CultureInfo.InvariantCulture) : null);
    }

    private static void AppendLine(StringBuilder builder, string key, decimal value)
    {
        AppendLine(builder, key, value.ToString("G29", CultureInfo.InvariantCulture));
    }

    private static void AppendLine(StringBuilder builder, string key, int value)
    {
        AppendLine(builder, key, value.ToString(CultureInfo.InvariantCulture));
    }

    private static void AppendLine(StringBuilder builder, string key, bool? value)
    {
        AppendLine(builder, key, value.HasValue ? FormatBoolean(value.Value) : null);
    }

    private static void AppendLine(StringBuilder builder, string key, bool value)
    {
        AppendLine(builder, key, FormatBoolean(value));
    }

    private static void AppendLine<TEnum>(StringBuilder builder, string key, TEnum value)
        where TEnum : struct, Enum
    {
        AppendLine(builder, key, value.ToString());
    }

    private static string Hash(string canonicalText)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalText));
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string FormatBoolean(bool value)
    {
        return value ? "true" : "false";
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "<null>"
            : value.Trim()
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal)
                .Replace("=", "\\=", StringComparison.Ordinal);
    }
}
