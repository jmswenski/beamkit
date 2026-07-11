using BeamKit.ChangeDetection;
using BeamKit.Core.Domain;
using BeamKit.Samples;

namespace BeamKit.Release.Tests;

public sealed class PlanFingerprintTests
{
    [Fact]
    public void PlanFingerprintIsStableWhenCollectionsAreReordered()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var reordered = plan with
        {
            Structures = plan.Structures.Reverse().ToArray(),
            Beams = plan.Beams.Reverse().ToArray(),
            ClinicalGoals = plan.ClinicalGoals.Reverse().ToArray(),
            Dose = plan.Dose! with
            {
                Statistics = plan.Dose.Statistics.Reverse().ToArray()
            }
        };

        Assert.Equal(PlanFingerprint.Compute(plan), PlanFingerprint.Compute(reordered));
    }

    [Fact]
    public void PlanFingerprintChangesWhenReleaseRelevantFieldsChange()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var changed = plan with
        {
            Prescription = plan.Prescription with { TotalDoseGy = plan.Prescription.TotalDoseGy + 1m }
        };

        Assert.NotEqual(PlanFingerprint.Compute(plan), PlanFingerprint.Compute(changed));
    }

    [Fact]
    public void PrescriptionFingerprintIgnoresPlanOnlyFields()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var changedPlanId = plan with { Id = "HN-SYN-002" };

        Assert.Equal(
            PlanFingerprint.Compute(plan.Prescription),
            PlanFingerprint.Compute(changedPlanId.Prescription));
    }

    [Fact]
    public void FingerprintHasSha256PrefixAndHexPayload()
    {
        var fingerprint = PlanFingerprint.Compute(SyntheticPlanFactory.CreateHeadAndNeckPlan());

        Assert.StartsWith("sha256:", fingerprint, StringComparison.Ordinal);
        Assert.Equal(71, fingerprint.Length);
        Assert.All(fingerprint["sha256:".Length..], character =>
            Assert.Contains(character.ToString(), "0123456789abcdef", StringComparison.Ordinal));
    }

    [Fact]
    public void FingerprintChangesWhenControlPointGeometryChanges()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var beam = plan.Beams.First(beam => beam.ControlPoints.Count > 0);
        var controlPoint = beam.ControlPoints[0];
        var changedBeam = beam with
        {
            ControlPoints = beam.ControlPoints
                .Select(item => item.Index == controlPoint.Index
                    ? item with { JawPositions = new BeamJawPositions(-5m, 5.5m, -6m, 6m) }
                    : item)
                .ToArray()
        };
        var changedPlan = plan with
        {
            Beams = plan.Beams.Select(item => item.Id == beam.Id ? changedBeam : item).ToArray()
        };

        Assert.NotEqual(PlanFingerprint.Compute(plan), PlanFingerprint.Compute(changedPlan));
    }
}
