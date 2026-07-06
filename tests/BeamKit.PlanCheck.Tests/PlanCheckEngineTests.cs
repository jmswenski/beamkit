using BeamKit.Core.Domain;
using BeamKit.Deliverability;
using BeamKit.PlanCheck;
using BeamKit.Samples;
using Xunit;

namespace BeamKit.PlanCheck.Tests;

public sealed class PlanCheckEngineTests
{
    [Fact]
    public void SyntheticBaselinePassesSyntheticPlan()
    {
        var report = new PlanCheckEngine().Evaluate(new PlanCheckRequest(
            SyntheticPlanFactory.CreateHeadAndNeckPlan(),
            PlanCheckCatalog.CreateSyntheticBaseline(),
            MachineConstraintProfile.CreateSynthetic()));

        Assert.False(report.HasBlockingIssues);
        Assert.True(report.PassCount >= 15);
        Assert.Equal(0, report.FailCount);
        Assert.Equal(0, report.NotEvaluableCount);
    }

    [Fact]
    public void MissingBodyFailsStructureCheck()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan() with
        {
            Structures = SyntheticPlanFactory.CreateHeadAndNeckPlan().Structures
                .Where(structure => structure.Id != "BODY")
                .ToArray()
        };

        var report = new PlanCheckEngine().Evaluate(new PlanCheckRequest(
            plan,
            PlanCheckCatalog.CreateSyntheticBaseline(),
            MachineConstraintProfile.CreateSynthetic()));

        Assert.Contains(report.Results, result => result.CheckId == "structure.body.exists" && result.Status == PlanCheckStatus.Fail);
    }

    [Fact]
    public void DoseMetricFailureUsesConfiguredSeverity()
    {
        var catalog = new PlanCheckCatalog(
            "Warnings",
            "1",
            new[]
            {
                new PlanCheckDefinition(
                    "heart.mean.strict",
                    "Heart strict",
                    "dose-metric",
                    PlanCheckSeverity.Warning,
                    parameters: new Dictionary<string, string>
                    {
                        ["structureName"] = "Heart",
                        ["metric"] = "Mean",
                        ["comparison"] = "LessThanOrEqual",
                        ["threshold"] = "1"
                    })
            });

        var report = new PlanCheckEngine().Evaluate(new PlanCheckRequest(SyntheticPlanFactory.CreateHeadAndNeckPlan(), catalog));

        var result = Assert.Single(report.Results);
        Assert.Equal(PlanCheckStatus.Warning, result.Status);
    }

    [Fact]
    public void DeliverabilityRequiresMachineProfile()
    {
        var catalog = new PlanCheckCatalog(
            "Deliverability",
            "1",
            new[] { new PlanCheckDefinition("deliverability", "Deliverability", "deliverability") });

        var report = new PlanCheckEngine().Evaluate(new PlanCheckRequest(SyntheticPlanFactory.CreateHeadAndNeckPlan(), catalog));

        var result = Assert.Single(report.Results);
        Assert.Equal(PlanCheckStatus.NotEvaluable, result.Status);
    }

    [Fact]
    public void PrescriptionEnergyMismatchFails()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan() with
        {
            Beams = SyntheticPlanFactory.CreateHeadAndNeckPlan().Beams
                .Select(beam => beam with { Energy = "10X" })
                .ToArray()
        };
        var catalog = new PlanCheckCatalog(
            "Energy",
            "1",
            new[] { new PlanCheckDefinition("rx.energy", "Rx energy", "prescription-energy") });

        var report = new PlanCheckEngine().Evaluate(new PlanCheckRequest(plan, catalog));

        var result = Assert.Single(report.Results);
        Assert.Equal(PlanCheckStatus.Fail, result.Status);
    }

    [Fact]
    public void PrescriptionTechniqueMissingMetadataIsNotEvaluable()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan() with
        {
            Beams = SyntheticPlanFactory.CreateHeadAndNeckPlan().Beams
                .Select(beam => beam with { TechniqueId = null })
                .ToArray()
        };
        var catalog = new PlanCheckCatalog(
            "Technique",
            "1",
            new[] { new PlanCheckDefinition("rx.technique", "Rx technique", "prescription-technique") });

        var report = new PlanCheckEngine().Evaluate(new PlanCheckRequest(plan, catalog));

        var result = Assert.Single(report.Results);
        Assert.Equal(PlanCheckStatus.NotEvaluable, result.Status);
    }

    [Fact]
    public void CalculationModelMismatchFails()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan() with
        {
            Dose = SyntheticPlanFactory.CreateHeadAndNeckPlan().Dose! with { CalculationModelVersion = "15.6" }
        };
        var catalog = new PlanCheckCatalog(
            "Calculation",
            "1",
            new[] { new PlanCheckDefinition("calc.model", "Calculation", "calculation-model") });

        var report = new PlanCheckEngine().Evaluate(new PlanCheckRequest(plan, catalog, MachineConstraintProfile.CreateSynthetic()));

        var result = Assert.Single(report.Results);
        Assert.Equal(PlanCheckStatus.Fail, result.Status);
    }

    [Fact]
    public void BeamModelMismatchFails()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan() with
        {
            Beams = SyntheticPlanFactory.CreateHeadAndNeckPlan().Beams
                .Select(beam => beam with { BeamModelId = "OTHER-MODEL" })
                .ToArray()
        };
        var catalog = new PlanCheckCatalog(
            "Beam model",
            "1",
            new[] { new PlanCheckDefinition("beam.model", "Beam model", "beam-model") });

        var report = new PlanCheckEngine().Evaluate(new PlanCheckRequest(plan, catalog, MachineConstraintProfile.CreateSynthetic()));

        var result = Assert.Single(report.Results);
        Assert.Equal(PlanCheckStatus.Fail, result.Status);
    }

    [Fact]
    public void CatalogLoaderRejectsDuplicateCheckIds()
    {
        var json = """
            {
              "name": "Duplicate",
              "version": "1",
              "checks": [
                { "id": "check", "title": "A", "type": "dose-exists" },
                { "id": "CHECK", "title": "B", "type": "dose-exists" }
              ]
            }
            """;

        var exception = Assert.Throws<InvalidOperationException>(() => PlanCheckCatalogLoader.FromJson(json));

        Assert.Contains("Duplicate plan-check id", exception.Message);
    }

    [Fact]
    public void RepositoryPlanCheckSamplesLoadAndPassSyntheticPlan()
    {
        var root = FindRepositoryRoot();
        var catalog = PlanCheckCatalogLoader.FromFile(Path.Combine(root, "samples", "plan-check-baseline.json"));
        var profile = MachineConstraintProfile.FromFile(Path.Combine(root, "samples", "machine-profile-synthetic.json"));

        var report = new PlanCheckEngine().Evaluate(new PlanCheckRequest(SyntheticPlanFactory.CreateHeadAndNeckPlan(), catalog, profile));

        Assert.False(report.HasBlockingIssues);
        Assert.Contains(report.Results, result => result.CheckId == "deliverability.profile");
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
