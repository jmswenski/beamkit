using BeamKit.Check;
using BeamKit.Deliverability;
using BeamKit.PlanCheck;
using BeamKit.Samples;
using BeamKit.Templates;
using BeamKit.Workflow;
using Xunit;

namespace BeamKit.Sdk.Tests;

public sealed class BeamKitClientTests
{
    private static readonly DateOnly AssignmentDate = new(2026, 7, 8);

    [Fact]
    public void CheckPlanRunsFlagshipCheckWorkflow()
    {
        var client = new BeamKitClient();

        var report = client.CheckPlan(SyntheticClinicalCaseLibrary.HeadAndNeckBaseline().Plan, CreateRulePack());

        Assert.Equal(BeamKitCheckStatus.Pass, report.Status);
        Assert.Equal("HN-SYN-001", report.PlanId);
    }

    [Fact]
    public void ValidateRulePackReturnsPolicyReport()
    {
        var client = new BeamKitClient();

        var report = client.ValidateRulePack(CreateRulePack());

        Assert.True(report.IsValid);
        Assert.StartsWith("sha256:", report.Fingerprint, StringComparison.Ordinal);
    }

    [Fact]
    public void TestRulePackRunsRegressionSuite()
    {
        var client = new BeamKitClient();
        var clinicalCase = SyntheticClinicalCaseLibrary.HeadAndNeckCordFailure();

        var report = client.TestRulePack(
            CreateRulePack(),
            new[]
            {
                new RulePackTestCase(
                    clinicalCase.Id,
                    clinicalCase.Description,
                    clinicalCase.Plan,
                    BeamKitCheckStatus.Fail,
                    new[] { "cord.max" })
            });

        Assert.True(report.Passed);
        Assert.Equal(1, report.PassedCount);
    }

    [Fact]
    public void RunCiGateReturnsProvenanceRecord()
    {
        var client = new BeamKitClient();

        var record = client.RunCiGate(new BeamKitCiRunRequest(
            SyntheticClinicalCaseLibrary.HeadAndNeckBaseline().Plan,
            CreateRulePack(),
            inputSource: "case:head-neck-pass",
            branch: "main",
            commit: "abc123",
            buildId: "build-1"));

        Assert.Equal(0, record.ExitCode);
        Assert.StartsWith("sha256:", record.Provenance.PlanFingerprint, StringComparison.Ordinal);
        Assert.Equal("main", record.Provenance.Branch);
    }

    [Fact]
    public void RecommendPlannerReturnsRankedAssignment()
    {
        var client = new BeamKitClient();
        var request = new PlannerAssignmentRequest(
            "case-1",
            "Head and Neck",
            AssignmentDate.AddDays(2),
            new[]
            {
                new PlannerProfile("low", "Low Match", new[] { "3D" }, activeCaseCount: 1, maxActiveCaseCount: 8),
                new PlannerProfile("high", "High Match", new[] { "VMAT", "Head and Neck" }, new[] { "Head and Neck" }, 2, 8)
            },
            new[] { "VMAT" },
            assignmentDate: AssignmentDate);

        var recommendation = client.RecommendPlanner(request);

        Assert.Equal("high", recommendation.RecommendedPlanner?.Planner.Id);
        Assert.Equal(2, recommendation.Candidates.Count);
    }

    [Fact]
    public void RecommendPlanningTeamReturnsRoleRecommendations()
    {
        var client = new BeamKitClient();
        var request = new PlannerAssignmentRequest(
            "case-1",
            "Lung",
            AssignmentDate.AddDays(2),
            new[]
            {
                new PlannerProfile("dosimetrist", "Dosimetrist", new[] { "VMAT", "SBRT", "Lung" }, new[] { "Lung" }, 1, 8, role: PlanningStaffRole.Dosimetrist),
                new PlannerProfile("physicist", "Physicist", new[] { "VMAT", "SBRT" }, new[] { "Lung" }, 2, 8, role: PlanningStaffRole.Physicist)
            },
            new[] { "SBRT" },
            complexityScore: 4,
            assignmentDate: AssignmentDate,
            requiredRoles: new[] { PlanningStaffRole.Dosimetrist, PlanningStaffRole.Physicist });

        var recommendation = client.RecommendPlanningTeam(request);

        Assert.True(recommendation.IsFullyStaffed);
        Assert.Equal(2, recommendation.RoleRecommendations.Count);
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
            query,
            owner: "BeamKit",
            description: "Synthetic default rule pack for SDK tests.",
            diseaseSite: "Head and Neck",
            tags: new[] { "synthetic", "sdk", "test" });
    }
}
