using BeamKit.Core.Domain;
using BeamKit.Metrics;
using BeamKit.Samples;
using Xunit;

namespace BeamKit.Metrics.Tests;

public sealed class PlanQualityMetricServiceTests
{
    [Fact]
    public void EvaluateReadsDoseStatisticForExpression()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();

        var result = new PlanQualityMetricService().Evaluate(plan, "PTV_7000", "D95%");

        Assert.True(result.IsEvaluable);
        Assert.Equal(67.4m, result.Value);
        Assert.Equal("Gy", result.Unit);
    }

    [Fact]
    public void EvaluateReportsNotEvaluableForMissingMetric()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();

        var result = new PlanQualityMetricService().Evaluate(plan, "PTV_7000", "D1%");

        Assert.False(result.IsEvaluable);
        Assert.Contains("was not available", result.Message);
    }

    [Fact]
    public void CalculateTargetMetricsComputesHomogeneityAndConformity()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();

        var metrics = new PlanQualityMetricService().CalculateTargetMetrics(plan);

        Assert.Equal("PTV_7000", metrics.TargetStructureName);
        Assert.Equal(0.094m, metrics.HomogeneityIndex);
        Assert.Equal(0.825m, Math.Round(metrics.ConformityIndex!.Value, 3));
        Assert.Equal(2.741m, Math.Round(metrics.GradientIndex!.Value, 3));
    }

    [Fact]
    public void EvaluateReadsPlanQualityMetricExpression()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();

        var result = new PlanQualityMetricService().Evaluate(plan, "PTV_7000", "CI");

        Assert.True(result.IsEvaluable);
        Assert.Equal(0.825m, Math.Round(result.Value!.Value, 3));
    }

    [Fact]
    public void CalculateTargetMetricsRejectsMissingTargetStatistics()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan() with
        {
            Dose = new Dose("Dose", new DoseGrid(2m, 2m, 2m))
        };

        Assert.Throws<InvalidOperationException>(() => new PlanQualityMetricService().CalculateTargetMetrics(plan));
    }
}
