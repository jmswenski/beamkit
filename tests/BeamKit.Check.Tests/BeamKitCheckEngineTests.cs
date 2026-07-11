using BeamKit.Check;
using BeamKit.Deliverability;
using BeamKit.PlanCheck;
using BeamKit.Samples;
using BeamKit.Templates;
using Xunit;

namespace BeamKit.Check.Tests;

public sealed class BeamKitCheckEngineTests
{
    [Fact]
    public void SyntheticHeadAndNeckCasePassesDefaultRulePack()
    {
        var report = new BeamKitCheckEngine().Evaluate(new BeamKitCheckRequest(
            SyntheticClinicalCaseLibrary.HeadAndNeckBaseline().Plan,
            CreateRulePack()));

        Assert.Equal(BeamKitCheckStatus.Pass, report.Status);
        Assert.False(report.HasBlockingIssues);
        Assert.NotNull(report.TargetMetrics);
        Assert.NotNull(report.NamingReport);
    }

    [Fact]
    public void FailingCordCaseProducesBlockingStatus()
    {
        var report = new BeamKitCheckEngine().Evaluate(new BeamKitCheckRequest(
            SyntheticClinicalCaseLibrary.HeadAndNeckCordFailure().Plan,
            CreateRulePack()));

        Assert.Equal(BeamKitCheckStatus.Fail, report.Status);
        Assert.True(report.HasBlockingIssues);
        Assert.Contains(report.PlanCheckReport.Results, result => result.CheckId == "cord.max" && result.Status == PlanCheckStatus.Fail);
        Assert.Contains(report.ClinicalGoalReport.Results, result => result.StructureName == "SpinalCord");
    }

    [Fact]
    public void MissingStructureCaseProducesNamingAndPlanCheckIssues()
    {
        var report = new BeamKitCheckEngine().Evaluate(new BeamKitCheckRequest(
            SyntheticClinicalCaseLibrary.HeadAndNeckMissingStructure().Plan,
            CreateRulePack()));

        Assert.Equal(BeamKitCheckStatus.Fail, report.Status);
        Assert.Contains(report.NamingReport!.MissingStructures, missing => missing.CanonicalName == "Lung_L");
        Assert.Contains(report.PlanCheckReport.Results, result => result.CheckId == "lung.l.v20" && result.Status == PlanCheckStatus.NotEvaluable);
    }

    [Fact]
    public void CaptureWriteUpAddsManifestEvidence()
    {
        var report = new BeamKitCheckEngine().Evaluate(new BeamKitCheckRequest(
            SyntheticClinicalCaseLibrary.HeadAndNeckBaseline().Plan,
            CreateRulePack(),
            captureWriteUpManifest: true));

        Assert.NotNull(report.WriteUpManifest);
        Assert.StartsWith("sha256:", report.WriteUpManifest!.PlanFingerprint, StringComparison.Ordinal);
    }

    [Fact]
    public void HtmlReportContainsFlagshipSections()
    {
        var report = new BeamKitCheckEngine().Evaluate(new BeamKitCheckRequest(
            SyntheticClinicalCaseLibrary.HeadAndNeckBaseline().Plan,
            CreateRulePack()));

        var html = BeamKitCheckReportWriter.Write(report, BeamKitCheckReportFormat.Html);

        Assert.Contains("BeamKit Check Report", html, StringComparison.Ordinal);
        Assert.Contains("Target Metrics", html, StringComparison.Ordinal);
        Assert.Contains("Plan Checks", html, StringComparison.Ordinal);
        Assert.Contains("Clinical Goals", html, StringComparison.Ordinal);
        Assert.Contains("Structure Naming", html, StringComparison.Ordinal);
        Assert.Contains("Readiness", html, StringComparison.Ordinal);
    }

    [Fact]
    public void RulePackLoaderLoadsRepositorySample()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, "samples", "rule-packs", "head-neck-v1", "beamkit-rule-pack.json");

        var rulePack = BeamKitRulePackLoader.FromFile(path);

        Assert.Equal("Synthetic head-and-neck check pack", rulePack.Name);
        Assert.NotNull(rulePack.NamingDictionary);
        Assert.NotNull(rulePack.MachineProfile);
        Assert.Equal("Synthetic clinical rule catalog selected rules", rulePack.ClinicalRuleSet.Name);
    }

    [Fact]
    public void SyntheticCaseLibraryFindsKnownCases()
    {
        var cases = SyntheticClinicalCaseLibrary.All();

        Assert.True(cases.Count >= 6);
        Assert.Equal("head-neck-pass", SyntheticClinicalCaseLibrary.Find("HEAD-NECK-PASS").Id);
    }

    private static BeamKitRulePack CreateRulePack()
    {
        var query = new ClinicalRuleCatalogQuery
        {
            DiseaseSite = "Head and Neck",
            Institution = "Synthetic",
            Tags = new[] { "baseline" }
        };

        return new BeamKitRulePack(
            "Synthetic head-and-neck check pack",
            "2026.1",
            SyntheticClinicalRuleCatalogFactory.CreateHeadAndNeckCatalog().ToRuleSet(query),
            PlanCheckCatalog.CreateSyntheticBaseline(),
            SyntheticStructureNameDictionaryFactory.CreateTg263Subset(),
            MachineConstraintProfile.CreateSynthetic(),
            new RulePackReadinessDefaults
            {
                CtImported = true,
                OptimizationFinished = true,
                PhysicsQaComplete = true,
                PhysicianApprovalComplete = true,
                TreatmentReady = true
            },
            query);
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
