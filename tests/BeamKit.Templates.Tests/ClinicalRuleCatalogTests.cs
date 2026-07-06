using BeamKit.Core.Domain;
using BeamKit.Rules;
using BeamKit.Samples;
using BeamKit.Templates;
using Xunit;

namespace BeamKit.Templates.Tests;

public sealed class ClinicalRuleCatalogTests
{
    [Fact]
    public void LoaderReadsCatalogMetadataAndRuleDetails()
    {
        var json = """
            {
              "name": "Clinic rules",
              "institution": "Example",
              "version": "2026.1",
              "description": "Versioned clinical rule library.",
              "owner": "Physics",
              "tags": [ "head-neck" ],
              "templateSets": [
                {
                  "name": "Head and Neck",
                  "diseaseSite": "Head and Neck",
                  "institution": "Example",
                  "version": "1",
                  "description": "Baseline rules.",
                  "owner": "Dosimetry",
                  "approvedBy": "Physics",
                  "approvedOn": "2026-07-06",
                  "tags": [ "baseline" ],
                  "goals": [
                    {
                      "id": "goal.ptv.d95",
                      "structureName": "PTV_7000",
                      "metricKey": "D95PercentDoseGy",
                      "comparison": "GreaterThanOrEqual",
                      "threshold": 66.5,
                      "unit": "Gy",
                      "severity": "Required",
                      "description": "Coverage rule.",
                      "reference": "Clinic protocol",
                      "rationale": "Documents current practice.",
                      "tags": [ "target" ]
                    }
                  ]
                }
              ]
            }
            """;

        var catalog = ClinicalRuleCatalogLoader.FromJson(json);
        var set = Assert.Single(catalog.TemplateSets);
        var goal = Assert.Single(set.Goals);

        Assert.Equal("Clinic rules", catalog.Name);
        Assert.Equal("Physics", catalog.Owner);
        Assert.Equal("Dosimetry", set.Owner);
        Assert.Equal("Physics", set.ApprovedBy);
        Assert.Equal("Coverage rule.", goal.Description);
        Assert.Equal("Clinic protocol", goal.Reference);
        Assert.Equal("target", Assert.Single(goal.Tags));
    }

    [Fact]
    public void CatalogQuerySelectsGlobalAndPhysicianSpecificRules()
    {
        var catalog = SyntheticClinicalRuleCatalogFactory.CreateHeadAndNeckCatalog();

        var selected = catalog.FindTemplateSets(new ClinicalRuleCatalogQuery
        {
            DiseaseSite = "Head and Neck",
            Physician = "Synthetic Physician"
        });

        Assert.Equal(2, selected.Count);
        Assert.Contains(selected, set => set.Physician is null);
        Assert.Contains(selected, set => set.Physician == "Synthetic Physician");
    }

    [Fact]
    public void CatalogQueryCanFilterByTags()
    {
        var catalog = SyntheticClinicalRuleCatalogFactory.CreateHeadAndNeckCatalog();

        var selected = catalog.FindTemplateSets(new ClinicalRuleCatalogQuery
        {
            Tags = new[] { "physician-addendum" }
        });

        var set = Assert.Single(selected);
        Assert.Equal("Synthetic Physician", set.Physician);
    }

    [Fact]
    public void ToRuleSetOmitsInactiveGoals()
    {
        var active = new ClinicalGoalTemplate("goal.active", "Heart", DoseMetricKeys.MeanDoseGy, GoalComparison.LessThanOrEqual, 10m, "Gy");
        var inactive = new ClinicalGoalTemplate("goal.retired", "Heart", DoseMetricKeys.MeanDoseGy, GoalComparison.LessThanOrEqual, 8m, "Gy", isActive: false);
        var catalog = new ClinicalRuleCatalog("Catalog", new[] { new ClinicalGoalTemplateSet("Set", new[] { active, inactive }) });

        var ruleSet = catalog.ToRuleSet();

        Assert.Single(ruleSet.Rules);
    }

    [Fact]
    public void ToRuleSetRejectsDuplicateActiveGoalIdsAcrossSelectedSets()
    {
        var first = new ClinicalGoalTemplateSet(
            "Set 1",
            new[] { new ClinicalGoalTemplate("goal.duplicate", "Heart", DoseMetricKeys.MeanDoseGy, GoalComparison.LessThanOrEqual, 10m, "Gy") });
        var second = new ClinicalGoalTemplateSet(
            "Set 2",
            new[] { new ClinicalGoalTemplate("goal.duplicate", "Heart", DoseMetricKeys.MeanDoseGy, GoalComparison.LessThanOrEqual, 12m, "Gy") });
        var catalog = new ClinicalRuleCatalog("Catalog", new[] { first, second });

        var exception = Assert.Throws<InvalidOperationException>(() => catalog.ToRuleSet());

        Assert.Contains("Duplicate active clinical goal id", exception.Message);
    }

    [Fact]
    public void LoaderRejectsDuplicateTemplateSetNames()
    {
        var json = """
            {
              "name": "Duplicate",
              "templateSets": [
                {
                  "name": "Rules",
                  "goals": [
                    { "id": "goal.1", "structureName": "Heart", "metricKey": "MeanDoseGy", "comparison": "LessThanOrEqual", "threshold": 10, "unit": "Gy" }
                  ]
                },
                {
                  "name": "rules",
                  "goals": [
                    { "id": "goal.2", "structureName": "Cord", "metricKey": "MaxDoseGy", "comparison": "LessThanOrEqual", "threshold": 45, "unit": "Gy" }
                  ]
                }
              ]
            }
            """;

        var exception = Assert.Throws<InvalidOperationException>(() => ClinicalRuleCatalogLoader.FromJson(json));

        Assert.Contains("Duplicate clinical rule template set name", exception.Message);
    }
}
