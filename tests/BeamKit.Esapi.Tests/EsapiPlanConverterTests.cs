using BeamKit.Core.Domain;
using BeamKit.Esapi;
using Xunit;

namespace BeamKit.Esapi.Tests;

public sealed class EsapiPlanConverterTests
{
    [Fact]
    public void ConvertsSnapshotToBeamKitPlan()
    {
        var snapshot = new EsapiPlanSnapshot(
            "P1",
            "Synthetic Patient",
            "C1",
            "PlanA",
            new EsapiPrescriptionSnapshot(70m, 35, "PTV_7000", true, "Definitive"),
            new[]
            {
                new EsapiStructureSnapshot("PTV_7000", "PTV_7000", StructureType.Target, 100m, true)
            },
            new EsapiDoseGridSnapshot(2.5m, 2.5m, 2.5m),
            new[]
            {
                new EsapiDoseStatisticsSnapshot(
                    "PTV_7000",
                    new Dictionary<string, decimal>
                    {
                        [DoseMetricKeys.MaximumDoseGy] = 74m
                    })
            },
            new[]
            {
                new EsapiBeamSnapshot("B1", "Arc 1", "Photon VMAT", "6X", MonitorUnits: 400m)
            },
            "Head and Neck");

        var plan = new EsapiPlanConverter().Convert(snapshot);

        Assert.Equal("PlanA", plan.Id);
        Assert.Equal("P1", plan.Patient.Id);
        Assert.Equal(70m, plan.Prescription.TotalDoseGy);
        Assert.Single(plan.Structures);
        Assert.NotNull(plan.Dose);
        Assert.Single(plan.Beams);
    }
}
