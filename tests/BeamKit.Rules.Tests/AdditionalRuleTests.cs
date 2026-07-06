using BeamKit.Core.Domain;
using BeamKit.Rules;
using BeamKit.Rules.Rules;
using BeamKit.Samples;
using Xunit;

namespace BeamKit.Rules.Tests;

public sealed class AdditionalRuleTests
{
    [Theory]
    [InlineData("PTV_7000")]
    [InlineData("SpinalCord")]
    public void StructureExistsRulePassesForIdOrName(string structureName)
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var result = new StructureExistsRule(structureName).Evaluate(new PlanEvaluationContext(plan));

        Assert.Equal(EvaluationStatus.Pass, result.Status);
    }

    [Fact]
    public void StructureExistsRuleHonorsCustomMissingStatus()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var result = new StructureExistsRule("Brainstem", EvaluationStatus.Warning).Evaluate(new PlanEvaluationContext(plan));

        Assert.Equal(EvaluationStatus.Warning, result.Status);
    }

    [Fact]
    public void StructureNotEmptyRuleFailsForZeroVolume()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan() with
        {
            Structures = new[] { new Structure("CORD", "SpinalCord", StructureType.OrganAtRisk, 0m) }
        };

        var result = new StructureNotEmptyRule("SpinalCord").Evaluate(new PlanEvaluationContext(plan));

        Assert.Equal(EvaluationStatus.Fail, result.Status);
    }

    [Fact]
    public void StructureNotEmptyRuleIsNotEvaluableWhenStructureIsMissing()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();

        var result = new StructureNotEmptyRule("Brainstem").Evaluate(new PlanEvaluationContext(plan));

        Assert.Equal(EvaluationStatus.NotEvaluable, result.Status);
    }

    [Fact]
    public void DoseGridSpacingRuleFailsWhenGridIsTooCoarse()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var result = new DoseGridSpacingRule(1m).Evaluate(new PlanEvaluationContext(plan));

        Assert.Equal(EvaluationStatus.Fail, result.Status);
        Assert.Equal(2.5m, result.ObservedValue);
    }

    [Fact]
    public void DoseGridSpacingRuleIsNotEvaluableWithoutDose()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan() with { Dose = null };

        var result = new DoseGridSpacingRule(2.5m).Evaluate(new PlanEvaluationContext(plan));

        Assert.Equal(EvaluationStatus.NotEvaluable, result.Status);
    }

    [Theory]
    [InlineData(GoalComparison.LessThan, 11, EvaluationStatus.Pass)]
    [InlineData(GoalComparison.LessThanOrEqual, 10, EvaluationStatus.Pass)]
    [InlineData(GoalComparison.GreaterThan, 9, EvaluationStatus.Pass)]
    [InlineData(GoalComparison.GreaterThanOrEqual, 10, EvaluationStatus.Pass)]
    [InlineData(GoalComparison.Equal, 11, EvaluationStatus.Fail)]
    public void DoseMetricThresholdRuleEvaluatesComparisonVariants(GoalComparison comparison, decimal threshold, EvaluationStatus expectedStatus)
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan() with
        {
            Dose = new Dose(
                "Dose",
                new DoseGrid(2m, 2m, 2m),
                new[] { new DoseStatistics("PTV_7000", new Dictionary<string, decimal> { ["Metric"] = 10m }) })
        };
        var rule = new DoseMetricThresholdRule("rule", "metric rule", "PTV_7000", "Metric", comparison, threshold, "Gy");

        var result = rule.Evaluate(new PlanEvaluationContext(plan));

        Assert.Equal(expectedStatus, result.Status);
    }
}
