using BeamKit.Check;
using BeamKit.Sdk;
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

    private static BeamKitCiServerService CreateService(ICiRunStore store)
    {
        return new BeamKitCiServerService(new BeamKitClient(), store, new FixedTimeProvider());
    }

    private static HostedCiRunRecord CreateRecord(string id, DateTimeOffset createdAtUtc, BeamKitCheckStatus status)
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

        return new HostedCiRunRecord(id, createdAtUtc, source.SyntheticCaseId, artifact);
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
