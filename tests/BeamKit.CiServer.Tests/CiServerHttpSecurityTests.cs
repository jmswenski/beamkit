using System.Net;
using System.Text;
using System.Text.Json;
using BeamKit.Core.Serialization;
using BeamKit.Samples;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BeamKit.CiServer.Tests;

public sealed class CiServerHttpSecurityTests
{
    private const string ApiKey = "test-secret";

    [Fact]
    public async Task HealthIsPublicButApiRoutesRequireApiKey()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path);
        using var client = factory.CreateClient();

        var health = await client.GetAsync("/health");
        var unauthorizedRuns = await client.GetAsync("/api/runs");
        using var authorizedRequest = CreateRequest(HttpMethod.Get, "/api/runs");
        var authorizedRuns = await client.SendAsync(authorizedRequest);

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedRuns.StatusCode);
        Assert.Equal(HttpStatusCode.OK, authorizedRuns.StatusCode);
    }

    [Fact]
    public async Task AuthorizedRunCreationWritesAuditEvent()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path);
        using var client = factory.CreateClient();
        using var createRun = CreateJsonRequest(HttpMethod.Post, "/api/runs", """{"syntheticCaseId":"head-neck-pass"}""");

        var response = await client.SendAsync(createRun);
        using var auditEvents = await GetJson(client, "/api/audit-events?action=run.created");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auditEvent = Assert.Single(auditEvents.RootElement.EnumerateArray());
        Assert.Equal("test-key", auditEvent.GetProperty("actor").GetString());
        Assert.Equal("run.created", auditEvent.GetProperty("action").GetString());
        Assert.Equal("head-neck-pass", auditEvent.GetProperty("caseId").GetString());
        Assert.Equal("Pass", auditEvent.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PlanSnapshotUploadRejectsOversizedPayloadBeforeModelBinding()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path, maxPlanSnapshotUploadBytes: 1_024);
        using var client = factory.CreateClient();
        var oversizedPlan = new string('x', 2_000);
        using var request = CreateJsonRequest(
            HttpMethod.Post,
            "/api/runs/from-plan-snapshot",
            $$"""{"format":"beamkit-plan-json","planJson":"{{oversizedPlan}}"}""");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task RulePackRegistryEndpointsUseApiKey()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path);
        using var client = factory.CreateClient();

        using var rulePacks = await GetJson(client, "/api/rule-packs");
        using var validate = CreateJsonRequest(HttpMethod.Post, "/api/rule-packs/synthetic-head-neck/validate", "{}");
        var validateResponse = await client.SendAsync(validate);
        using var validation = JsonDocument.Parse(await validateResponse.Content.ReadAsStringAsync());

        var rulePack = Assert.Single(rulePacks.RootElement.EnumerateArray());
        Assert.Equal("synthetic-head-neck", rulePack.GetProperty("id").GetString());
        Assert.True(rulePack.GetProperty("isValid").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);
        Assert.True(validation.RootElement.GetProperty("isValid").GetBoolean());
    }

    [Fact]
    public async Task UploadedPlanCanUseRegisteredRulePackId()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path);
        using var client = factory.CreateClient();
        var planJson = BeamKitPlanJson.ToJson(SyntheticClinicalCaseLibrary.HeadAndNeckBaseline().Plan);
        var payload = JsonSerializer.Serialize(new
        {
            format = "beamkit-plan-json",
            planJson,
            rulePackId = "synthetic-head-neck"
        });
        using var request = CreateJsonRequest(HttpMethod.Post, "/api/runs/from-plan-snapshot", payload);

        var response = await client.SendAsync(request);
        using var run = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("HN-SYN-001", run.RootElement.GetProperty("caseId").GetString());
        Assert.Equal("Pass", run.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ManagedRulePackImportPromoteAndRunFlowUsesApiKey()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path);
        using var client = factory.CreateClient();
        var importPayload = JsonSerializer.Serialize(new
        {
            rulePackId = "institution-head-neck",
            manifestPath = SampleRulePackPath(),
            importedBy = "physics"
        });
        using var importRequest = CreateJsonRequest(HttpMethod.Post, "/api/rule-packs/import", importPayload);

        var importResponse = await client.SendAsync(importRequest);
        using var importResult = JsonDocument.Parse(await importResponse.Content.ReadAsStringAsync());
        var version = importResult.RootElement.GetProperty("version");
        var versionId = version.GetProperty("versionId").GetString()
            ?? throw new InvalidOperationException("Import did not return a version id.");
        var fingerprint = version.GetProperty("fingerprint").GetString()
            ?? throw new InvalidOperationException("Import did not return a fingerprint.");
        var safetyEvidence = CreateSafetyEvidence(versionId, fingerprint);
        using var safetyReviewRequest = CreateJsonRequest(
            HttpMethod.Post,
            $"/api/rule-packs/institution-head-neck/versions/{versionId}/safety-evidence/validate",
            JsonSerializer.Serialize(safetyEvidence));
        var safetyReviewResponse = await client.SendAsync(safetyReviewRequest);
        using var safetyReview = JsonDocument.Parse(await safetyReviewResponse.Content.ReadAsStringAsync());
        var promotePayload = JsonSerializer.Serialize(new
        {
            promotedBy = "physics",
            note = "Approved.",
            safetyEvidence
        });
        using var promoteRequest = CreateJsonRequest(
            HttpMethod.Post,
            $"/api/rule-packs/institution-head-neck/versions/{versionId}/promote",
            promotePayload);
        var promoteResponse = await client.SendAsync(promoteRequest);
        using var activeRulePack = await GetJson(client, "/api/rule-packs/institution-head-neck");
        using var storedSafetyEvidence = await GetJson(client, $"/api/rule-packs/institution-head-neck/versions/{versionId}/safety-evidence");
        using var runRequest = CreateJsonRequest(
            HttpMethod.Post,
            "/api/runs",
            """{"syntheticCaseId":"head-neck-pass","rulePackId":"institution-head-neck"}""");
        var runResponse = await client.SendAsync(runRequest);
        using var run = JsonDocument.Parse(await runResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Created, importResponse.StatusCode);
        Assert.True(importResult.RootElement.GetProperty("validation").GetProperty("isValid").GetBoolean());
        Assert.True(importResult.RootElement.GetProperty("testReport").GetProperty("passed").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, safetyReviewResponse.StatusCode);
        Assert.True(safetyReview.RootElement.GetProperty("isAcceptable").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, promoteResponse.StatusCode);
        Assert.Equal("Managed", activeRulePack.RootElement.GetProperty("summary").GetProperty("sourceKind").GetString());
        Assert.Equal(versionId, storedSafetyEvidence.RootElement.GetProperty("subjectVersion").GetString());
        Assert.Equal(HttpStatusCode.Created, runResponse.StatusCode);
        Assert.Equal("Pass", run.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task RulePackDraftReviewEndpointUsesApiKey()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path);
        using var client = factory.CreateClient();
        var payload = JsonSerializer.Serialize(new
        {
            manifestPath = SampleRulePackPath(),
            runRegressionTests = true
        });
        using var request = CreateJsonRequest(HttpMethod.Post, "/api/rule-packs/synthetic-head-neck/review-draft", payload);

        var response = await client.SendAsync(request);
        using var review = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(review.RootElement.GetProperty("isPromotable").GetBoolean());
        Assert.True(review.RootElement.GetProperty("validation").GetProperty("isValid").GetBoolean());
        Assert.True(review.RootElement.GetProperty("testReport").GetProperty("passed").GetBoolean());
        Assert.Equal("synthetic-head-neck", review.RootElement.GetProperty("rulePackId").GetString());
    }

    private static async Task<JsonDocument> GetJson(HttpClient client, string path)
    {
        using var request = CreateRequest(HttpMethod.Get, path);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static HttpRequestMessage CreateJsonRequest(HttpMethod method, string path, string json)
    {
        var request = CreateRequest(method, path);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return request;
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-BeamKit-Api-Key", ApiKey);
        return request;
    }

    private static object CreateSafetyEvidence(string versionId, string fingerprint)
    {
        return new
        {
            id = $"evidence-{versionId}",
            subjectType = "RulePack",
            subjectId = "institution-head-neck",
            subjectVersion = versionId,
            subjectFingerprint = fingerprint,
            generatedAtUtc = "2026-07-09T12:00:00Z",
            intendedUse = "ClinicalDecisionSupport",
            owner = "Physics",
            reviewer = "Clinical QA",
            checklist = new
            {
                name = "Managed rule-pack promotion controls",
                version = "1",
                controls = new[]
                {
                    new
                    {
                        id = "CTRL-REGRESSION",
                        title = "Regression tests pass",
                        description = "Known-good and known-bad rule-pack cases have been executed.",
                        type = "Verification",
                        isRequired = true,
                        isSatisfied = true,
                        evidenceIds = new[] { "EV-REGRESSION" }
                    },
                    new
                    {
                        id = "CTRL-CLINICAL-REVIEW",
                        title = "Clinical reviewer accepted policy",
                        description = "A qualified reviewer accepted the policy content for the stated intended use.",
                        type = "Process",
                        isRequired = true,
                        isSatisfied = true,
                        evidenceIds = new[] { "EV-CLINICAL-REVIEW" }
                    }
                }
            },
            evidenceItems = new[]
            {
                new
                {
                    id = "EV-REGRESSION",
                    title = "Managed rule-pack regression suite",
                    kind = "RegressionTest",
                    status = "Pass",
                    performedAtUtc = "2026-07-09T12:00:00Z",
                    source = "BeamKit.CiServer HTTP regression flow",
                    reviewedBy = (string?)null
                },
                new
                {
                    id = "EV-CLINICAL-REVIEW",
                    title = "Clinical review signoff",
                    kind = "ClinicalReview",
                    status = "Pass",
                    performedAtUtc = "2026-07-09T12:05:00Z",
                    source = "Clinical QA signoff",
                    reviewedBy = (string?)"Physics"
                }
            }
        };
    }

    private static string SampleRulePackPath()
    {
        return System.IO.Path.Combine(FindRepositoryRoot(), "samples", "rule-packs", "head-neck-v1", "beamkit-rule-pack.json");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(System.IO.Path.Combine(directory.FullName, "BeamKit.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find BeamKit repository root.");
    }

    private sealed class TestCiServerFactory : WebApplicationFactory<Program>
    {
        private readonly string databasePath;
        private readonly long maxPlanSnapshotUploadBytes;

        public TestCiServerFactory(string databasePath, long maxPlanSnapshotUploadBytes = 5_000_000)
        {
            this.databasePath = databasePath;
            this.maxPlanSnapshotUploadBytes = maxPlanSnapshotUploadBytes;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["BeamKit:CiServer:Storage:DatabasePath"] = databasePath,
                    ["BeamKit:CiServer:Storage:EnableRetention"] = "false",
                    ["BeamKit:CiServer:Security:RequireApiKey"] = "true",
                    ["BeamKit:CiServer:Security:HeaderName"] = "X-BeamKit-Api-Key",
                    ["BeamKit:CiServer:Security:MaxPlanSnapshotUploadBytes"] = maxPlanSnapshotUploadBytes.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["BeamKit:CiServer:Security:ApiKeys:0:Label"] = "test-key",
                    ["BeamKit:CiServer:Security:ApiKeys:0:Key"] = ApiKey
                });
            });
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
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beamkit-ci-server-http-tests", Guid.NewGuid().ToString("N"));
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
