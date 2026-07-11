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
        Assert.StartsWith("sha256:", found.PlanFingerprint, StringComparison.Ordinal);
        var artifactJson = secondStore.FindArtifactJson(saved.Id) ?? throw new InvalidOperationException("Artifact JSON was not stored.");
        Assert.Contains(saved.Artifact.Provenance.PlanFingerprint, artifactJson, StringComparison.Ordinal);
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
    }

    private static BeamKitCiServerService CreateService(ICiRunStore store)
    {
        return new BeamKitCiServerService(new BeamKitClient(), store, new FixedTimeProvider());
    }

    private static HostedCiRunRecord CreateRecord(
        string id,
        DateTimeOffset createdAtUtc,
        BeamKitCheckStatus status,
        CiRunInputKind inputKind = CiRunInputKind.SyntheticCase)
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

        return new HostedCiRunRecord(id, createdAtUtc, source.CaseId, artifact, inputKind);
    }

    private static void CreateLegacyDatabase(string path)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
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
