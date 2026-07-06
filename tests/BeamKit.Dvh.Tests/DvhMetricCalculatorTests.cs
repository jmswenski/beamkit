using BeamKit.Core.Domain;
using BeamKit.Dvh;
using Xunit;

namespace BeamKit.Dvh.Tests;

public sealed class DvhMetricCalculatorTests
{
    [Fact]
    public void CalculatesDoseAtVolumeByInterpolation()
    {
        var curve = CreateSimpleCurve();

        var dose = new DvhMetricCalculator().DoseAtVolumePercent(curve, 95m);

        Assert.Equal(5m, dose);
    }

    [Fact]
    public void CalculatesVolumeAtDoseByInterpolation()
    {
        var curve = CreateSimpleCurve();

        var volume = new DvhMetricCalculator().VolumeAtDoseGy(curve, 20m);

        Assert.Equal(80m, volume);
    }

    [Fact]
    public void CreatesDoseStatisticsWithRequestedMetrics()
    {
        var curve = CreateSimpleCurve();

        var statistics = new DvhMetricCalculator().ToDoseStatistics(curve, new[] { 95m }, new[] { 20m });

        Assert.Equal(100m, statistics.GetMetric(DoseMetricKeys.MaximumDoseGy));
        Assert.Equal(5m, statistics.GetMetric(DoseMetricKeys.DoseAtVolumePercent(95m)));
        Assert.Equal(80m, statistics.GetMetric(DoseMetricKeys.VolumeAtDoseGy(20m)));
    }

    private static DvhCurve CreateSimpleCurve()
    {
        return new DvhCurve(
            "PTV_7000",
            new[]
            {
                new DvhPoint(0m, 100m),
                new DvhPoint(50m, 50m),
                new DvhPoint(100m, 0m)
            });
    }
}
