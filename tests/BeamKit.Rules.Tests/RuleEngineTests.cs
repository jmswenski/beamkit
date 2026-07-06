using BeamKit.Core.Domain;
using BeamKit.Rules;
using BeamKit.Rules.Rules;
using BeamKit.Samples;
using System.Globalization;
using Xunit;

namespace BeamKit.Rules.Tests;

public sealed class RuleEngineTests
{
    [Fact]
    public void SyntheticMilestoneOneRuleSetPasses()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var ruleSet = SyntheticRuleSetFactory.CreateMilestoneOneRuleSet();

        var results = new RuleEngine().Evaluate(plan, ruleSet);

        Assert.All(results, result => Assert.Equal(EvaluationStatus.Pass, result.Status));
    }

    [Fact]
    public void MissingStructureRuleFails()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var rule = new StructureExistsRule("Brainstem");

        var result = rule.Evaluate(new PlanEvaluationContext(plan));

        Assert.Equal(EvaluationStatus.Fail, result.Status);
        Assert.Equal("Brainstem", result.StructureName);
    }

    [Fact]
    public void DoseMetricRuleIsNotEvaluableWhenMetricIsMissing()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var rule = new DoseMetricThresholdRule(
            "missing.metric",
            "PTV missing metric",
            "PTV_7000",
            DoseMetricKeys.VolumeAtDoseGy(10m),
            GoalComparison.LessThanOrEqual,
            50m,
            "%");

        var result = rule.Evaluate(new PlanEvaluationContext(plan));

        Assert.Equal(EvaluationStatus.NotEvaluable, result.Status);
    }

    [Fact]
    public void RuleEngineIsolatesRuleExceptionsAsErrors()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var ruleSet = new PlanRuleSet("throwing", new[] { new ThrowingRule() });

        var result = Assert.Single(new RuleEngine().Evaluate(plan, ruleSet));

        Assert.Equal(EvaluationStatus.Error, result.Status);
        Assert.Contains("InvalidOperationException", result.Message);
    }

    [Fact]
    public void PlanRuleSetCanBeBuiltFromClinicalGoals()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var ruleSet = PlanRuleSet.FromClinicalGoals(plan);

        var results = new RuleEngine().Evaluate(plan, ruleSet);

        Assert.Equal(plan.ClinicalGoals.Count, results.Count);
        Assert.All(results, result => Assert.Equal(EvaluationStatus.Pass, result.Status));
    }

    [Fact]
    public void WarningClinicalGoalProducesWarningWhenViolated()
    {
        var goal = new ClinicalGoal(
            "goal.warning",
            "Heart",
            DoseMetricKeys.MeanDoseGy,
            GoalComparison.LessThanOrEqual,
            1m,
            "Gy",
            GoalSeverity.Warning);
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan() with
        {
            ClinicalGoals = new[] { goal }
        };

        var result = Assert.Single(new RuleEngine().Evaluate(plan, PlanRuleSet.FromClinicalGoals(plan)));

        Assert.Equal(EvaluationStatus.Warning, result.Status);
    }

    [Fact]
    public void DvhRuleIdentifiersAreInvariantAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

            var rule = new DvhDoseAtVolumeRule("PTV_7000", 2.5m, GoalComparison.GreaterThanOrEqual, 66.5m);

            Assert.Equal("dvh.d2p5.ptv7000", rule.Id);
            Assert.Contains("D2.5%", rule.Description);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private sealed class ThrowingRule : IPlanRule
    {
        public string Id => "throwing";

        public string Description => "Throws during evaluation";

        public EvaluationResult Evaluate(PlanEvaluationContext context)
        {
            throw new InvalidOperationException("Synthetic failure");
        }
    }
}
