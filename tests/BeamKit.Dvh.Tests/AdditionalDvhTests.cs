using BeamKit.Dvh;
using Xunit;

namespace BeamKit.Dvh.Tests;

public sealed class AdditionalDvhTests
{
    [Fact]
    public void CurveOrdersPointsByDose()
    {
        var curve = new DvhCurve(
            "PTV",
            new[]
            {
                new DvhPoint(50m, 40m),
                new DvhPoint(0m, 100m),
                new DvhPoint(25m, 75m)
            });

        Assert.Equal(new[] { 0m, 25m, 50m }, curve.Points.Select(point => point.DoseGy));
    }

    [Fact]
    public void CurveRejectsEmptyPointSet()
    {
        Assert.Throws<ArgumentException>(() => new DvhCurve("PTV", Array.Empty<DvhPoint>()));
    }

    [Theory]
    [InlineData(-0.1, 50)]
    [InlineData(1, -0.1)]
    [InlineData(1, 100.1)]
    public void DvhPointRejectsInvalidCoordinates(decimal doseGy, decimal volumePercent)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DvhPoint(doseGy, volumePercent));
    }

    [Theory]
    [InlineData(100, 0)]
    [InlineData(0, 100)]
    public void DoseAtVolumeReturnsBoundaryDoses(decimal volumePercent, decimal expectedDoseGy)
    {
        var curve = CreateSimpleCurve();

        var doseGy = new DvhMetricCalculator().DoseAtVolumePercent(curve, volumePercent);

        Assert.Equal(expectedDoseGy, doseGy);
    }

    private static DvhCurve CreateSimpleCurve()
    {
        return new DvhCurve(
            "PTV",
            new[]
            {
                new DvhPoint(0m, 100m),
                new DvhPoint(50m, 50m),
                new DvhPoint(100m, 0m)
            });
    }
}
