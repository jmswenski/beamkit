using BeamKit.Naming;
using BeamKit.Samples;
using BeamKit.Templates;
using BeamKit.Workflow;
using Xunit;

namespace BeamKit.Samples.Tests;

public sealed class SyntheticSampleTests
{
    [Fact]
    public void SyntheticPlanUsesClearlySyntheticIdentifiers()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();

        Assert.StartsWith("SYN-", plan.Patient.Id, StringComparison.Ordinal);
        Assert.Contains("Synthetic", plan.Patient.DisplayName, StringComparison.Ordinal);
        Assert.StartsWith("HN-SYN-", plan.Id, StringComparison.Ordinal);
    }

    [Fact]
    public void SyntheticRuleSetHasStableUniqueRuleIds()
    {
        var ruleSet = SyntheticRuleSetFactory.CreateMilestoneOneRuleSet();

        Assert.Equal(ruleSet.Rules.Count, ruleSet.Rules.Select(rule => rule.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(ruleSet.Rules, rule => rule.Id == "dose.grid.spacing");
    }

    [Fact]
    public void SyntheticTemplateSetProducesExecutableRules()
    {
        var templateSet = SyntheticClinicalGoalTemplateSetFactory.CreateHeadAndNeckBaseline();

        var ruleSet = templateSet.ToRuleSet();

        Assert.Equal(templateSet.Goals.Count, ruleSet.Rules.Count);
        Assert.Equal("Synthetic head and neck baseline", ruleSet.Name);
    }

    [Fact]
    public void SyntheticRuleCatalogContainsBaselineAndPhysicianRules()
    {
        var catalog = SyntheticClinicalRuleCatalogFactory.CreateHeadAndNeckCatalog();

        var selected = catalog.FindTemplateSets(new ClinicalRuleCatalogQuery
        {
            DiseaseSite = "Head and Neck",
            Physician = "Synthetic Physician"
        });

        Assert.Equal(2, selected.Count);
        Assert.Contains(selected.SelectMany(set => set.Goals), goal => goal.Id == "goal.parotid.mean");
    }

    [Fact]
    public void SyntheticDictionaryRequiredStructuresAreCanonical()
    {
        var dictionary = SyntheticStructureNameDictionaryFactory.CreateTg263Subset();

        Assert.All(dictionary.RequiredStructureNames, required =>
            Assert.Contains(dictionary.CanonicalNames, canonical => string.Equals(canonical, required, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void RepositorySampleJsonFilesLoad()
    {
        var root = FindRepositoryRoot();

        var templateSet = ClinicalGoalTemplateLoader.FromFile(Path.Combine(root, "samples", "clinical-goals-head-neck.json"));
        var catalog = ClinicalRuleCatalogLoader.FromFile(Path.Combine(root, "samples", "rule-catalog-head-neck.json"));
        var dictionary = StructureNameDictionaryLoader.FromFile(Path.Combine(root, "samples", "naming-dictionary-head-neck.json"));
        var roster = StaffRosterLoader.FromFile(Path.Combine(root, "samples", "staff-roster-synthetic.json"));

        Assert.NotEmpty(templateSet.Goals);
        Assert.NotEmpty(catalog.TemplateSets);
        Assert.NotEmpty(dictionary.CanonicalNames);
        Assert.Contains(roster.Staff, member => member.Role == PlanningStaffRole.Physicist);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BeamKit.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
