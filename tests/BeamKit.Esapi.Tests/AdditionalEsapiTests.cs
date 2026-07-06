using BeamKit.Core.Domain;
using BeamKit.Esapi;
using Xunit;

namespace BeamKit.Esapi.Tests;

public sealed class AdditionalEsapiTests
{
    [Fact]
    public void ConverterLeavesDoseNullWhenDoseGridIsAbsent()
    {
        var snapshot = CreateSnapshot(doseGrid: null);

        var plan = new EsapiPlanConverter().Convert(snapshot);

        Assert.Null(plan.Dose);
        Assert.Empty(plan.Beams);
    }

    [Fact]
    public void SnapshotTrimsOptionalLabels()
    {
        var snapshot = CreateSnapshot(patientDisplayName: "  Synthetic  ", diseaseSite: "  Lung  ");

        Assert.Equal("Synthetic", snapshot.PatientDisplayName);
        Assert.Equal("Lung", snapshot.DiseaseSite);
    }

    [Fact]
    public void SnapshotRejectsNullStructures()
    {
        Assert.Throws<ArgumentNullException>(() => new EsapiPlanSnapshot(
            "P1",
            null,
            "C1",
            "Plan",
            new EsapiPrescriptionSnapshot(50m, 25, "PTV", true),
            null!));
    }

    [Fact]
    public void ConverterPreservesDoseStatistics()
    {
        var snapshot = CreateSnapshot(
            doseGrid: new EsapiDoseGridSnapshot(2m, 2m, 2m),
            doseStatistics: new[] { new EsapiDoseStatisticsSnapshot("PTV", new Dictionary<string, decimal> { [DoseMetricKeys.MaximumDoseGy] = 52m }) });

        var plan = new EsapiPlanConverter().Convert(snapshot);

        Assert.Equal(52m, plan.Dose?.FindStatistics("PTV")?.GetMetric("maxdosegy"));
    }

    [Fact]
    public void ConverterRejectsNullSnapshot()
    {
        Assert.Throws<ArgumentNullException>(() => new EsapiPlanConverter().Convert(null!));
    }

    private static EsapiPlanSnapshot CreateSnapshot(
        string? patientDisplayName = null,
        EsapiDoseGridSnapshot? doseGrid = null,
        IEnumerable<EsapiDoseStatisticsSnapshot>? doseStatistics = null,
        string? diseaseSite = null)
    {
        return new EsapiPlanSnapshot(
            "P1",
            patientDisplayName,
            "C1",
            "Plan",
            new EsapiPrescriptionSnapshot(50m, 25, "PTV", true),
            new[] { new EsapiStructureSnapshot("PTV", "PTV", StructureType.Target, 100m, true) },
            doseGrid,
            doseStatistics,
            diseaseSite: diseaseSite);
    }
}
