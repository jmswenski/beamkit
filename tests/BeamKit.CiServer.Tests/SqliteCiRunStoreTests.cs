using BeamKit.Check;
using BeamKit.Sdk;
using Microsoft.Data.Sqlite;
using Xunit;

namespace BeamKit.CiServer.Tests;

public sealed class SqliteCiRunStoreTests
{
    [Fact]
    public void SavedRunSurvivesNewStoreInstance()
    {
        using var database = TemporaryDatabase.Create();
        var firstStore = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });
        var service = CreateService(firstStore);

        var saved = service.CreateRun(new HostedCiRunRequest
        {
            SyntheticCaseId = "head-neck-pass",
            Branch = "main",
            Commit = "abc123",
            BuildId = "build-1"
        });

        var secondStore = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });
        var found = secondStore.Find(saved.Id);

        Assert.NotNull(found);
        Assert.Equal(saved.Id, found.Id);
        Assert.Equal(BeamKitCheckStatus.Pass, found.Status);
        Assert.Equal(CiRunInputKind.SyntheticCase, found.InputKind);
        Assert.Equal("main", found.Branch);
        Assert.True(found.HasPlanSnapshot);
        Assert.StartsWith("sha256:", found.PlanFingerprint, StringComparison.Ordinal);
        var artifactJson = secondStore.FindArtifactJson(saved.Id) ?? throw new InvalidOperationException("Artifact JSON was not stored.");
        Assert.Contains(saved.Artifact.Provenance.PlanFingerprint, artifactJson, StringComparison.Ordinal);
        var planSnapshotJson = secondStore.FindPlanSnapshotJson(saved.Id) ?? throw new InvalidOperationException("Plan snapshot JSON was not stored.");
        Assert.Contains("HN-SYN-001", planSnapshotJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ListFiltersByStatusCaseBranchAndDateRange()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });
        var service = CreateService(store);
        service.CreateRun(new HostedCiRunRequest { SyntheticCaseId = "head-neck-pass", Branch = "main" });
        service.CreateRun(new HostedCiRunRequest { SyntheticCaseId = "head-neck-cord-fail", Branch = "feature" });

        var failed = store.List(new CiRunQuery { Status = BeamKitCheckStatus.Fail });
        var main = store.List(new CiRunQuery { Branch = "main" });
        var cord = store.List(new CiRunQuery { SyntheticCaseId = "head-neck-cord-fail" });
        var inRange = store.List(new CiRunQuery
        {
            CreatedFromUtc = new DateTimeOffset(2026, 7, 9, 11, 59, 0, TimeSpan.Zero),
            CreatedToUtc = new DateTimeOffset(2026, 7, 9, 12, 1, 0, TimeSpan.Zero)
        });

        Assert.Single(failed);
        Assert.Equal("head-neck-cord-fail", failed[0].SyntheticCaseId);
        Assert.Single(main);
        Assert.Equal("main", main[0].Branch);
        Assert.Single(cord);
        Assert.Equal(2, inRange.Count);
    }

    [Fact]
    public void RetentionPrunesOldRuns()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteCiRunStore(new CiServerStorageOptions
        {
            DatabasePath = database.Path,
            EnableRetention = true,
            RetentionLimit = 2
        });

        store.Save(CreateRecord("old", new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero), BeamKitCheckStatus.Pass));
        store.Save(CreateRecord("middle", new DateTimeOffset(2026, 7, 9, 12, 1, 0, TimeSpan.Zero), BeamKitCheckStatus.Warning));
        store.Save(CreateRecord("new", new DateTimeOffset(2026, 7, 9, 12, 2, 0, TimeSpan.Zero), BeamKitCheckStatus.Fail));

        var records = store.List(new CiRunQuery { Limit = 10 });

        Assert.Equal(new[] { "new", "middle" }, records.Select(record => record.Id));
        Assert.Null(store.Find("old"));
    }

    [Fact]
    public void BaselineSurvivesNewStoreInstance()
    {
        using var database = TemporaryDatabase.Create();
        var firstStore = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });
        var run = firstStore.Save(CreateRecord("run-1", new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero), BeamKitCheckStatus.Pass));
        var summary = firstStore.Find(run.Id) ?? throw new InvalidOperationException("Run summary was not stored.");
        var baseline = firstStore.SaveBaseline(CiRunBaseline.FromRun(
            summary,
            new DateTimeOffset(2026, 7, 9, 12, 5, 0, TimeSpan.Zero),
            promotedBy: "physics",
            note: "Approved baseline."));

        var secondStore = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });
        var found = secondStore.FindBaseline(baseline.CaseId);

        Assert.NotNull(found);
        Assert.Equal("run-1", found.BaselineRunId);
        Assert.Equal("physics", found.PromotedBy);
        Assert.Equal("Approved baseline.", found.Note);
        Assert.Single(secondStore.ListBaselines());
    }

    [Fact]
    public void RetentionPreservesPromotedBaselineRun()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteCiRunStore(new CiServerStorageOptions
        {
            DatabasePath = database.Path,
            EnableRetention = true,
            RetentionLimit = 1
        });
        var old = store.Save(CreateRecord("old", new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero), BeamKitCheckStatus.Pass));
        var oldSummary = store.Find(old.Id) ?? throw new InvalidOperationException("Run summary was not stored.");
        store.SaveBaseline(CiRunBaseline.FromRun(oldSummary, new DateTimeOffset(2026, 7, 9, 12, 1, 0, TimeSpan.Zero)));

        store.Save(CreateRecord("new", new DateTimeOffset(2026, 7, 9, 12, 2, 0, TimeSpan.Zero), BeamKitCheckStatus.Fail));

        Assert.NotNull(store.Find("old"));
        Assert.NotNull(store.Find("new"));
    }

    [Fact]
    public void RetentionPreservesPromotedBaselinePlanSnapshot()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteCiRunStore(new CiServerStorageOptions
        {
            DatabasePath = database.Path,
            EnableRetention = true,
            RetentionLimit = 1
        });
        var service = CreateService(store);
        var baselineRun = service.CreateRun(new HostedCiRunRequest { SyntheticCaseId = "head-neck-pass" });
        service.PromoteBaseline(baselineRun.Id, new PromoteCiRunBaselineRequest());

        service.CreateRun(new HostedCiRunRequest { SyntheticCaseId = "head-neck-cord-fail" });

        Assert.NotNull(store.Find(baselineRun.Id));
        Assert.NotNull(store.FindPlanSnapshotJson(baselineRun.Id));
    }

    [Fact]
    public void ExistingDatabaseWithoutInputKindColumnIsUpgraded()
    {
        using var database = TemporaryDatabase.Create();
        CreateLegacyDatabase(database.Path);
        var store = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });

        store.Save(CreateRecord(
            "uploaded",
            new DateTimeOffset(2026, 7, 9, 12, 3, 0, TimeSpan.Zero),
            BeamKitCheckStatus.Pass,
            CiRunInputKind.BeamKitPlanJson));

        var found = store.Find("uploaded") ?? throw new InvalidOperationException("Run was not stored.");
        Assert.Equal(CiRunInputKind.BeamKitPlanJson, found.InputKind);
        Assert.False(found.HasPlanSnapshot);
        Assert.Null(store.FindPlanSnapshotJson("uploaded"));
    }

    [Fact]
    public void ExistingDatabaseWithoutPlanSnapshotColumnIsUpgraded()
    {
        using var database = TemporaryDatabase.Create();
        CreateDatabaseWithoutPlanSnapshotColumn(database.Path);
        var store = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });

        var saved = store.Save(CreateRecord(
            "upgraded",
            new DateTimeOffset(2026, 7, 9, 12, 4, 0, TimeSpan.Zero),
            BeamKitCheckStatus.Pass,
            planSnapshotJson: """{"patient":{"id":"P1"},"plan":{"id":"plan-1"}}"""));

        var found = store.Find(saved.Id) ?? throw new InvalidOperationException("Run was not stored.");
        Assert.True(found.HasPlanSnapshot);
        Assert.Contains("plan-1", store.FindPlanSnapshotJson(saved.Id), StringComparison.Ordinal);
    }

    [Fact]
    public void AuditEventsSurviveNewStoreInstance()
    {
        using var database = TemporaryDatabase.Create();
        var firstStore = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });
        firstStore.SaveAuditEvent(new CiServerAuditEvent(
            "audit-1",
            new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero),
            "physics-key",
            "baseline.promoted",
            "/api/runs/run-1/baseline",
            "POST",
            "run-1",
            "case-1",
            "Pass",
            "127.0.0.1",
            "physics"));

        var secondStore = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });
        var events = secondStore.ListAuditEvents(new CiServerAuditQuery { CaseId = "case-1" });

        var auditEvent = Assert.Single(events);
        Assert.Equal("audit-1", auditEvent.Id);
        Assert.Equal("physics-key", auditEvent.Actor);
        Assert.Equal("baseline.promoted", auditEvent.Action);
        Assert.Equal("run-1", auditEvent.RunId);
    }

    [Fact]
    public void WorkItemsSurviveNewStoreInstance()
    {
        using var database = TemporaryDatabase.Create();
        var firstStore = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });
        firstStore.SaveWorkItem(CreateWorkItem(
            "work-1",
            "case-1",
            CaseWorkItemStatus.Assigned,
            new DateOnly(2026, 7, 12),
            dosimetristId: "planner-jane",
            physicistId: "physicist-morgan"));
        firstStore.SaveWorkItem(CreateWorkItem(
            "work-2",
            "case-2",
            CaseWorkItemStatus.OnHold,
            new DateOnly(2026, 7, 10),
            dosimetristId: "planner-jane"));

        var secondStore = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });
        var active = secondStore.ListWorkItems(new CaseWorkItemQuery
        {
            ActiveOnly = true,
            AssignedStaffId = "planner-jane"
        });

        var workItem = Assert.Single(active);
        Assert.Equal("work-1", workItem.Id);
        Assert.Equal("case-1", workItem.CaseId);
        Assert.Equal("planner-jane", workItem.AssignedDosimetristId);
        Assert.Equal("physicist-morgan", workItem.AssignedPhysicistId);
        Assert.Equal(CaseWorkItemStatus.Assigned, workItem.AssignmentHistory.Single().Status);
        Assert.Equal("case-1", secondStore.FindWorkItem("WORK-1")?.CaseId);
    }

    [Fact]
    public void RulePackVersionsSurviveNewStoreInstance()
    {
        using var database = TemporaryDatabase.Create();
        var firstStore = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });
        firstStore.SaveRulePackVersion(CreateRulePackVersion("institution-head-neck", "v1", new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero)));
        firstStore.SaveRulePackVersion(CreateRulePackVersion("institution-head-neck", "v2", new DateTimeOffset(2026, 7, 9, 12, 1, 0, TimeSpan.Zero)));
        firstStore.PromoteRulePackVersion("institution-head-neck", "v2", new DateTimeOffset(2026, 7, 9, 12, 2, 0, TimeSpan.Zero), "physics", "Approved.");

        var secondStore = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });
        var versions = secondStore.ListRulePackVersions("institution-head-neck");
        var active = secondStore.FindActiveRulePackVersion("institution-head-neck");

        Assert.Equal(new[] { "v2", "v1" }, versions.Select(version => version.VersionId));
        Assert.NotNull(active);
        Assert.Equal("v2", active.VersionId);
        Assert.Equal("physics", active.ActivatedBy);
        Assert.True(active.TestReport?.Passed);
        Assert.True(active.ValidationReport.IsValid);
    }

    [Fact]
    public void RtpxAcceptancesSurviveNewStoreInstance()
    {
        using var database = TemporaryDatabase.Create();
        var firstStore = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });
        firstStore.SaveRtpxAcceptance(CreateRtpxAcceptance("rtpx-1", new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero), accepted: true) with
        {
            ReviewStatus = RtpxDraftReviewStatus.Approved,
            ReviewUpdatedAtUtc = new DateTimeOffset(2026, 7, 9, 12, 5, 0, TimeSpan.Zero),
            ReviewedBy = "physics",
            ReviewNote = "Reviewed protocol diff.",
            ApprovedBy = "physics",
            ApprovedAtUtc = new DateTimeOffset(2026, 7, 9, 12, 6, 0, TimeSpan.Zero),
            ApprovalNote = "Approved for release.",
            DiffAcknowledgedBy = "physics",
            DiffAcknowledgedAtUtc = new DateTimeOffset(2026, 7, 9, 12, 4, 0, TimeSpan.Zero),
            AcknowledgedDiffChangeIds = new[] { "metadata:name:changed", "structure:cord:added" }
        });
        firstStore.SaveRtpxAcceptance(CreateRtpxAcceptance("rtpx-2", new DateTimeOffset(2026, 7, 9, 12, 1, 0, TimeSpan.Zero), accepted: false));

        var secondStore = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });
        var records = secondStore.ListRtpxAcceptances();
        var found = secondStore.FindRtpxAcceptance("RTPX-1");

        Assert.Equal(new[] { "rtpx-2", "rtpx-1" }, records.Select(record => record.Id));
        Assert.NotNull(found);
        Assert.True(found.Accepted);
        Assert.Equal("sha256:package", found.PackageFingerprint);
        Assert.Contains("acceptance-report", found.ReportJson, StringComparison.Ordinal);
        Assert.Equal(RtpxDraftReviewStatus.Approved, found.ReviewStatus);
        Assert.Equal("physics", found.ReviewedBy);
        Assert.Equal("Approved for release.", found.ApprovalNote);
        Assert.Equal(new[] { "metadata:name:changed", "structure:cord:added" }, found.AcknowledgedDiffChangeIds);
    }

    [Fact]
    public void ProtocolComplianceRunsSurviveNewStoreInstance()
    {
        using var database = TemporaryDatabase.Create();
        var firstStore = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });
        firstStore.SaveProtocolComplianceRun(CreateProtocolComplianceRun("pcr-1", new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero), ProtocolComplianceStatus.Pass));
        firstStore.SaveProtocolComplianceRun(CreateProtocolComplianceRun("pcr-2", new DateTimeOffset(2026, 7, 9, 12, 1, 0, TimeSpan.Zero), ProtocolComplianceStatus.Fail));

        var secondStore = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });
        var records = secondStore.ListProtocolComplianceRuns();
        var found = secondStore.FindProtocolComplianceRun("PCR-2");

        Assert.Equal(new[] { "pcr-2", "pcr-1" }, records.Select(record => record.Id));
        Assert.NotNull(found);
        Assert.Equal(ProtocolComplianceStatus.Fail, found.Status);
        Assert.Equal("rtpx-head-neck", found.RulePackId);
        Assert.Equal("rtpx-1", found.RtpxAcceptanceId);
        Assert.Contains("Protocol Compliance", found.MarkdownReport, StringComparison.Ordinal);
        Assert.Contains("plan-1", found.PlanSnapshotJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ExistingRtpxAcceptanceTableMigratesReviewColumnsBeforeIndexCreation()
    {
        using var database = TemporaryDatabase.Create();
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = database.Path, Pooling = false }.ToString()))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE ci_rtpx_acceptances (
                    id TEXT PRIMARY KEY,
                    created_at_utc TEXT NOT NULL,
                    institution TEXT NOT NULL,
                    package_path TEXT NOT NULL,
                    output_directory TEXT NOT NULL,
                    accepted INTEGER NOT NULL,
                    promoted INTEGER NOT NULL,
                    rule_pack_id TEXT NULL,
                    version_id TEXT NULL,
                    source_protocol_id TEXT NOT NULL,
                    source_protocol_name TEXT NOT NULL,
                    source_protocol_version TEXT NOT NULL,
                    local_protocol_id TEXT NOT NULL,
                    package_fingerprint TEXT NOT NULL,
                    institution_profile_fingerprint TEXT NOT NULL,
                    esapi_snapshot_fingerprint TEXT NULL,
                    has_esapi_evidence INTEGER NOT NULL,
                    error_count INTEGER NOT NULL,
                    warning_count INTEGER NOT NULL,
                    report_json TEXT NOT NULL,
                    safety_evidence_json TEXT NULL
                );
                """;
            command.ExecuteNonQuery();
        }

        var store = new SqliteCiRunStore(new CiServerStorageOptions { DatabasePath = database.Path, EnableRetention = false });
        store.SaveRtpxAcceptance(CreateRtpxAcceptance("rtpx-legacy", new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero), accepted: true));

        var found = store.FindRtpxAcceptance("rtpx-legacy");

        Assert.NotNull(found);
        Assert.Equal(RtpxDraftReviewStatus.Promoted, found.ReviewStatus);
    }

    private static BeamKitCiServerService CreateService(ICiRunStore store)
    {
        return new BeamKitCiServerService(new BeamKitClient(), store, new FixedTimeProvider());
    }

    private static HostedCiRunRecord CreateRecord(
        string id,
        DateTimeOffset createdAtUtc,
        BeamKitCheckStatus status,
        CiRunInputKind inputKind = CiRunInputKind.SyntheticCase,
        string? planSnapshotJson = null)
    {
        var service = CreateService(new CiRunStore());
        var source = status == BeamKitCheckStatus.Fail
            ? service.CreateRun(new HostedCiRunRequest { SyntheticCaseId = "head-neck-cord-fail" })
            : service.CreateRun(new HostedCiRunRequest { SyntheticCaseId = "head-neck-pass" });
        var artifact = source.Artifact;
        if (status == BeamKitCheckStatus.Warning)
        {
            artifact = new BeamKitCiRunRecord(
                artifact.Provenance with { Status = BeamKitCheckStatus.Warning },
                artifact.PolicyValidation,
                artifact.CheckReport);
        }

        return new HostedCiRunRecord(id, createdAtUtc, source.CaseId, artifact, inputKind, planSnapshotJson);
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
            promoted: accepted,
            "rtpx-head-neck",
            "version-1",
            "rtpx.synthetic.head-neck",
            "Synthetic Head and Neck",
            "1.0",
            "rtpx.synthetic.head-neck.accepted.synthetic-hospital",
            "sha256:package",
            "sha256:profile",
            "sha256:esapi",
            hasEsapiEvidence: true,
            errorCount: accepted ? 0 : 1,
            warningCount: 2,
            """{"title":"acceptance-report"}""",
            """{"subjectType":"RulePack"}""");
    }

    private static ProtocolComplianceRunRecord CreateProtocolComplianceRun(
        string id,
        DateTimeOffset createdAtUtc,
        ProtocolComplianceStatus status)
    {
        return new ProtocolComplianceRunRecord(
            id,
            createdAtUtc,
            status,
            "plan-1",
            "patient-1",
            "course-1",
            "Head and Neck",
            CiRunInputKind.SyntheticCase,
            "case:head-neck-pass",
            "rtpx-head-neck",
            "version-1",
            "rtpx-1",
            "rtpx.synthetic.head-neck",
            "Synthetic Head and Neck",
            "1.0",
            "sha256:package",
            passCount: status == ProtocolComplianceStatus.Pass ? 4 : 3,
            warningCount: 0,
            failCount: status == ProtocolComplianceStatus.Fail ? 1 : 0,
            notEvaluableCount: 0,
            acceptedVarianceCount: 0,
            unresolvedBlockingCount: status == ProtocolComplianceStatus.Fail ? 1 : 0,
            """{"runId":"test"}""",
            "# BeamKit Protocol Compliance Packet",
            """{"id":"plan-1"}""");
    }

    private static CaseWorkItem CreateWorkItem(
        string id,
        string caseId,
        CaseWorkItemStatus status,
        DateOnly dueDate,
        string? dosimetristId = null,
        string? physicistId = null)
    {
        return new CaseWorkItem
        {
            Id = id,
            CreatedAtUtc = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc = new DateTimeOffset(2026, 7, 9, 12, 1, 0, TimeSpan.Zero),
            CaseId = caseId,
            SyntheticCaseId = "lung-sbrt-pass",
            DiseaseSite = "Lung",
            DueDate = dueDate,
            Priority = 4,
            Status = status,
            Physician = "Dr Smith",
            AssignedDosimetristId = dosimetristId,
            AssignedPhysicistId = physicistId,
            LastCheckStatus = BeamKitCheckStatus.Pass,
            AssignmentHistory = new[]
            {
                new CaseWorkItemAssignmentEvent
                {
                    Id = $"{id}-event",
                    OccurredAtUtc = new DateTimeOffset(2026, 7, 9, 12, 1, 0, TimeSpan.Zero),
                    Actor = "test",
                    Action = "assigned",
                    Status = status,
                    DosimetristId = dosimetristId,
                    PhysicistId = physicistId
                }
            }
        };
    }

    private static void CreateLegacyDatabase(string path)
    {
        using var connection = new SqliteConnection(CreateTemporaryDatabaseConnectionString(path));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE ci_runs (
                id TEXT PRIMARY KEY,
                created_at_utc TEXT NOT NULL,
                synthetic_case_id TEXT NOT NULL,
                status TEXT NOT NULL,
                exit_code INTEGER NOT NULL,
                input_source TEXT NULL,
                branch TEXT NULL,
                commit_sha TEXT NULL,
                build_id TEXT NULL,
                plan_id TEXT NOT NULL,
                rule_pack_name TEXT NOT NULL,
                rule_pack_version TEXT NOT NULL,
                plan_fingerprint TEXT NOT NULL,
                prescription_fingerprint TEXT NOT NULL,
                rule_pack_fingerprint TEXT NOT NULL,
                artifact_json TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    private static void CreateDatabaseWithoutPlanSnapshotColumn(string path)
    {
        using var connection = new SqliteConnection(CreateTemporaryDatabaseConnectionString(path));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE ci_runs (
                id TEXT PRIMARY KEY,
                created_at_utc TEXT NOT NULL,
                synthetic_case_id TEXT NOT NULL,
                input_kind TEXT NOT NULL DEFAULT 'SyntheticCase',
                status TEXT NOT NULL,
                exit_code INTEGER NOT NULL,
                input_source TEXT NULL,
                branch TEXT NULL,
                commit_sha TEXT NULL,
                build_id TEXT NULL,
                plan_id TEXT NOT NULL,
                rule_pack_name TEXT NOT NULL,
                rule_pack_version TEXT NOT NULL,
                plan_fingerprint TEXT NOT NULL,
                prescription_fingerprint TEXT NOT NULL,
                rule_pack_fingerprint TEXT NOT NULL,
                artifact_json TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    private static string CreateTemporaryDatabaseConnectionString(string path)
    {
        return new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Pooling = false
        }.ToString();
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        }
    }

    private sealed class TemporaryDatabase : IDisposable
    {
        private TemporaryDatabase(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryDatabase Create()
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beamkit-ci-server-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return new TemporaryDatabase(System.IO.Path.Combine(directory, "runs.db"));
        }

        public void Dispose()
        {
            var directory = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
