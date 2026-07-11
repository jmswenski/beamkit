using BeamKit.Core.Domain;

namespace BeamKit.ChangeDetection;

/// <summary>
/// Compares two BeamKit plans and reports clinically relevant differences.
/// </summary>
public sealed class PlanChangeDetector
{
    /// <summary>
    /// Compares a baseline plan to a newer plan.
    /// </summary>
    public PlanChangeReport Compare(Plan baseline, Plan comparison, PlanChangeDetectionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(comparison);

        options ??= new PlanChangeDetectionOptions();
        var changes = new List<PlanChange>();
        ComparePlanMetadata(baseline, comparison, changes);
        ComparePrescription(baseline.Prescription, comparison.Prescription, options, changes);
        CompareStructures(baseline.Structures, comparison.Structures, options, changes);
        CompareDose(baseline.Dose, comparison.Dose, options, changes);
        CompareBeams(baseline.Beams, comparison.Beams, options, changes);
        CompareClinicalGoals(baseline.ClinicalGoals, comparison.ClinicalGoals, changes);
        return new PlanChangeReport(baseline.Id, comparison.Id, changes);
    }

    private static void ComparePlanMetadata(Plan before, Plan after, ICollection<PlanChange> changes)
    {
        AddMetadataChangeIfDifferent(changes, "Plan.Id", before.Id, after.Id, PlanChangeSeverity.Blocking);
        AddMetadataChangeIfDifferent(changes, "Patient.Id", before.Patient.Id, after.Patient.Id, PlanChangeSeverity.Blocking);
        AddMetadataChangeIfDifferent(changes, "Plan.CourseId", before.CourseId, after.CourseId, PlanChangeSeverity.Blocking);
        AddMetadataChangeIfDifferent(changes, "Plan.DiseaseSite", before.DiseaseSite, after.DiseaseSite, PlanChangeSeverity.Warning);
    }

