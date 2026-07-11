using BeamKit.Core.Domain;
using BeamKit.Esapi;
using Xunit;

namespace BeamKit.Esapi.Tests;

public sealed class EsapiPlanSnapshotJsonTests
{
    [Fact]
    public void SnapshotRoundTripsThroughJson()
    {
        var snapshot = CreateSnapshot();

        var roundTripped = EsapiPlanSnapshotJson.FromJson(EsapiPlanSnapshotJson.ToJson(snapshot));

        Assert.Equal(snapshot.PatientId, roundTripped.PatientId);
        Assert.Equal(snapshot.PlanId, roundTripped.PlanId);
        Assert.Equal(snapshot.Structures.Single().Type, roundTripped.Structures.Single().Type);
        var controlPoints = Assert.IsAssignableFrom<IReadOnlyList<EsapiBeamControlPointSnapshot>>(roundTripped.Beams.Single().ControlPoints);
        Assert.Equal(snapshot.Beams.Single().ControlPoints?.Single().JawPositions?.X2Cm, controlPoints.Single().JawPositions?.X2Cm);
    }

    [Fact]
    public void JsonSnapshotCanConvertToCorePlan()
    {
        var json = EsapiPlanSnapshotJson.ToJson(CreateSnapshot());

        var plan = new EsapiPlanConverter().Convert(EsapiPlanSnapshotJson.FromJson(json));

        Assert.Equal("PlanA", plan.Id);
        Assert.Equal(50m, plan.Prescription.TotalDoseGy);
        Assert.Equal(52m, plan.Dose?.FindStatistics("PTV")?.GetMetric(DoseMetricKeys.MaximumDoseGy));
        Assert.Equal("SYN-6X", plan.Beams.Single().BeamModelId);
    }

    private static EsapiPlanSnapshot CreateSnapshot()
    {
        return new EsapiPlanSnapshot(
            "PatientA",
            "Synthetic Patient",
            "CourseA",
            "PlanA",
            new EsapiPrescriptionSnapshot(50m, 25, "PTV", true, RequestedEnergy: "6X", RequestedTechniqueId: "VMAT"),
            new[] { new EsapiStructureSnapshot("PTV", "PTV", StructureType.Target, 100m, true) },
            new EsapiDoseGridSnapshot(2.5m, 2.5m, 2.5m, "AAA", "16.1"),
            new[] { new EsapiDoseStatisticsSnapshot("PTV", new Dictionary<string, decimal> { [DoseMetricKeys.MaximumDoseGy] = 52m }) },
            new[]
            {
                new EsapiBeamSnapshot(
                    "B1",
                    "Arc 1",
                    "Photon",
                    "6X",
                    MonitorUnits: 350m,
                    TreatmentUnitId: "TB1",
                    TechniqueId: "VMAT",
                    ControlPoints: new[]
                    {
                        new EsapiBeamControlPointSnapshot(0, 179m, 0m, new EsapiBeamJawPositionsSnapshot(-5m, 5m, -6m, 6m))
                    },
                    BeamModelId: "SYN-6X",
                    JawTrackingEnabled: true)
            },
            "Head and Neck");
    }
}
