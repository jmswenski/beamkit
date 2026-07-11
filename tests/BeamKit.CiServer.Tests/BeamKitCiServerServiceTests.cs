using BeamKit.Check;
using BeamKit.Sdk;
using Xunit;

namespace BeamKit.CiServer.Tests;

public sealed class BeamKitCiServerServiceTests
{
    [Fact]
    public void ListCasesReturnsSyntheticCaseLibrary()
    {
        var service = CreateService();

        var cases = service.ListCases();

        Assert.True(cases.Count >= 6);
        Assert.Contains(cases, clinicalCase => clinicalCase.Id == "head-neck-pass");
        Assert.Contains(cases, clinicalCase => clinicalCase.Id == "head-neck-cord-fail" && !clinicalCase.ExpectedToPass);
    }

    [Fact]
    public void CreateRunStoresPassingCiArtifact()
    {
        var store = new CiRunStore();
        var service = CreateService(store);

        var record = service.CreateRun(new HostedCiRunRequest
        {
            SyntheticCaseId = "head-neck-pass",
            Branch = "main",
            Commit = "abc123",
            BuildId = "build-1"
        });

        Assert.Equal("head-neck-pass", record.SyntheticCaseId);
        Assert.Equal(BeamKitCheckStatus.Pass, record.Status);
        Assert.Equal(0, record.ExitCode);
        Assert.Equal("main", record.Artifact.Provenance.Branch);
        var summary = store.Find(record.Id);
        Assert.NotNull(summary);
        Assert.Equal(record.Id, summary.Id);
        Assert.Equal("main", summary.Branch);
        var artifactJson = store.FindArtifactJson(record.Id) ?? throw new InvalidOperationException("Artifact JSON was not stored.");
        Assert.Contains("planFingerprint", artifactJson, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateRunStoresFailingCiArtifact()
    {
        var service = CreateService();

        var record = service.CreateRun(new HostedCiRunRequest { SyntheticCaseId = "head-neck-cord-fail" });

        Assert.Equal(BeamKitCheckStatus.Fail, record.Status);
        Assert.Equal(2, record.ExitCode);
        Assert.True(record.Artifact.CheckReport.HasBlockingIssues);
    }

    [Fact]
    public void ValidateRulePackReturnsCleanPolicyReport()
    {
        var service = CreateService();

        var report = service.ValidateRulePack(new RulePackValidationServerRequest());

        Assert.True(report.IsValid);
        Assert.Equal(0, report.ErrorCount);
        Assert.StartsWith("sha256:", report.Fingerprint, StringComparison.Ordinal);
    }

    [Fact]
    public void TestRulePackRunsDefaultRegressionSuite()
    {
        var service = CreateService();

        var report = service.TestRulePack(new RulePackTestServerRequest());

        Assert.True(report.Passed);
        Assert.Equal(3, report.PassedCount);
    }

    [Fact]
    public void TestRulePackCanRunSingleSyntheticCase()
    {
        var service = CreateService();

        var report = service.TestRulePack(new RulePackTestServerRequest { SyntheticCaseId = "head-neck-cord-fail" });

        var result = Assert.Single(report.Results);
        Assert.True(result.Passed);
        Assert.Equal(BeamKitCheckStatus.Fail, result.ActualStatus);
    }

    [Fact]
    public void RecommendAssignmentReturnsRankedPlanner()
    {
        var service = CreateService();

        var recommendation = service.RecommendAssignment(new AssignmentServerRequest
        {
            DiseaseSite = "Head and Neck",
            RequiredSkills = new[] { "VMAT" },
            DueDate = "2026-07-12",
            ComplexityScore = 4,
            Priority = 4
        });

        Assert.Equal("planner-jane", recommendation.RecommendedPlanner?.Planner.Id);
        Assert.Contains(recommendation.RecommendedPlanner!.Reasons, reason => reason.Contains("All required skills", StringComparison.Ordinal));
    }

    private static BeamKitCiServerService CreateService(ICiRunStore? store = null)
    {
        return new BeamKitCiServerService(new BeamKitClient(), store ?? new CiRunStore(), new FixedTimeProvider());
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        }
    }
}
