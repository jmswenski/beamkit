using BeamKit.Calculations;
using Xunit;

namespace BeamKit.Calculations.Tests;

public sealed class DoseCalculationServiceTests
{
    [Fact]
    public void ConvertsGyAndCGy()
    {
        var dose = DoseValue.FromGy(2.5m);

        Assert.Equal(250m, dose.CGy);
        Assert.Equal(2.5m, dose.ConvertTo(DoseUnit.CGy).Gy);
    }

    [Fact]
    public void CreatesFractionationFromDosePerFraction()
    {
        var scheme = FractionationScheme.FromDosePerFractionGy(2m, 35);

        Assert.Equal(70m, scheme.TotalDoseGy);
        Assert.Equal(200m, scheme.DosePerFractionCGy);
    }

    [Fact]
    public void CalculatesBedAndEqd2ForConventionalFractionation()
    {
        var result = new DoseCalculationService().Calculate(FractionationScheme.FromTotalDoseGy(70m, 35), 10m);

        Assert.Equal(84m, result.BedGy);
        Assert.Equal(70m, result.Eqd2Gy);
    }

    [Fact]
    public void CalculatesHypofractionatedEqd2()
    {
        var result = new DoseCalculationService().Calculate(FractionationScheme.FromTotalDoseGy(60m, 20), 10m);

        Assert.Equal(78m, result.BedGy);
        Assert.Equal(65m, result.Eqd2Gy);
    }

    [Fact]
    public void CalculatesEquivalentTotalDoseForEqd2()
    {
        var totalDoseGy = new DoseCalculationService().CalculateTotalDoseForEqd2Gy(70m, 35, 10m);

        Assert.Equal(70m, Math.Round(totalDoseGy, 3));
    }

    [Fact]
    public void CalculatesEquivalentFractionation()
    {
        var source = FractionationScheme.FromTotalDoseGy(60m, 20);

        var equivalent = new DoseCalculationService().CalculateEquivalentFractionation(source, 30, 10m);

        Assert.Equal(65m, Math.Round(equivalent.TargetEqd2Gy, 3));
        Assert.Equal(30, equivalent.Fractionation.Fractions);
        Assert.Equal(65m, Math.Round(new DoseCalculationService().Calculate(equivalent.Fractionation, 10m).Eqd2Gy, 3));
    }

    [Fact]
    public void CalculatesCumulativeEqd2AcrossCourses()
    {
        var courses = new[]
        {
            FractionationScheme.FromTotalDoseGy(45m, 25, "Initial"),
            FractionationScheme.FromTotalDoseGy(20m, 10, "Boost")
        };

        var result = new DoseCalculationService().CalculateCumulative(courses, 10m);

        Assert.Equal(65m, result.TotalPhysicalDoseGy);
        Assert.Equal(64.25m, Math.Round(result.TotalEqd2Gy, 3));
        Assert.Equal(2, result.Components.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectsNonPositiveAlphaBeta(decimal alphaBetaGy)
    {
        var scheme = FractionationScheme.FromTotalDoseGy(70m, 35);

        Assert.Throws<ArgumentOutOfRangeException>(() => new DoseCalculationService().Calculate(scheme, alphaBetaGy));
    }

    [Fact]
    public void RejectsEmptyCumulativeCourses()
    {
        Assert.Throws<ArgumentException>(() => new DoseCalculationService().CalculateCumulative(Array.Empty<FractionationScheme>(), 10m));
    }

    [Fact]
    public void RejectsNegativeDoseValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DoseValue.FromGy(-1m));
    }
}
