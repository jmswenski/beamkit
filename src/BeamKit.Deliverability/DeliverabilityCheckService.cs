using BeamKit.Core.Domain;

namespace BeamKit.Deliverability;

/// <summary>
/// Evaluates beam deliverability against a machine constraint profile.
/// </summary>
public sealed class DeliverabilityCheckService
{
    private readonly BeamDeliverabilityAnalyzer analyzer = new();

    /// <summary>
    /// Evaluates all supported deliverability checks.
    /// </summary>
    public IReadOnlyList<DeliverabilityCheckResult> Evaluate(Plan plan, MachineConstraintProfile profile)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(profile);

        var results = new List<DeliverabilityCheckResult>();
        var treatmentBeams = plan.Beams.Where(beam => !beam.IsSetupField).ToArray();
        if (treatmentBeams.Length == 0)
        {
            return new[]
            {
                new DeliverabilityCheckResult("deliverability.beams.present", "Treatment beams present", DeliverabilityStatus.NotEvaluable, "Plan has no treatment beams.")
            };
        }

        results.AddRange(CheckDoseCalculation(plan, profile));

        foreach (var beam in treatmentBeams)
        {
            results.AddRange(CheckBeamProfile(beam, profile));
            results.AddRange(CheckJawTracking(beam, profile));
            results.AddRange(CheckBeamMonitorUnits(beam, profile));
            results.AddRange(CheckSegments(plan, beam, profile));
            results.AddRange(CheckFieldSize(beam, profile));
        }

