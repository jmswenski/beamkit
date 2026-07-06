using BeamKit.Core.Domain;
using BeamKit.Rules;
using BeamKit.Templates;
using Xunit;

namespace BeamKit.Templates.Tests;

public sealed class ClinicalGoalTemplateLoaderTests
{
    [Fact]
    public void LoadsTemplateSetFromJsonAndCreatesRuleSet()
    {
        var json = """
            {
              "name": "Head and Neck baseline",
              "diseaseSite": "Head and Neck",
              "version": "2026.1",
              "goals": [
                {
                  "id": "goal.ptv.d95",
                  "structureName": "PTV_7000",
                  "metricKey": "D95PercentDoseGy",
                  "comparison": "GreaterThanOrEqual",
                  "threshold": 66.5,
                  "unit": "Gy",
                  "severity": "Required"
                }
              ]
            }
            """;

        var templateSet = ClinicalGoalTemplateLoader.FromJson(json);
        var ruleSet = templateSet.ToRuleSet();

        Assert.Equal("Head and Neck baseline", templateSet.Name);
        Assert.Equal("Head and Neck", templateSet.DiseaseSite);
        Assert.Equal("goal.ptv.d95", templateSet.Goals.Single().Id);
        Assert.Single(ruleSet.Rules);
    }

    [Fact]
    public void RejectsDuplicateGoalIds()
    {
        var json = """
            {
              "name": "Duplicate",
              "goals": [
                { "id": "goal.1", "structureName": "Heart", "metricKey": "MeanDoseGy", "comparison": "LessThanOrEqual", "threshold": 10, "unit": "Gy", "severity": "Required" },
                { "id": "goal.1", "structureName": "Heart", "metricKey": "MeanDoseGy", "comparison": "LessThanOrEqual", "threshold": 12, "unit": "Gy", "severity": "Warning" }
              ]
            }
            """;

        var exception = Assert.Throws<InvalidOperationException>(() => ClinicalGoalTemplateLoader.FromJson(json));

        Assert.Contains("Duplicate clinical goal id", exception.Message);
    }
}
