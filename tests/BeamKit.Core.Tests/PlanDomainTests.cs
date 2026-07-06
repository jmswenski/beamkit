using BeamKit.Core.Domain;
using BeamKit.Samples;
using Xunit;

namespace BeamKit.Core.Tests;

public sealed class PlanDomainTests
{
    [Fact]
    public void FindsStructureByIdOrNameIgnoringCase()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();

        Assert.Equal("PTV_7000", plan.FindStructure("ptv_7000")?.Id);
        Assert.Equal("LUNG_R", plan.FindStructure("lung_r")?.Id);
    }

    [Fact]
    public void DoseGridReportsLargestSpacing()
    {
        var grid = new DoseGrid(2.5m, 3m, 1.25m);

        Assert.Equal(3m, grid.MaxSpacingMm);
    }

    [Fact]
    public void PrescriptionReportsDosePerFraction()
    {
        var prescription = new Prescription(70m, 35, "PTV_7000");

        Assert.Equal(2m, prescription.DosePerFractionGy);
    }
}
