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

    [Fact]
    public void SnapshotValidatorDetectsMissingTargetAndBeamMetadata()
    {
        var snapshot = new EsapiPlanSnapshot(
            "SYN",
            "Synthetic",
            "C1",
            "P1",
            new EsapiPrescriptionSnapshot(70m, 35, "PTV", IsSigned: false),
            new[] { new EsapiStructureSnapshot("BODY", "Body", StructureType.External, 1000m, true) },
            new EsapiDoseGridSnapshot(2.5m, 2.5m, 2.5m),
            beams: new[] { new EsapiBeamSnapshot("B1", "Beam", "Photon", "6X", MonitorUnits: null) });

        var report = new EsapiSnapshotValidator().Validate(snapshot);

        Assert.Contains(report.Issues, issue => issue.Code == "structures.target-missing" && issue.Severity == EsapiSnapshotIssueSeverity.Error);
        Assert.Contains(report.Issues, issue => issue.Code == "rx.unsigned" && issue.Severity == EsapiSnapshotIssueSeverity.Warning);
        Assert.Contains(report.Issues, issue => issue.Code == "beams.model-missing" && issue.Severity == EsapiSnapshotIssueSeverity.Warning);
        Assert.True(report.HasErrors);
    }

    [Fact]
    public void SnapshotValidatorPassesCompleteSyntheticSnapshot()
    {
        var snapshot = new EsapiPlanSnapshot(
            "SYN",
            "Synthetic",
            "C1",
            "P1",
            new EsapiPrescriptionSnapshot(70m, 35, "PTV", IsSigned: true, RequestedEnergy: "6X", RequestedTechniqueId: "VMAT"),
            new[]
            {
                new EsapiStructureSnapshot("BODY", "Body", StructureType.External, 1000m, true),
                new EsapiStructureSnapshot("PTV", "PTV", StructureType.Target, 100m, true)
            },
            new EsapiDoseGridSnapshot(2.5m, 2.5m, 2.5m, "AAA", "16.1"),
            new[] { new EsapiDoseStatisticsSnapshot("PTV", new Dictionary<string, decimal> { [DoseMetricKeys.DoseAtVolumePercent(95m)] = 67.1m }) },
            new[]
            {
                new EsapiBeamSnapshot(
                    "B1",
                    "Beam",
                    "Photon",
                    "6X",
                    MonitorUnits: 200m,
                    TechniqueId: "VMAT",
                    ControlPoints: new[] { new EsapiBeamControlPointSnapshot(0, 0m, 0m), new EsapiBeamControlPointSnapshot(1, 180m, 1m) },
                    BeamModelId: "MODEL")
            });

        var report = new EsapiSnapshotValidator().Validate(snapshot);

        Assert.Empty(report.Issues);
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
