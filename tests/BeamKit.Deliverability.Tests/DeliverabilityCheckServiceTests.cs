using BeamKit.Core.Domain;
using BeamKit.Deliverability;
using BeamKit.Samples;
using Xunit;

namespace BeamKit.Deliverability.Tests;

public sealed class DeliverabilityCheckServiceTests
{
    [Fact]
    public void SyntheticPlanPassesSyntheticProfile()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();

        var results = new DeliverabilityCheckService().Evaluate(plan, MachineConstraintProfile.CreateSynthetic());

        Assert.NotEmpty(results);
        Assert.DoesNotContain(results, result => result.Status == DeliverabilityStatus.Fail);
        Assert.DoesNotContain(results, result => result.Status == DeliverabilityStatus.NotEvaluable);
    }

    [Fact]
    public void LowMuPerDegreeFailsArcCheck()
    {
        var plan = CreatePlanWithBeam(new Beam(
            "B1",
            "Arc",
            "Photon VMAT",
            "6X",
            monitorUnits: 10m,
            techniqueId: "VMAT",
            controlPoints: new[]
            {
                new BeamControlPoint(0, 0m, 0m, new BeamJawPositions(-5m, 5m, -5m, 5m)),
                new BeamControlPoint(1, 180m, 1m, new BeamJawPositions(-5m, 5m, -5m, 5m))
            }));

        var results = new DeliverabilityCheckService().Evaluate(plan, new MachineConstraintProfile("Arc", "1", minMonitorUnitsPerDegree: 0.1m));

        Assert.Contains(results, result => result.CheckId == "deliverability.arc.min-mu-per-degree" && result.Status == DeliverabilityStatus.Fail);
    }

    [Fact]
    public void FffFieldLargerThanProfileFails()
    {
        var plan = CreatePlanWithBeam(new Beam(
            "B1",
            "FFF Arc",
            "Photon VMAT",
            "6FFF",
            monitorUnits: 100m,
            techniqueId: "VMAT",
            controlPoints: new[]
            {
                new BeamControlPoint(0, 0m, 0m, new BeamJawPositions(-8m, 8m, -5m, 5m)),
                new BeamControlPoint(1, 10m, 1m, new BeamJawPositions(-8m, 8m, -5m, 5m))
            }));

        var results = new DeliverabilityCheckService().Evaluate(plan, new MachineConstraintProfile("FFF", "1", maxFffFieldSizeCm: 15m));

        Assert.Contains(results, result => result.CheckId == "deliverability.field-size" && result.Status == DeliverabilityStatus.Fail);
    }

    [Fact]
    public void MissingControlPointWeightsAreNotEvaluable()
    {
        var plan = CreatePlanWithBeam(new Beam(
            "B1",
            "Arc",
            "Photon VMAT",
            "6X",
            monitorUnits: 100m,
            techniqueId: "VMAT",
            controlPoints: new[]
            {
                new BeamControlPoint(0, 0m, jawPositions: new BeamJawPositions(-5m, 5m, -5m, 5m)),
                new BeamControlPoint(1, 10m, jawPositions: new BeamJawPositions(-5m, 5m, -5m, 5m))
            }));

        var results = new DeliverabilityCheckService().Evaluate(plan, new MachineConstraintProfile("Segments", "1", minMonitorUnitsPerSegment: 0.1m));

        Assert.Contains(results, result => result.CheckId == "deliverability.segment.data" && result.Status == DeliverabilityStatus.NotEvaluable);
    }

    [Fact]
    public void BeamModelOutsideProfileFails()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan() with
        {
            Beams = SyntheticPlanFactory.CreateHeadAndNeckPlan().Beams
                .Select(beam => beam with { BeamModelId = "WRONG-MODEL" })
                .ToArray()
        };

        var results = new DeliverabilityCheckService().Evaluate(plan, MachineConstraintProfile.CreateSynthetic());

        Assert.Contains(results, result => result.CheckId == "deliverability.beam.model" && result.Status == DeliverabilityStatus.Fail);
    }

    [Fact]
    public void MinJawOpeningBelowProfileFails()
    {
        var plan = CreatePlanWithBeam(new Beam(
            "B1",
            "Small jaw",
            "Photon VMAT",
            "6X",
            monitorUnits: 100m,
            techniqueId: "VMAT",
            controlPoints: new[]
            {
                new BeamControlPoint(0, 0m, 0m, new BeamJawPositions(-0.1m, 0.1m, -5m, 5m)),
                new BeamControlPoint(1, 10m, 1m, new BeamJawPositions(-0.1m, 0.1m, -5m, 5m))
            }));

        var results = new DeliverabilityCheckService().Evaluate(plan, new MachineConstraintProfile("Jaw", "1", minJawOpeningCm: 0.5m));

        Assert.Contains(results, result => result.CheckId == "deliverability.jaw.min-opening" && result.Status == DeliverabilityStatus.Fail);
    }

    [Fact]
    public void MuPerDegreeConstraintMissIsNotEvaluable()
    {
        var plan = CreatePlanWithBeam(new Beam(
            "B1",
            "Arc",
            "Photon VMAT",
            "6X",
            monitorUnits: 100m,
            treatmentUnitId: "OTHER-LINAC",
            techniqueId: "VMAT",
            controlPoints: new[]
            {
                new BeamControlPoint(0, 0m, 0m, new BeamJawPositions(-5m, 5m, -5m, 5m)),
                new BeamControlPoint(1, 180m, 1m, new BeamJawPositions(-5m, 5m, -5m, 5m))
            }));
        var profile = new MachineConstraintProfile(
            "Keyed",
            "1",
            monitorUnitsPerDegreeConstraints: new[]
            {
                new MonitorUnitsPerDegreeConstraint(0.1m, machineId: "SYN-LINAC", energy: "6X", techniqueId: "VMAT")
            });

        var results = new DeliverabilityCheckService().Evaluate(plan, profile);

        Assert.Contains(results, result => result.CheckId == "deliverability.arc.min-mu-per-degree.profile" && result.Status == DeliverabilityStatus.NotEvaluable);
    }

    private static Plan CreatePlanWithBeam(Beam beam)
    {
        var patient = new Patient("SYN-DELIV", "Synthetic");
        return new Plan(
            "Plan",
            patient,
            "C1",
            new Prescription(10m, 1, "PTV"),
            new[] { new Structure("PTV", "PTV", StructureType.Target, 10m) },
            beams: new[] { beam });
    }
}
