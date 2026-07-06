using BeamKit.ChangeDetection;
using BeamKit.Core.Domain;
using BeamKit.Samples;
using Xunit;

namespace BeamKit.ChangeDetection.Tests;

public sealed class AdditionalPlanChangeDetectorTests
{
    [Fact]
    public void IdenticalPlansProduceNoChanges()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();

        var report = new PlanChangeDetector().Compare(plan, plan);

        Assert.Empty(report.Changes);
        Assert.False(report.HasBlockingChanges);
    }

    [Fact]
    public void DoseAddedIsWarning()
    {
        var comparison = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var baseline = comparison with { Dose = null };

        var change = Assert.Single(new PlanChangeDetector().Compare(baseline, comparison).Changes);

        Assert.Equal(PlanChangeType.DoseAdded, change.Type);
        Assert.Equal(PlanChangeSeverity.Warning, change.Severity);
    }

    [Fact]
    public void DoseRemovedIsBlocking()
    {
        var baseline = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var comparison = baseline with { Dose = null };

        var change = Assert.Single(new PlanChangeDetector().Compare(baseline, comparison).Changes);

        Assert.Equal(PlanChangeType.DoseRemoved, change.Type);
        Assert.Equal(PlanChangeSeverity.Blocking, change.Severity);
    }

    [Fact]
    public void DoseMetricAddedAndRemovedAreDetected()
    {
        var baseline = CreatePlanWithMetrics(new Dictionary<string, decimal> { ["A"] = 1m, ["B"] = 2m });
        var comparison = CreatePlanWithMetrics(new Dictionary<string, decimal> { ["A"] = 1m, ["C"] = 3m });

        var report = new PlanChangeDetector().Compare(baseline, comparison);

        Assert.Contains(report.Changes, change => change.Subject == "PTV.B" && change.AfterValue is null);
        Assert.Contains(report.Changes, change => change.Subject == "PTV.C" && change.BeforeValue is null);
    }

    [Fact]
    public void BeamAddedAndRemovedAreDetected()
    {
        var baseline = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var comparison = baseline with
        {
            Beams = baseline.Beams
                .Where(beam => beam.Id != "B1")
                .Append(new Beam("B3", "Arc 3", "Photon VMAT", "6X"))
                .ToArray()
        };

        var report = new PlanChangeDetector().Compare(baseline, comparison);

        Assert.Contains(report.Changes, change => change.Type == PlanChangeType.BeamRemoved && change.Subject == "B1");
        Assert.Contains(report.Changes, change => change.Type == PlanChangeType.BeamAdded && change.Subject == "B3");
    }

    [Fact]
    public void StructureContourStateChangeIsBlocking()
    {
        var baseline = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var comparison = baseline with
        {
            Structures = baseline.Structures
                .Select(structure => structure.Id == "PTV_7000" ? structure with { HasContours = false } : structure)
                .ToArray()
        };

        var report = new PlanChangeDetector().Compare(baseline, comparison);

        Assert.Contains(report.Changes, change =>
            change.Type == PlanChangeType.StructureVolumeChanged
            && change.Subject == "PTV_7000"
            && change.Severity == PlanChangeSeverity.Blocking);
    }

    [Fact]
    public void TolerancesSuppressSmallNumericDifferences()
    {
        var baseline = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var comparison = baseline with
        {
            Prescription = baseline.Prescription with { TotalDoseGy = baseline.Prescription.TotalDoseGy + 0.001m },
            Dose = baseline.Dose! with { Grid = new DoseGrid(2.5001m, 2.5m, 2.5m) },
            Beams = baseline.Beams.Select(beam => beam.Id == "B1" ? beam with { MonitorUnits = beam.MonitorUnits + 0.001m } : beam).ToArray()
        };

        var report = new PlanChangeDetector().Compare(baseline, comparison);

        Assert.Empty(report.Changes);
    }

    [Fact]
    public void DetectsDoseCalculationAndBeamModelMetadataChanges()
    {
        var baseline = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var comparison = baseline with
        {
            Dose = baseline.Dose! with { CalculationModelVersion = "15.6" },
            Beams = baseline.Beams
                .Select(beam => beam.Id == "B1"
                    ? beam with { BeamModelId = "OTHER-MODEL", JawTrackingEnabled = false }
                    : beam)
                .ToArray()
        };

        var report = new PlanChangeDetector().Compare(baseline, comparison);

        Assert.Contains(report.Changes, change => change.Type == PlanChangeType.DoseCalculationChanged && change.Subject == "Dose.CalculationModelVersion");
        Assert.Contains(report.Changes, change => change.Type == PlanChangeType.BeamChanged && change.Subject == "Beam.B1.BeamModelId");
        Assert.Contains(report.Changes, change => change.Type == PlanChangeType.BeamChanged && change.Subject == "Beam.B1.JawTrackingEnabled");
    }

    [Fact]
    public void DetectsControlPointAndJawChanges()
    {
        var baseline = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var comparison = baseline with
        {
            Beams = baseline.Beams.Select(beam =>
            {
                if (beam.Id != "B1")
                {
                    return beam;
                }

                return beam with
                {
                    ControlPoints = beam.ControlPoints.Select(controlPoint => controlPoint.Index == 1
                        ? controlPoint with
                        {
                            CumulativeMetersetWeight = 0.55m,
                            JawPositions = controlPoint.JawPositions! with { X2Cm = controlPoint.JawPositions.X2Cm + 0.2m }
                        }
                        : controlPoint).ToArray()
                };
            }).ToArray()
        };

        var report = new PlanChangeDetector().Compare(baseline, comparison);

        Assert.Contains(report.Changes, change => change.Type == PlanChangeType.BeamControlPointChanged && change.Subject == "Beam.B1.ControlPoint.1.CumulativeMetersetWeight");
        Assert.Contains(report.Changes, change => change.Type == PlanChangeType.BeamControlPointChanged && change.Subject == "Beam.B1.ControlPoint.1.Jaw.X2Cm");
    }

    [Fact]
    public void IntegrityVerifierTreatsAnyQaPlanDifferenceAsBlocking()
    {
        var treatmentPlan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var qaPlan = treatmentPlan with
        {
            Beams = treatmentPlan.Beams
                .Select(beam => beam.Id == "B1" ? beam with { MonitorUnits = beam.MonitorUnits + 1m } : beam)
                .ToArray()
        };

        var report = new PlanIntegrityVerifier().VerifyTreatmentAndQaPlan(treatmentPlan, qaPlan);

        Assert.True(report.HasBlockingChanges);
        Assert.All(report.Changes, change => Assert.Equal(PlanChangeSeverity.Blocking, change.Severity));
        Assert.Contains(report.Changes, change => change.Subject == "Beam.B1.MonitorUnits");
    }

    [Theory]
    [InlineData("baseline")]
    [InlineData("comparison")]
    public void CompareRejectsNullPlan(string nullSide)
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();

        Assert.Throws<ArgumentNullException>(() => nullSide == "baseline"
            ? new PlanChangeDetector().Compare(null!, plan)
            : new PlanChangeDetector().Compare(plan, null!));
    }

    private static Plan CreatePlanWithMetrics(IReadOnlyDictionary<string, decimal> metrics)
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        return plan with
        {
            Dose = new Dose("Dose", new DoseGrid(2m, 2m, 2m), new[] { new DoseStatistics("PTV", metrics) }),
            Structures = new[] { new Structure("PTV", "PTV", StructureType.Target, 100m) }
        };
    }
}