    private static void ComparePrescription(Prescription before, Prescription after, PlanChangeDetectionOptions options, ICollection<PlanChange> changes)
    {
        AddIfChanged(changes, "Prescription.TotalDoseGy", before.TotalDoseGy, after.TotalDoseGy, options.DoseToleranceGy);
        if (before.FractionCount != after.FractionCount)
        {
            AddChange(changes, "Prescription.FractionCount", before.FractionCount.ToString(System.Globalization.CultureInfo.InvariantCulture), after.FractionCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        AddIfChanged(changes, "Prescription.TargetStructureId", before.TargetStructureId, after.TargetStructureId);
        if (before.IsSigned != after.IsSigned)
        {
            AddChange(changes, "Prescription.IsSigned", before.IsSigned.ToString(), after.IsSigned.ToString());
        }

        AddIfChanged(changes, "Prescription.Intent", before.Intent, after.Intent);
        AddIfChanged(changes, "Prescription.RequestedEnergy", before.RequestedEnergy, after.RequestedEnergy);
        AddIfChanged(changes, "Prescription.RequestedTechniqueId", before.RequestedTechniqueId, after.RequestedTechniqueId);
    }

    private static void CompareStructures(
        IReadOnlyList<Structure> before,
        IReadOnlyList<Structure> after,
        PlanChangeDetectionOptions options,
        ICollection<PlanChange> changes)
    {
        var beforeById = before.ToDictionary(structure => structure.Id, StringComparer.OrdinalIgnoreCase);
        var afterById = after.ToDictionary(structure => structure.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var removed in beforeById.Keys.Except(afterById.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            changes.Add(new PlanChange(PlanChangeType.StructureRemoved, PlanChangeSeverity.Blocking, removed, "Structure was removed."));
        }

        foreach (var added in afterById.Keys.Except(beforeById.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            changes.Add(new PlanChange(PlanChangeType.StructureAdded, PlanChangeSeverity.Warning, added, "Structure was added."));
        }

        foreach (var id in beforeById.Keys.Intersect(afterById.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            var beforeStructure = beforeById[id];
            var afterStructure = afterById[id];
            if (!string.Equals(beforeStructure.Name, afterStructure.Name, StringComparison.Ordinal))
            {
                changes.Add(new PlanChange(PlanChangeType.StructureRenamed, PlanChangeSeverity.Warning, id, "Structure name changed.", beforeStructure.Name, afterStructure.Name));
            }

            if (Math.Abs(beforeStructure.VolumeCc - afterStructure.VolumeCc) > options.VolumeToleranceCc)
            {
                changes.Add(new PlanChange(
                    PlanChangeType.StructureVolumeChanged,
                    PlanChangeSeverity.Warning,
                    id,
                    "Structure volume changed.",
                    FormatDecimal(beforeStructure.VolumeCc),
                    FormatDecimal(afterStructure.VolumeCc)));
            }

            if (beforeStructure.HasContours != afterStructure.HasContours)
            {
                changes.Add(new PlanChange(
                    PlanChangeType.StructureVolumeChanged,
                    PlanChangeSeverity.Blocking,
                    id,
                    "Structure contour state changed.",
                    beforeStructure.HasContours.ToString(),
                    afterStructure.HasContours.ToString()));
            }
        }
    }

    private static void CompareDose(Dose? before, Dose? after, PlanChangeDetectionOptions options, ICollection<PlanChange> changes)
    {
        if (before is null && after is null)
        {
            return;
        }

        if (before is null)
        {
            changes.Add(new PlanChange(PlanChangeType.DoseAdded, PlanChangeSeverity.Warning, after!.Id, "Dose was added."));
            return;
        }

        if (after is null)
        {
            changes.Add(new PlanChange(PlanChangeType.DoseRemoved, PlanChangeSeverity.Blocking, before.Id, "Dose was removed."));
            return;
        }

        if (Math.Abs(before.Grid.SpacingXMm - after.Grid.SpacingXMm) > options.GridSpacingToleranceMm
            || Math.Abs(before.Grid.SpacingYMm - after.Grid.SpacingYMm) > options.GridSpacingToleranceMm
            || Math.Abs(before.Grid.SpacingZMm - after.Grid.SpacingZMm) > options.GridSpacingToleranceMm)
        {
            changes.Add(new PlanChange(
                PlanChangeType.DoseGridChanged,
                PlanChangeSeverity.Blocking,
                before.Id,
                "Dose grid spacing changed.",
                $"{FormatDecimal(before.Grid.SpacingXMm)}/{FormatDecimal(before.Grid.SpacingYMm)}/{FormatDecimal(before.Grid.SpacingZMm)}",
                $"{FormatDecimal(after.Grid.SpacingXMm)}/{FormatDecimal(after.Grid.SpacingYMm)}/{FormatDecimal(after.Grid.SpacingZMm)}"));
        }

        CompareDoseText("Dose.CalculationModel", before.CalculationModel, after.CalculationModel, changes);
        CompareDoseText("Dose.CalculationModelVersion", before.CalculationModelVersion, after.CalculationModelVersion, changes);
        CompareDoseStatistics(before.Statistics, after.Statistics, options, changes);
    }

    private static void CompareDoseStatistics(
        IReadOnlyList<DoseStatistics> before,
        IReadOnlyList<DoseStatistics> after,
        PlanChangeDetectionOptions options,
        ICollection<PlanChange> changes)
    {
        var beforeByStructure = before.ToDictionary(statistics => statistics.StructureId, StringComparer.OrdinalIgnoreCase);
        var afterByStructure = after.ToDictionary(statistics => statistics.StructureId, StringComparer.OrdinalIgnoreCase);

        foreach (var structureId in beforeByStructure.Keys.Union(afterByStructure.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            beforeByStructure.TryGetValue(structureId, out var beforeStatistics);
            afterByStructure.TryGetValue(structureId, out var afterStatistics);
            var beforeMetrics = beforeStatistics?.Metrics ?? new Dictionary<string, decimal>();
            var afterMetrics = afterStatistics?.Metrics ?? new Dictionary<string, decimal>();

            foreach (var metricKey in beforeMetrics.Keys.Union(afterMetrics.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
            {
                var hasBefore = beforeMetrics.TryGetValue(metricKey, out var beforeValue);
                var hasAfter = afterMetrics.TryGetValue(metricKey, out var afterValue);
                if (!hasBefore || !hasAfter || Math.Abs(beforeValue - afterValue) > options.DoseToleranceGy)
                {
                    changes.Add(new PlanChange(
                        PlanChangeType.DoseMetricChanged,
                        PlanChangeSeverity.Warning,
                        $"{structureId}.{metricKey}",
                        "Dose metric changed.",
                        hasBefore ? FormatDecimal(beforeValue) : null,
                        hasAfter ? FormatDecimal(afterValue) : null));
                }
            }
        }
    }

    private static void CompareBeams(IReadOnlyList<Beam> before, IReadOnlyList<Beam> after, PlanChangeDetectionOptions options, ICollection<PlanChange> changes)
    {
        var beforeById = before.ToDictionary(beam => beam.Id, StringComparer.OrdinalIgnoreCase);
        var afterById = after.ToDictionary(beam => beam.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var removed in beforeById.Keys.Except(afterById.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            changes.Add(new PlanChange(PlanChangeType.BeamRemoved, PlanChangeSeverity.Warning, removed, "Beam was removed."));
        }

        foreach (var added in afterById.Keys.Except(beforeById.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            changes.Add(new PlanChange(PlanChangeType.BeamAdded, PlanChangeSeverity.Warning, added, "Beam was added."));
        }

        foreach (var id in beforeById.Keys.Intersect(afterById.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            var beforeBeam = beforeById[id];
            var afterBeam = afterById[id];
            CompareBeamText(id, "Name", beforeBeam.Name, afterBeam.Name, changes);
            CompareBeamText(id, "Modality", beforeBeam.Modality, afterBeam.Modality, changes);
            CompareBeamText(id, "Energy", beforeBeam.Energy, afterBeam.Energy, changes);
            CompareBeamText(id, "TreatmentUnitId", beforeBeam.TreatmentUnitId, afterBeam.TreatmentUnitId, changes);
            CompareBeamText(id, "TechniqueId", beforeBeam.TechniqueId, afterBeam.TechniqueId, changes);
            CompareBeamText(id, "BeamModelId", beforeBeam.BeamModelId, afterBeam.BeamModelId, changes);
            CompareBeamBoolean(id, "JawTrackingEnabled", beforeBeam.JawTrackingEnabled, afterBeam.JawTrackingEnabled, changes);
            CompareBeamDecimal(id, "GantryAngleDegrees", beforeBeam.GantryAngleDegrees, afterBeam.GantryAngleDegrees, options.GantryAngleToleranceDegrees, changes);
            CompareBeamDecimal(id, "MonitorUnits", beforeBeam.MonitorUnits, afterBeam.MonitorUnits, options.MonitorUnitTolerance, changes);
            CompareControlPoints(id, beforeBeam.ControlPoints, afterBeam.ControlPoints, options, changes);
        }
    }

    private static void CompareClinicalGoals(IReadOnlyList<ClinicalGoal> before, IReadOnlyList<ClinicalGoal> after, ICollection<PlanChange> changes)
    {
        var beforeById = before.ToDictionary(goal => goal.Id, StringComparer.OrdinalIgnoreCase);
        var afterById = after.ToDictionary(goal => goal.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var removed in beforeById.Keys.Except(afterById.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            changes.Add(new PlanChange(PlanChangeType.ClinicalGoalRemoved, PlanChangeSeverity.Blocking, removed, "Clinical goal was removed."));
        }

        foreach (var added in afterById.Keys.Except(beforeById.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            changes.Add(new PlanChange(PlanChangeType.ClinicalGoalAdded, PlanChangeSeverity.Warning, added, "Clinical goal was added."));
        }

        foreach (var id in beforeById.Keys.Intersect(afterById.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            var beforeGoal = beforeById[id];
            var afterGoal = afterById[id];
            AddClinicalGoalChangeIfDifferent(changes, id, "StructureName", beforeGoal.StructureName, afterGoal.StructureName);
            AddClinicalGoalChangeIfDifferent(changes, id, "MetricKey", beforeGoal.MetricKey, afterGoal.MetricKey);
            AddClinicalGoalChangeIfDifferent(changes, id, "Comparison", beforeGoal.Comparison.ToString(), afterGoal.Comparison.ToString());
            AddClinicalGoalChangeIfDifferent(changes, id, "Threshold", FormatDecimal(beforeGoal.Threshold), FormatDecimal(afterGoal.Threshold));
            AddClinicalGoalChangeIfDifferent(changes, id, "Unit", beforeGoal.Unit, afterGoal.Unit);
            AddClinicalGoalChangeIfDifferent(changes, id, "Severity", beforeGoal.Severity.ToString(), afterGoal.Severity.ToString());
        }
    }

    private static void CompareDoseText(string subject, string? before, string? after, ICollection<PlanChange> changes)
    {
        if (!string.Equals(before, after, StringComparison.Ordinal))
        {
            changes.Add(new PlanChange(PlanChangeType.DoseCalculationChanged, PlanChangeSeverity.Blocking, subject, "Dose calculation metadata changed.", before, after));
        }
    }

    private static void CompareBeamText(string beamId, string propertyName, string? before, string? after, ICollection<PlanChange> changes)
    {
        if (!string.Equals(before, after, StringComparison.Ordinal))
        {
            changes.Add(new PlanChange(PlanChangeType.BeamChanged, PlanChangeSeverity.Warning, $"Beam.{beamId}.{propertyName}", "Beam property changed.", before, after));
        }
    }

    private static void CompareBeamBoolean(string beamId, string propertyName, bool? before, bool? after, ICollection<PlanChange> changes)
    {
        if (before != after)
        {
            changes.Add(new PlanChange(
                PlanChangeType.BeamChanged,
                PlanChangeSeverity.Warning,
                $"Beam.{beamId}.{propertyName}",
                "Beam property changed.",
                before?.ToString(),
                after?.ToString()));
        }
    }

    private static void CompareBeamDecimal(
        string beamId,
        string propertyName,
        decimal? before,
        decimal? after,
        decimal tolerance,
        ICollection<PlanChange> changes)
    {
        if (before.HasValue != after.HasValue || (before.HasValue && after.HasValue && Math.Abs(before.Value - after.Value) > tolerance))
        {
            changes.Add(new PlanChange(
                PlanChangeType.BeamChanged,
                PlanChangeSeverity.Warning,
                $"Beam.{beamId}.{propertyName}",
                "Beam property changed.",
                before.HasValue ? FormatDecimal(before.Value) : null,
                after.HasValue ? FormatDecimal(after.Value) : null));
        }
    }

    private static void CompareControlPoints(
        string beamId,
        IReadOnlyList<BeamControlPoint> before,
        IReadOnlyList<BeamControlPoint> after,
        PlanChangeDetectionOptions options,
        ICollection<PlanChange> changes)
    {
        var beforeByIndex = before.ToDictionary(controlPoint => controlPoint.Index);
        var afterByIndex = after.ToDictionary(controlPoint => controlPoint.Index);

        foreach (var removed in beforeByIndex.Keys.Except(afterByIndex.Keys).Order())
        {
            changes.Add(new PlanChange(
                PlanChangeType.BeamControlPointChanged,
                PlanChangeSeverity.Warning,
                $"Beam.{beamId}.ControlPoint.{removed}",
                "Beam control point was removed."));
        }

        foreach (var added in afterByIndex.Keys.Except(beforeByIndex.Keys).Order())
        {
            changes.Add(new PlanChange(
                PlanChangeType.BeamControlPointChanged,
                PlanChangeSeverity.Warning,
                $"Beam.{beamId}.ControlPoint.{added}",
                "Beam control point was added."));
        }

        foreach (var index in beforeByIndex.Keys.Intersect(afterByIndex.Keys).Order())
        {
            var beforeControlPoint = beforeByIndex[index];
            var afterControlPoint = afterByIndex[index];
            CompareControlPointDecimal(beamId, index, "GantryAngleDegrees", beforeControlPoint.GantryAngleDegrees, afterControlPoint.GantryAngleDegrees, options.GantryAngleToleranceDegrees, changes);
            CompareControlPointDecimal(beamId, index, "CumulativeMetersetWeight", beforeControlPoint.CumulativeMetersetWeight, afterControlPoint.CumulativeMetersetWeight, options.ControlPointWeightTolerance, changes);
            CompareJawPositions(beamId, index, beforeControlPoint.JawPositions, afterControlPoint.JawPositions, options, changes);
        }
    }

    private static void CompareJawPositions(
        string beamId,
        int controlPointIndex,
        BeamJawPositions? before,
        BeamJawPositions? after,
        PlanChangeDetectionOptions options,
        ICollection<PlanChange> changes)
    {
        if (before is null && after is null)
        {
            return;
        }

        if (before is null || after is null)
        {
            changes.Add(new PlanChange(
                PlanChangeType.BeamControlPointChanged,
                PlanChangeSeverity.Warning,
                $"Beam.{beamId}.ControlPoint.{controlPointIndex}.Jaws",
                "Beam jaw geometry changed.",
                before is null ? null : "present",
                after is null ? null : "present"));
            return;
        }

        CompareControlPointDecimal(beamId, controlPointIndex, "Jaw.X1Cm", before.X1Cm, after.X1Cm, options.JawPositionToleranceCm, changes);
        CompareControlPointDecimal(beamId, controlPointIndex, "Jaw.X2Cm", before.X2Cm, after.X2Cm, options.JawPositionToleranceCm, changes);
        CompareControlPointDecimal(beamId, controlPointIndex, "Jaw.Y1Cm", before.Y1Cm, after.Y1Cm, options.JawPositionToleranceCm, changes);
        CompareControlPointDecimal(beamId, controlPointIndex, "Jaw.Y2Cm", before.Y2Cm, after.Y2Cm, options.JawPositionToleranceCm, changes);
    }

    private static void CompareControlPointDecimal(
        string beamId,
        int controlPointIndex,
        string propertyName,
        decimal? before,
        decimal? after,
        decimal tolerance,
        ICollection<PlanChange> changes)
    {
        if (before.HasValue != after.HasValue || (before.HasValue && after.HasValue && Math.Abs(before.Value - after.Value) > tolerance))
        {
            changes.Add(new PlanChange(
                PlanChangeType.BeamControlPointChanged,
                PlanChangeSeverity.Warning,
                $"Beam.{beamId}.ControlPoint.{controlPointIndex}.{propertyName}",
                "Beam control-point property changed.",
                before.HasValue ? FormatDecimal(before.Value) : null,
                after.HasValue ? FormatDecimal(after.Value) : null));
        }
    }

    private static void AddIfChanged(ICollection<PlanChange> changes, string subject, decimal before, decimal after, decimal tolerance)
    {
        if (Math.Abs(before - after) > tolerance)
        {
            AddChange(changes, subject, FormatDecimal(before), FormatDecimal(after));
        }
    }

    private static void AddIfChanged(ICollection<PlanChange> changes, string subject, string? before, string? after)
    {
        if (!string.Equals(before, after, StringComparison.Ordinal))
        {
            AddChange(changes, subject, before, after);
        }
    }

    private static void AddChange(ICollection<PlanChange> changes, string subject, string? before, string? after)
    {
        changes.Add(new PlanChange(PlanChangeType.PrescriptionChanged, PlanChangeSeverity.Blocking, subject, "Prescription changed.", before, after));
    }

    private static void AddMetadataChangeIfDifferent(
        ICollection<PlanChange> changes,
        string subject,
        string? before,
        string? after,
        PlanChangeSeverity severity)
    {
        if (!string.Equals(before, after, StringComparison.Ordinal))
        {
            changes.Add(new PlanChange(PlanChangeType.PlanMetadataChanged, severity, subject, "Plan metadata changed.", before, after));
        }
    }

    private static void AddClinicalGoalChangeIfDifferent(
        ICollection<PlanChange> changes,
        string goalId,
        string propertyName,
        string? before,
        string? after)
    {
        if (!string.Equals(before, after, StringComparison.Ordinal))
        {
            changes.Add(new PlanChange(
                PlanChangeType.ClinicalGoalChanged,
                PlanChangeSeverity.Warning,
                $"ClinicalGoal.{goalId}.{propertyName}",
                "Clinical goal property changed.",
                before,
                after));
        }
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }
}
