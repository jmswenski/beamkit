using BeamKit.Core.Domain;
using BeamKit.Metrics;
using Xunit;

namespace BeamKit.Metrics.Tests;

public sealed class MetricExpressionTests
{
    [Theory]
    [InlineData("D95%", DvhMetricKind.DoseAtVolume, 95, "%")]
    [InlineData("D2cc", DvhMetricKind.DoseAtVolume, 2, "cc")]
    [InlineData("V20Gy", DvhMetricKind.VolumeAtDose, 20, "Gy")]
    [InlineData("V3000cGy", DvhMetricKind.VolumeAtDose, 3000, "cGy")]
    public void ParseRecognizesParameterizedMetrics(string text, DvhMetricKind kind, decimal queryValue, string queryUnit)
    {
        var expression = DvhMetricExpression.Parse(text);

        Assert.Equal(kind, expression.Kind);
        Assert.Equal(queryValue, expression.QueryValue);
        Assert.Equal(queryUnit, expression.QueryUnit);
    }

    [Theory]
    [InlineData("Max", DvhMetricKind.MaximumDose)]
    [InlineData("Mean", DvhMetricKind.MeanDose)]
    [InlineData("HI", DvhMetricKind.HomogeneityIndex)]
    [InlineData("R50", DvhMetricKind.R50)]
    public void ParseRecognizesNamedMetrics(string text, DvhMetricKind kind)
    {
        Assert.Equal(kind, DvhMetricExpression.Parse(text).Kind);
    }

    [Fact]
    public void DoseAtVolumePercentMapsToDoseMetricKey()
    {
        var expression = DvhMetricExpression.Parse("D95%");

        Assert.Equal(DoseMetricKeys.DoseAtVolumePercent(95m), expression.ToDoseMetricKey());
    }

    [Fact]
    public void VolumeAtDoseCGyMapsToGyMetricKey()
    {
        var expression = DvhMetricExpression.Parse("V2000cGy");

        Assert.Equal(DoseMetricKeys.VolumeAtDoseGy(20m), expression.ToDoseMetricKey());
    }

    [Fact]
    public void ParseRejectsUnknownMetric()
    {
        Assert.Throws<FormatException>(() => DvhMetricExpression.Parse("BAD"));
    }
}
