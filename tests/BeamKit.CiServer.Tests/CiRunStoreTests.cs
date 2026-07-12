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
            CreateArtifact(BeamKitCheckStatus.Pass),
            planSnapshotJson: """{"patient":{"id":"P1"},"plan":{"id":"plan-1"}}""");

        store.Save(record);

        var summary = store.Find("RUN-1");

        Assert.NotNull(summary);
        Assert.Equal(record.Id, summary.Id);
        Assert.Equal(record.Status, summary.Status);
        var artifactJson = store.FindArtifactJson("RUN-1") ?? throw new InvalidOperationException("Artifact JSON was not stored.");
        Assert.Contains("planFingerprint", artifactJson, StringComparison.Ordinal);
        Assert.True(summary.HasPlanSnapshot);
        Assert.Contains("plan-1", store.FindPlanSnapshotJson("RUN-1"), StringComparison.Ordinal);
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

    [Fact]
    public void SaveBaselineReplacesBaselineForCase()
    {
        var store = new CiRunStore();
        var first = CreateBaseline("case-1", "run-1", DateTimeOffset.UtcNow);
        var second = CreateBaseline("case-1", "run-2", DateTimeOffset.UtcNow.AddMinutes(1));

        store.SaveBaseline(first);
        store.SaveBaseline(second);

        Assert.Equal("run-2", store.FindBaseline("CASE-1")?.BaselineRunId);
        Assert.Single(store.ListBaselines());
    }

    [Fact]
    public void SaveAuditEventStoresQueryableAuditHistory()
    {
        var store = new CiRunStore();
        store.SaveAuditEvent(new CiServerAuditEvent(
            "audit-1",
            new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero),
            "physics",
            "run.created",
            "/api/runs",
            "POST",
            "run-1",
            "case-1",
            "Pass",
            "127.0.0.1",
            "sha256:pack"));
        store.SaveAuditEvent(new CiServerAuditEvent(
            "audit-2",
            new DateTimeOffset(2026, 7, 9, 12, 1, 0, TimeSpan.Zero),
            "physics",
            "baseline.promoted",
            "/api/runs/run-1/baseline",
            "POST",
            "run-1",
            "case-1"));

        var events = store.ListAuditEvents(new CiServerAuditQuery { Action = "run.created" });

        var auditEvent = Assert.Single(events);
        Assert.Equal("physics", auditEvent.Actor);
        Assert.Equal("run-1", auditEvent.RunId);
        Assert.Equal("case-1", auditEvent.CaseId);
    }

    [Fact]
    public void SaveWorkItemStoresQueryableQueueState()
    {
        var store = new CiRunStore();
        store.SaveWorkItem(CreateWorkItem(
            "work-1",
            "case-1",
            CaseWorkItemStatus.Assigned,
            new DateOnly(2026, 7, 12),
            dosimetristId: "planner-jane"));
        store.SaveWorkItem(CreateWorkItem(
            "work-2",
            "case-2",
            CaseWorkItemStatus.Completed,
            new DateOnly(2026, 7, 10),
            dosimetristId: "planner-jane"));

        var active = store.ListWorkItems(new CaseWorkItemQuery
        {
            ActiveOnly = true,
            AssignedStaffId = "PLANNER-JANE"
        });

        var workItem = Assert.Single(active);
        Assert.Equal("work-1", workItem.Id);
        Assert.True(workItem.IsActiveWorkload);
        Assert.Equal("planner-jane", store.FindWorkItem("WORK-1")?.AssignedDosimetristId);
    }

    [Fact]
    public void SaveRulePackVersionStoresQueryableVersionHistory()
    {
        var store = new CiRunStore();
        store.SaveRulePackVersion(CreateRulePackVersion("institution-head-neck", "v1", DateTimeOffset.UtcNow.AddMinutes(-1)));
        store.SaveRulePackVersion(CreateRulePackVersion("institution-head-neck", "v2", DateTimeOffset.UtcNow));

        var versions = store.ListRulePackVersions("INSTITUTION-HEAD-NECK");

        Assert.Equal(new[] { "v2", "v1" }, versions.Select(version => version.VersionId));
        var found = store.FindRulePackVersion("institution-head-neck", "V1") ?? throw new InvalidOperationException("Version was not stored.");
        Assert.Equal("sha256:v1", found.Fingerprint);
    }

    [Fact]
    public void PromoteRulePackVersionMakesOnlyOneVersionActive()
    {
        var store = new CiRunStore();
        store.SaveRulePackVersion(CreateRulePackVersion("institution-head-neck", "v1", DateTimeOffset.UtcNow.AddMinutes(-1)));
        store.SaveRulePackVersion(CreateRulePackVersion("institution-head-neck", "v2", DateTimeOffset.UtcNow));

        var promoted = store.PromoteRulePackVersion("institution-head-neck", "v2", DateTimeOffset.UtcNow, "physics", "Approved.");

        Assert.True(promoted.IsActive);
        Assert.Equal("physics", promoted.ActivatedBy);
        Assert.Equal("v2", store.FindActiveRulePackVersion("INSTITUTION-HEAD-NECK")?.VersionId);
        Assert.False(store.FindRulePackVersion("institution-head-neck", "v1")!.IsActive);
    }

    [Fact]
    public void SaveRtpxAcceptanceStoresQueryableAcceptanceHistory()
    {
        var store = new CiRunStore();
        store.SaveRtpxAcceptance(CreateRtpxAcceptance("old", DateTimeOffset.UtcNow.AddMinutes(-1), accepted: false));
        store.SaveRtpxAcceptance(CreateRtpxAcceptance("new", DateTimeOffset.UtcNow, accepted: true));

        var records = store.ListRtpxAcceptances();
        var found = store.FindRtpxAcceptance("NEW");

        Assert.Equal(new[] { "new", "old" }, records.Select(record => record.Id));
        Assert.NotNull(found);
        Assert.True(found.Accepted);
        Assert.Equal("rtpx-head-neck", found.RulePackId);
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

    private static CiRunBaseline CreateBaseline(string caseId, string runId, DateTimeOffset promotedAtUtc)
    {
        return new CiRunBaseline(
            caseId,
            runId,
            promotedAtUtc,
            CiRunInputKind.SyntheticCase,
            BeamKitCheckStatus.Pass,
            0,
            "case:head-neck-pass",
            "main",
            "abc123",
            "build-1",
            "plan-1",
            "Rule pack",
            "1",
            "sha256:plan",
            "sha256:rx",
            "sha256:pack",
            promotedBy: "physics");
    }

    private static CiServerManagedRulePackVersion CreateRulePackVersion(string rulePackId, string versionId, DateTimeOffset importedAtUtc)
    {
        return new CiServerManagedRulePackVersion(
            rulePackId,
            versionId,
            importedAtUtc,
            "physics",
            "InlineJson",
            "test",
            Directory.GetCurrentDirectory(),
            "{}",
            "Rule pack",
            versionId,
            "BeamKit",
            "Test rule pack.",
            "Head and Neck",
            new[] { "test" },
            $"sha256:{versionId}",
            new RulePackValidationReport("Rule pack", versionId, $"sha256:{versionId}", Array.Empty<RulePackPolicyIssue>()),
            new RulePackTestReport("Rule pack", versionId, importedAtUtc, Array.Empty<RulePackTestResult>()));
    }

    private static CiServerRtpxAcceptanceRecord CreateRtpxAcceptance(string id, DateTimeOffset createdAtUtc, bool accepted)
    {
        return new CiServerRtpxAcceptanceRecord(
            id,
            createdAtUtc,
            "Synthetic Hospital",
            "/tmp/protocol.rtpx.zip",
            "/tmp/accepted",
            accepted,
            promoted: false,
            "rtpx-head-neck",
            "version-1",
            "rtpx.synthetic.head-neck",
            "Synthetic Head and Neck",
            "1.0",
            "rtpx.synthetic.head-neck.accepted.synthetic-hospital",
            "sha256:package",
            "sha256:profile",
            null,
            hasEsapiEvidence: false,
            errorCount: accepted ? 0 : 1,
            warningCount: 0,
            """{"accepted":true}""",
            """{"subjectType":"RulePack"}""");
    }

    private static CaseWorkItem CreateWorkItem(
        string id,
        string caseId,
        CaseWorkItemStatus status,
        DateOnly dueDate,
        string? dosimetristId = null)
    {
        return new CaseWorkItem
        {
            Id = id,
            CreatedAtUtc = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc = new DateTimeOffset(2026, 7, 9, 12, 1, 0, TimeSpan.Zero),
            CaseId = caseId,
            DiseaseSite = "Lung",
            DueDate = dueDate,
            Priority = 4,
            Status = status,
            AssignedDosimetristId = dosimetristId,
            AssignmentHistory = new[]
            {
                new CaseWorkItemAssignmentEvent
                {
                    Id = $"{id}-event",
                    OccurredAtUtc = new DateTimeOffset(2026, 7, 9, 12, 1, 0, TimeSpan.Zero),
                    Actor = "test",
                    Action = "assigned",
                    Status = status,
                    DosimetristId = dosimetristId
                }
            }
        };
    }
}
