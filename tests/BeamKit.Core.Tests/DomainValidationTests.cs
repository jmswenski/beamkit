using BeamKit.Core.Domain;
using BeamKit.Samples;
using Xunit;

namespace BeamKit.Core.Tests;

public sealed class DomainValidationTests
{
    [Fact]
    public void PatientTrimsOptionalDisplayName()
    {
        var patient = new Patient("P1", "  Synthetic Patient  ");

        Assert.Equal("Synthetic Patient", patient.DisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PatientRequiresIdentifier(string patientId)
    {
        Assert.Throws<ArgumentException>(() => new Patient(patientId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PrescriptionRejectsNonPositiveTotalDose(decimal totalDoseGy)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Prescription(totalDoseGy, 1, "PTV"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-35)]
    public void DoseGridRejectsNonPositiveSpacing(decimal spacingMm)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DoseGrid(spacingMm, 1m, 1m));
    }

    [Fact]
    public void DoseStatisticsLookupIsCaseInsensitive()
    {
        var statistics = new DoseStatistics("PTV", new Dictionary<string, decimal> { ["MeanDoseGy"] = 12.3m });

        Assert.Equal(12.3m, statistics.GetMetric("meandosegy"));
    }

    [Fact]
    public void PlanDefaultsOptionalCollectionsToEmpty()
    {
        var patient = new Patient("P1");
        var plan = new Plan("Plan1", patient, "C1", new Prescription(50m, 25, "PTV"));

        Assert.Empty(plan.Structures);
        Assert.Empty(plan.Beams);
        Assert.Empty(plan.ClinicalGoals);
    }
}
