using System.Text.Json;
using BeamKit.Core.Domain;
using BeamKit.Rules;
using BeamKit.Samples;
using BeamKit.Templates;
using Xunit;

namespace BeamKit.Templates.Tests;

public sealed class AdditionalTemplateTests
{
    [Fact]
    public void LoaderRejectsTemplateSetWithoutGoals()
    {
        var json = """{ "name": "Empty", "goals": [] }""";

        var exception = Assert.Throws<InvalidOperationException>(() => ClinicalGoalTemplateLoader.FromJson(json));

        Assert.Contains("at least one goal", exception.Message);
    }

    [Fact]
    public void LoaderRejectsTemplateSetWithoutName()
    {
        var json = """
            {
              "goals": [
                { "id": "goal.1", "structureName": "Heart", "metricKey": "MeanDoseGy", "comparison": "LessThanOrEqual", "threshold": 10, "unit": "Gy" }
              ]
            }
            """;

        var exception = Assert.Throws<InvalidOperationException>(() => ClinicalGoalTemplateLoader.FromJson(json));

        Assert.Contains("requires a name", exception.Message);
    }

    [Fact]
    public void LoaderRejectsMalformedJson()
    {
        Assert.Throws<JsonException>(() => ClinicalGoalTemplateLoader.FromJson("{"));
    }

    [Fact]
    public void AdvisoryTemplateProducesWarningWhenViolated()
    {
        var templateSet = new ClinicalGoalTemplateSet(
            "Advisory",
            new[] { new ClinicalGoalTemplate("goal", "Heart", DoseMetricKeys.MeanDoseGy, GoalComparison.LessThanOrEqual, 1m, "Gy", GoalSeverity.Advisory) });
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();

        var result = Assert.Single(new RuleEngine().Evaluate(plan, templateSet.ToRuleSet()));

        Assert.Equal(EvaluationStatus.Warning, result.Status);
    }

    [Fact]
    public void ToClinicalGoalsPreservesTemplateFields()
    {
        var template = new ClinicalGoalTemplate(
            "goal",
            "Heart",
            DoseMetricKeys.MeanDoseGy,
            GoalComparison.LessThanOrEqual,
            10m,
            "Gy",
            GoalSeverity.Warning,
            "Mean heart dose goal.",
            "Protocol",
            "Rationale",
            new[] { "heart", "oar" });
        var templateSet = new ClinicalGoalTemplateSet("Set", new[] { template }, "  Cardiac  ", "  Institution  ", "  Physician  ", "  1.0  ");

        var goal = templateSet.ToClinicalGoals().Single();

        Assert.Equal("Cardiac", templateSet.DiseaseSite);
        Assert.Equal("Institution", templateSet.Institution);
        Assert.Equal("Physician", templateSet.Physician);
        Assert.Equal("1.0", templateSet.Version);
        Assert.Equal(template.Id, goal.Id);
        Assert.Equal(GoalSeverity.Warning, goal.Severity);
        Assert.Equal("Mean heart dose goal.", template.Description);
        Assert.Equal("Protocol", template.Reference);
        Assert.Equal(new[] { "heart", "oar" }, template.Tags);
    }

    [Fact]
    public void ToRuleSetOmitsInactiveTemplates()
    {
        var templateSet = new ClinicalGoalTemplateSet(
            "Set",
            new[]
            {
                new ClinicalGoalTemplate("goal.active", "Heart", DoseMetricKeys.MeanDoseGy, GoalComparison.LessThanOrEqual, 10m, "Gy"),
                new ClinicalGoalTemplate("goal.inactive", "Cord", DoseMetricKeys.MaximumDoseGy, GoalComparison.LessThanOrEqual, 45m, "Gy", isActive: false)
            });

        var ruleSet = templateSet.ToRuleSet();

        Assert.Single(ruleSet.Rules);
    }

    [Fact]
    public void FromFileLoadsTemplateJson()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, """
            {
              "name": "File",
              "goals": [
                { "id": "goal.1", "structureName": "Heart", "metricKey": "MeanDoseGy", "comparison": "LessThanOrEqual", "threshold": 10, "unit": "Gy" }
              ]
            }
            """);

        try
        {
            var templateSet = ClinicalGoalTemplateLoader.FromFile(path);

            Assert.Equal("File", templateSet.Name);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FromFileRejectsBlankPath()
    {
        Assert.Throws<ArgumentException>(() => ClinicalGoalTemplateLoader.FromFile(" "));
    }
}
