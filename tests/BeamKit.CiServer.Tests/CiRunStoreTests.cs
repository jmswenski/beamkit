using BeamKit.Check;
using BeamKit.PlanCheck;
using BeamKit.Reporting;
using BeamKit.Workflow;
using Xunit;

namespace BeamKit.CiServer.Tests;

public sealed class CiRunStoreTests
{
    [Fact]
    public void SaveAndFindReturnsStoredRecord()
    {
        var store = new CiRunStore();
        var record = new HostedCiRunRecord(
            "run-1",
            new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero),
            "head-neck-pass",
            CreateArtifact(BeamKitCheckStatus.Pass));

        store.Save(record);

        var summary = store.Find("RUN-1");

        Assert.NotNull(summary);
        Assert.Equal(record.Id, summary.Id);
        Assert.Equal(record.Status, summary.Status);
        var artifactJson = store.FindArtifactJson("RUN-1") ?? throw new InvalidOperationException("Artifact JSON was not stored.");
        Assert.Contains("planFingerprint", artifactJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ListReturnsNewestRunsFirst()
    {
        var store = new CiRunStore();
        store.Save(new HostedCiRunRecord("old", new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero), "head-neck-pass", CreateArtifact(BeamKitCheckStatus.Pass)));
        store.Save(new HostedCiRunRecord("new", new DateTimeOffset(2026, 7, 9, 12, 5, 0, TimeSpan.Zero), "head-neck-cord-fail", CreateArtifact(BeamKitCheckStatus.Fail)));

        var records = store.List();

        Assert.Equal(new[] { "new", "old" }, records.Select(record => record.Id));
    }

    [Fact]
    public void ListClampsLimit()
    {
        var store = new CiRunStore();
        store.Save(new HostedCiRunRecord("run-1", DateTimeOffset.UtcNow, "head-neck-pass", CreateArtifact(BeamKitCheckStatus.Pass)));
        store.Save(new HostedCiRunRecord("run-2", DateTimeOffset.UtcNow.AddMinutes(1), "head-neck-pass", CreateArtifact(BeamKitCheckStatus.Pass)));

        Assert.Single(store.List(0));
    }

    private static BeamKitCiRunRecord CreateArtifact(BeamKitCheckStatus status)
    {
        return new BeamKitCiRunRecord(
            new CheckRunProvenance(
                "artifact-1",
                "plan-1",
                "patient-1",
                "sha256:plan",
                "sha256:rx",
                "Rule pack",
                "1",
                "sha256:pack",
                status,
                DateTimeOffset.UtcNow),
            new RulePackValidationReport("Rule pack", "1", "sha256:pack", Array.Empty<RulePackPolicyIssue>()),
            new BeamKitCheckReport(
                "plan-1",
                "patient-1",
                "course-1",
                DateTimeOffset.UtcNow,
                "Rule pack",
                "1",
                new PlanCheckReport("plan-1", "Catalog", "1", Array.Empty<PlanCheckResult>()),
                new PlanEvaluationReport("plan-1", "patient-1", "Rule set", DateTimeOffset.UtcNow, Array.Empty<BeamKit.Rules.EvaluationResult>()),
                namingReport: null,
                new PlanReadinessState("plan-1", Array.Empty<ReadinessItem>()),
                targetMetrics: null));
    }
}