        return results;
    }

    private static IEnumerable<DeliverabilityCheckResult> CheckDoseCalculation(Plan plan, MachineConstraintProfile profile)
    {
        if (profile.CalculationModel is null && profile.CalculationModelVersion is null)
        {
            yield break;
        }

        if (plan.Dose is null)
        {
            yield return new DeliverabilityCheckResult(
                "deliverability.dose.calculation-model",
                "Dose calculation model",
                DeliverabilityStatus.NotEvaluable,
                "Plan has no dose calculation metadata.");
            yield break;
        }

        if (profile.CalculationModel is not null)
        {
            yield return CheckTextValue(
                "deliverability.dose.calculation-model",
                "Dose calculation model",
                "Plan dose calculation model",
                plan.Dose.CalculationModel,
                profile.CalculationModel);
        }

        if (profile.CalculationModelVersion is not null)
        {
            yield return CheckTextValue(
                "deliverability.dose.calculation-version",
                "Dose calculation version",
                "Plan dose calculation version",
                plan.Dose.CalculationModelVersion,
                profile.CalculationModelVersion);
        }
    }

    private static IEnumerable<DeliverabilityCheckResult> CheckBeamProfile(Beam beam, MachineConstraintProfile profile)
    {
        if (profile.MachineId is not null)
        {
            yield return CheckTextValue(
                "deliverability.beam.machine",
                "Treatment machine",
                $"Beam '{beam.Id}' treatment machine",
                beam.TreatmentUnitId,
                profile.MachineId,
                beam.Id);
        }

        var allowedEnergies = AllowedValues(profile.AllowedEnergies, profile.Energy);
        if (allowedEnergies.Count > 0)
        {
            yield return CheckAllowedValue(
                "deliverability.beam.energy",
                "Allowed beam energy",
                $"Beam '{beam.Id}' energy",
                beam.Energy,
                allowedEnergies,
                beam.Id);
        }

        if (profile.AllowedTechniques.Count > 0)
        {
            yield return CheckAllowedValue(
                "deliverability.beam.technique",
                "Allowed beam technique",
                $"Beam '{beam.Id}' technique",
                beam.TechniqueId,
                profile.AllowedTechniques,
                beam.Id);
        }

        var allowedBeamModels = AllowedValues(profile.AllowedBeamModelIds, profile.BeamModelId);
        if (allowedBeamModels.Count > 0)
        {
            yield return CheckAllowedValue(
                "deliverability.beam.model",
                "Allowed beam model",
                $"Beam '{beam.Id}' beam model",
                beam.BeamModelId,
                allowedBeamModels,
                beam.Id);
        }
    }

    private static IEnumerable<DeliverabilityCheckResult> CheckJawTracking(Beam beam, MachineConstraintProfile profile)
    {
        if (!profile.RequireJawTracking.HasValue)
        {
            yield break;
        }

        if (!beam.JawTrackingEnabled.HasValue)
        {
            yield return new DeliverabilityCheckResult(
                "deliverability.jaw-tracking",
                "Jaw tracking",
                DeliverabilityStatus.NotEvaluable,
                $"Beam '{beam.Id}' is missing jaw-tracking metadata.",
                beam.Id);
            yield break;
        }

        var passes = beam.JawTrackingEnabled.Value == profile.RequireJawTracking.Value;
        yield return new DeliverabilityCheckResult(
            "deliverability.jaw-tracking",
            "Jaw tracking",
            passes ? DeliverabilityStatus.Pass : DeliverabilityStatus.Fail,
            passes
                ? $"Beam '{beam.Id}' jaw-tracking state matches profile."
                : $"Beam '{beam.Id}' jaw-tracking state does not match profile.",
            beam.Id);
    }

    private static IEnumerable<DeliverabilityCheckResult> CheckBeamMonitorUnits(Beam beam, MachineConstraintProfile profile)
    {
        if (!profile.MinMonitorUnitsPerBeam.HasValue)
        {
            yield break;
        }

        if (!beam.MonitorUnits.HasValue)
        {
            yield return new DeliverabilityCheckResult(
                "deliverability.beam.min-mu",
                "Minimum beam MU",
                DeliverabilityStatus.NotEvaluable,
                $"Beam '{beam.Id}' is missing monitor units.",
                beam.Id,
                expectedValue: profile.MinMonitorUnitsPerBeam,
                unit: "MU");
            yield break;
        }

        var passes = beam.MonitorUnits.Value >= profile.MinMonitorUnitsPerBeam.Value;
        yield return new DeliverabilityCheckResult(
            "deliverability.beam.min-mu",
            "Minimum beam MU",
            passes ? DeliverabilityStatus.Pass : DeliverabilityStatus.Fail,
            passes
                ? $"Beam '{beam.Id}' has sufficient MU."
                : $"Beam '{beam.Id}' has MU below the configured minimum.",
            beam.Id,
            observedValue: beam.MonitorUnits,
            expectedValue: profile.MinMonitorUnitsPerBeam,
            unit: "MU");
    }

    private IEnumerable<DeliverabilityCheckResult> CheckSegments(Plan plan, Beam beam, MachineConstraintProfile profile)
    {
        var minMonitorUnitsPerDegree = ResolveMinMonitorUnitsPerDegree(plan, beam, profile);
        if (minMonitorUnitsPerDegree.NoMatchingConstraint)
        {
            yield return new DeliverabilityCheckResult(
                "deliverability.arc.min-mu-per-degree.profile",
                "Minimum arc MU per degree profile",
                DeliverabilityStatus.NotEvaluable,
                $"Beam '{beam.Id}' did not match any MU/degree constraint in the machine profile.",
                beam.Id);
        }

        if (!profile.MinMonitorUnitsPerSegment.HasValue && !minMonitorUnitsPerDegree.Threshold.HasValue && !profile.MaxDcaStepSizeDegrees.HasValue)
        {
            yield break;
        }

        var segments = analyzer.CalculateSegments(beam);
        if (segments.Count == 0)
        {
            yield return new DeliverabilityCheckResult(
                "deliverability.segment.data",
                "Control-point segment data",
                DeliverabilityStatus.NotEvaluable,
                $"Beam '{beam.Id}' is missing monitor units or control-point weights.",
                beam.Id);
            yield break;
        }

        foreach (var segment in segments)
        {
            if (profile.MinMonitorUnitsPerSegment.HasValue)
            {
                var passes = segment.DeltaMonitorUnits >= profile.MinMonitorUnitsPerSegment.Value;
                yield return new DeliverabilityCheckResult(
                    "deliverability.segment.min-mu",
                    "Minimum segment MU",
                    passes ? DeliverabilityStatus.Pass : DeliverabilityStatus.Fail,
                    passes
                        ? $"Beam '{beam.Id}' segment has sufficient MU."
                        : $"Beam '{beam.Id}' segment MU is below the configured minimum.",
                    beam.Id,
                    segment.EndControlPointIndex,
                    segment.DeltaMonitorUnits,
                    profile.MinMonitorUnitsPerSegment,
                    "MU");
            }

            if (minMonitorUnitsPerDegree.Threshold.HasValue && segment.MonitorUnitsPerDegree.HasValue)
            {
                var passes = segment.MonitorUnitsPerDegree.Value >= minMonitorUnitsPerDegree.Threshold.Value;
                yield return new DeliverabilityCheckResult(
                    "deliverability.arc.min-mu-per-degree",
                    "Minimum arc MU per degree",
                    passes ? DeliverabilityStatus.Pass : DeliverabilityStatus.Fail,
                    passes
                        ? $"Beam '{beam.Id}' segment has sufficient MU per degree."
                        : $"Beam '{beam.Id}' segment MU per degree is below the configured minimum.",
                    beam.Id,
                    segment.EndControlPointIndex,
                    segment.MonitorUnitsPerDegree,
                    minMonitorUnitsPerDegree.Threshold,
                    "MU/deg");
            }

            if (profile.MaxDcaStepSizeDegrees.HasValue && IsDca(beam) && segment.DeltaGantryDegrees.HasValue)
            {
                var passes = segment.DeltaGantryDegrees.Value <= profile.MaxDcaStepSizeDegrees.Value;
                yield return new DeliverabilityCheckResult(
                    "deliverability.dca.max-step",
                    "Maximum DCA step size",
                    passes ? DeliverabilityStatus.Pass : DeliverabilityStatus.Fail,
                    passes
                        ? $"Beam '{beam.Id}' DCA step size is within profile limits."
                        : $"Beam '{beam.Id}' DCA step size exceeds the configured maximum.",
                    beam.Id,
                    segment.EndControlPointIndex,
                    segment.DeltaGantryDegrees,
                    profile.MaxDcaStepSizeDegrees,
                    "deg");
            }
        }
    }

    private static IEnumerable<DeliverabilityCheckResult> CheckFieldSize(Beam beam, MachineConstraintProfile profile)
    {
        var limit = beam.IsFlatteningFilterFree
            ? profile.MaxFffFieldSizeCm ?? profile.MaxMlcFieldSizeCm ?? profile.MaxOpenFieldSizeCm
            : profile.MaxMlcFieldSizeCm ?? profile.MaxOpenFieldSizeCm;
        if (!limit.HasValue && !profile.MinJawOpeningCm.HasValue)
        {
            yield break;
        }

        var controlPointsWithJaws = beam.ControlPoints.Where(controlPoint => controlPoint.JawPositions is not null).ToArray();
        if (controlPointsWithJaws.Length == 0)
        {
            yield return new DeliverabilityCheckResult(
                "deliverability.field-size",
                "Maximum field size",
                DeliverabilityStatus.NotEvaluable,
                $"Beam '{beam.Id}' has no jaw geometry.",
                beam.Id,
                expectedValue: limit ?? profile.MinJawOpeningCm,
                unit: "cm");
            yield break;
        }

        foreach (var controlPoint in controlPointsWithJaws)
        {
            var jawPositions = controlPoint.JawPositions!;
            if (limit.HasValue)
            {
                var observed = jawPositions.LargestDimensionCm;
                var passes = observed <= limit.Value;
                yield return new DeliverabilityCheckResult(
                    "deliverability.field-size",
                    "Maximum field size",
                    passes ? DeliverabilityStatus.Pass : DeliverabilityStatus.Fail,
                    passes
                        ? $"Beam '{beam.Id}' field size is within profile limits."
                        : $"Beam '{beam.Id}' field size exceeds the configured maximum.",
                    beam.Id,
                    controlPoint.Index,
                    observed,
                    limit,
                    "cm");
            }

            if (profile.MinJawOpeningCm.HasValue)
            {
                var minimumOpening = Math.Min(jawPositions.WidthCm, jawPositions.LengthCm);
                var minJawPasses = minimumOpening >= profile.MinJawOpeningCm.Value;
                yield return new DeliverabilityCheckResult(
                    "deliverability.jaw.min-opening",
                    "Minimum jaw opening",
                    minJawPasses ? DeliverabilityStatus.Pass : DeliverabilityStatus.Fail,
                    minJawPasses
                        ? $"Beam '{beam.Id}' jaw opening is above the configured minimum."
                        : $"Beam '{beam.Id}' jaw opening is below the configured minimum.",
                    beam.Id,
                    controlPoint.Index,
                    minimumOpening,
                    profile.MinJawOpeningCm,
                    "cm");
            }
        }
    }

    private static DeliverabilityCheckResult CheckTextValue(
        string checkId,
        string title,
        string subject,
        string? observed,
        string expected,
        string? beamId = null)
    {
        if (string.IsNullOrWhiteSpace(observed))
        {
            return new DeliverabilityCheckResult(
                checkId,
                title,
                DeliverabilityStatus.NotEvaluable,
                $"{subject} is missing.",
                beamId);
        }

        var passes = string.Equals(observed, expected, StringComparison.OrdinalIgnoreCase);
        return new DeliverabilityCheckResult(
            checkId,
            title,
            passes ? DeliverabilityStatus.Pass : DeliverabilityStatus.Fail,
            passes ? $"{subject} matches profile." : $"{subject} does not match profile.",
            beamId);
    }

    private static DeliverabilityCheckResult CheckAllowedValue(
        string checkId,
        string title,
        string subject,
        string? observed,
        IReadOnlyList<string> allowed,
        string? beamId = null)
    {
        if (string.IsNullOrWhiteSpace(observed))
        {
            return new DeliverabilityCheckResult(
                checkId,
                title,
                DeliverabilityStatus.NotEvaluable,
                $"{subject} is missing.",
                beamId);
        }

        var passes = allowed.Contains(observed, StringComparer.OrdinalIgnoreCase);
        return new DeliverabilityCheckResult(
            checkId,
            title,
            passes ? DeliverabilityStatus.Pass : DeliverabilityStatus.Fail,
            passes ? $"{subject} is allowed by profile." : $"{subject} is not allowed by profile.",
            beamId);
    }

    private static MuPerDegreeResolution ResolveMinMonitorUnitsPerDegree(Plan plan, Beam beam, MachineConstraintProfile profile)
    {
        if (profile.MonitorUnitsPerDegreeConstraints.Count == 0)
        {
            return new MuPerDegreeResolution(profile.MinMonitorUnitsPerDegree, false);
        }

        var match = profile.MonitorUnitsPerDegreeConstraints
            .Where(constraint => Matches(constraint.MachineId, beam.TreatmentUnitId)
                && Matches(constraint.Energy, beam.Energy)
                && Matches(constraint.TechniqueId, beam.TechniqueId)
                && Matches(constraint.DiseaseSite, plan.DiseaseSite))
            .OrderByDescending(constraint => constraint.Specificity)
            .FirstOrDefault();

        return match is null
            ? new MuPerDegreeResolution(null, true)
            : new MuPerDegreeResolution(match.MinMonitorUnitsPerDegree, false);
    }

    private static bool Matches(string? selector, string? value)
    {
        return string.IsNullOrWhiteSpace(selector)
            || string.Equals(selector, value, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> AllowedValues(IReadOnlyList<string> values, string? singleValue)
    {
        return string.IsNullOrWhiteSpace(singleValue)
            ? values
            : values.Concat(new[] { singleValue }).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static bool IsDca(Beam beam)
    {
        return (beam.TechniqueId?.Contains("DCA", StringComparison.OrdinalIgnoreCase) ?? false)
            || beam.Modality.Contains("DCA", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record MuPerDegreeResolution(decimal? Threshold, bool NoMatchingConstraint);
}
