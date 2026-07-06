using BeamKit.ChangeDetection;
using BeamKit.Core.Domain;
using BeamKit.Samples;
using Xunit;

namespace BeamKit.ChangeDetection.Tests;

public sealed class PlanChangeDetectorTests
{
    [Fact]
    public void DetectsPrescriptionChangeAsBlocking()
    {
        var baseline = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var comparison = baseline with
        {
            Prescription = new Prescription(66m, 33, "PTV_7000", isSigned: true, intent: "Definitive")
        };

        var report = new PlanChangeDetector().Compare(baseline, comparison);

        Assert.True(report.HasBlockingChanges);
        Assert.Contains(report.Changes, change => change.Type == PlanChangeType.PrescriptionChanged && change.Subject == "Prescription.TotalDoseGy");
        Assert.Contains(report.Changes, change => change.Type == PlanChangeType.PrescriptionChanged && change.Subject == "Prescription.FractionCount");
    }

    [Fact]
    public void DetectsStructureDoseAndBeamChanges()
    {
        var baseline = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var comparison = baseline with
        {
            Structures = baseline.Structures
                .Where(structure => structure.Id != "LUNG_L")
                .Select(structure => structure.Id == "CORD" ? structure with { Name = "Cord", VolumeCc = 45m } : structure)
                .Append(new Structure("PAROTID_R", "Parotid_R", StructureType.OrganAtRisk, 22m))
                .ToArray(),
            Dose = baseline.Dose! with
            {
                Grid = new DoseGrid(3m, 3m, 3m),
                Statistics = baseline.Dose.Statistics
                    .Select(statistics => statistics.StructureId == "CORD"
                        ? new DoseStatistics("CORD", new Dictionary<string, decimal> { [DoseMetricKeys.MaximumDoseGy] = 47m })
                        : statistics)
                    .ToArray()
            },
            Beams = baseline.Beams
                .Select(beam => beam.Id == "B1" ? beam with { MonitorUnits = beam.MonitorUnits + 10m } : beam)
                .ToArray()
        };

        var report = new PlanChangeDetector().Compare(baseline, comparison);

        Assert.Contains(report.Changes, change => change.Type == PlanChangeType.StructureRemoved && change.Subject == "LUNG_L");
        Assert.Contains(report.Changes, change => change.Type == PlanChangeType.StructureAdded && change.Subject == "PAROTID_R");
        Assert.Contains(report.Changes, change => change.Type == PlanChangeType.StructureRenamed && change.Subject == "CORD");
        Assert.Contains(report.Changes, change => change.Type == PlanChangeType.DoseGridChanged);
        Assert.Contains(report.Changes, change => change.Type == PlanChangeType.DoseMetricChanged && change.Subject == "CORD.MaxDoseGy");
        Assert.Contains(report.Changes, change => change.Type == PlanChangeType.BeamChanged && change.Subject == "Beam.B1.MonitorUnits");
    }
}
