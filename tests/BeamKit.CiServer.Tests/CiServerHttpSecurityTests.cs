using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeamKit.Core.Domain;
using BeamKit.Core.Serialization;
using BeamKit.Protocols;
using BeamKit.Protocols.Acceptance;
using BeamKit.Protocols.Word;
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
    public async Task RtpxWordExtractRejectsOversizedPayloadBeforeModelBinding()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path, maxPlanSnapshotUploadBytes: 1_024);
        using var client = factory.CreateClient();
        var oversizedDocx = new string('x', 2_000);
        using var request = CreateJsonRequest(
            HttpMethod.Post,
            "/api/rtpx/word/extract",
            $$"""{"fileName":"protocol.docx","docxBase64":"{{oversizedDocx}}"}""");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task RtpxWordExtractAllowsLocalWordAddInCorsPreflight()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/rtpx/word/extract");
        request.Headers.Add("Origin", "https://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,x-beamkit-api-key");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains("https://localhost:3000", response.Headers.GetValues("Access-Control-Allow-Origin"));
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

    [Fact]
    public async Task RtpxAcceptanceEndpointAcceptsPackageUploadAndListsRecord()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path);
        using var client = factory.CreateClient();
        var packagePath = CreateWordRtpxPackage();
        var payload = JsonSerializer.Serialize(new
        {
            packageBase64 = Convert.ToBase64String(File.ReadAllBytes(packagePath)),
            institutionProfileJson = CreateInstitutionProfileJson(),
            rulePackId = "http-rtpx-head-neck",
            runRegressionTests = false,
            importedBy = "http-test"
        });
        using var request = CreateJsonRequest(HttpMethod.Post, "/api/rtpx/acceptance", payload);

        var response = await client.SendAsync(request);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        using var records = await GetJson(client, "/api/rtpx/acceptance?limit=10");
        var acceptanceId = result.RootElement.GetProperty("acceptance").GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Acceptance id was not returned.");
        using var detail = await GetJson(client, $"/api/rtpx/acceptance/{acceptanceId}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(result.RootElement.GetProperty("report").GetProperty("isAccepted").GetBoolean());
        Assert.Equal("http-rtpx-head-neck", result.RootElement.GetProperty("rulePackImport").GetProperty("version").GetProperty("rulePackId").GetString());
        var record = Assert.Single(records.RootElement.EnumerateArray());
        Assert.Equal(acceptanceId, record.GetProperty("id").GetString());
        Assert.True(record.GetProperty("accepted").GetBoolean());
        Assert.Contains("Synthetic Hospital", detail.RootElement.GetProperty("reportJson").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RtpxWordExtractEndpointAcceptsDocxUploadAndReturnsPackage()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path);
        using var client = factory.CreateClient();
        var docxPath = CreateWordProtocolDocument();
        var payload = JsonSerializer.Serialize(new
        {
            fileName = "protocol.docx",
            docxBase64 = Convert.ToBase64String(File.ReadAllBytes(docxPath)),
            includeSourceDocument = true
        });
        using var request = CreateJsonRequest(HttpMethod.Post, "/api/rtpx/word/extract", payload);

        var response = await client.SendAsync(request);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(result.RootElement.GetProperty("isValid").GetBoolean());
        Assert.Equal("rtpx.example.protocol", result.RootElement.GetProperty("extraction").GetProperty("package").GetProperty("id").GetString());
        Assert.False(string.IsNullOrWhiteSpace(result.RootElement.GetProperty("rtpxJson").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(result.RootElement.GetProperty("rtpxPackageBase64").GetString()));
        Assert.Equal("protocol.docx", result.RootElement.GetProperty("sourceFileName").GetString());
    }

    [Fact]
    public async Task RtpxWordExtractEndpointSupportsQuickCheckWithoutPackageGeneration()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path);
        using var client = factory.CreateClient();
        var docxPath = CreateWordProtocolDocument();
        var payload = JsonSerializer.Serialize(new
        {
            fileName = "protocol.docx",
            docxBase64 = Convert.ToBase64String(File.ReadAllBytes(docxPath)),
            generatePackage = false
        });
        using var request = CreateJsonRequest(HttpMethod.Post, "/api/rtpx/word/extract", payload);

        var response = await client.SendAsync(request);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(result.RootElement.GetProperty("isValid").GetBoolean());
        Assert.Equal("rtpx.example.protocol", result.RootElement.GetProperty("extraction").GetProperty("package").GetProperty("id").GetString());
        Assert.False(string.IsNullOrWhiteSpace(result.RootElement.GetProperty("rtpxJson").GetString()));
        Assert.Equal(JsonValueKind.Null, result.RootElement.GetProperty("rtpxPackageBase64").ValueKind);
        Assert.Equal(JsonValueKind.Null, result.RootElement.GetProperty("rtpxPackageFileName").ValueKind);
    }

    [Fact]
    public async Task RtpxAuthoringLibraryEndpointsReturnTemplatesAndSnippets()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path);
        using var client = factory.CreateClient();

        using var templates = await GetJson(client, "/api/rtpx/authoring/templates");
        using var snippets = await GetJson(client, "/api/rtpx/authoring/snippets");

        Assert.Equal("beamkit.defaults.rtpx.templates", templates.RootElement.GetProperty("libraryId").GetString());
        Assert.True(templates.RootElement.GetProperty("templates").GetArrayLength() >= 6);
        Assert.Equal("beamkit.defaults.rtpx.snippets", snippets.RootElement.GetProperty("libraryId").GetString());
        Assert.True(snippets.RootElement.GetProperty("snippets").GetArrayLength() >= 8);
    }

    [Fact]
    public async Task RtpxWordPublishDraftEndpointImportsDraftAndListsReview()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path);
        using var client = factory.CreateClient();
        var docxPath = CreateWordProtocolDocument();
        var payload = JsonSerializer.Serialize(new
        {
            fileName = "protocol.docx",
            docxBase64 = Convert.ToBase64String(File.ReadAllBytes(docxPath)),
            rulePackId = "draft-head-neck",
            syntheticCaseId = "head-neck-pass",
            runRegressionTests = true
        });
        using var request = CreateJsonRequest(HttpMethod.Post, "/api/rtpx/word/publish-draft", payload);

        var response = await client.SendAsync(request);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        using var drafts = await GetJson(client, "/api/rtpx/drafts");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(result.RootElement.GetProperty("published").GetBoolean());
        Assert.True(result.RootElement.GetProperty("acceptance").GetProperty("accepted").GetBoolean());
        Assert.Equal("draft-head-neck", result.RootElement.GetProperty("rulePackImport").GetProperty("version").GetProperty("rulePackId").GetString());
        Assert.True(result.RootElement.GetProperty("protocolDiff").GetProperty("isInitial").GetBoolean());
        var draft = Assert.Single(drafts.RootElement.EnumerateArray());
        Assert.Equal(result.RootElement.GetProperty("acceptance").GetProperty("id").GetString(), draft.GetProperty("acceptance").GetProperty("id").GetString());
        Assert.Equal("Draft", draft.GetProperty("acceptance").GetProperty("reviewStatus").GetString());
        Assert.True(draft.GetProperty("version").GetProperty("isValid").GetBoolean());
        Assert.True(draft.GetProperty("protocolDiff").GetProperty("isInitial").GetBoolean());
        Assert.Equal(JsonValueKind.Object, draft.GetProperty("safetyEvidence").ValueKind);
    }

    [Fact]
    public async Task RtpxDraftReviewEndpointsRequireApprovalBeforePromotion()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path);
        using var client = factory.CreateClient();
        var packagePath = CreatePassingHeadNeckRtpxPackage();
        var payload = JsonSerializer.Serialize(new
        {
            packageBase64 = Convert.ToBase64String(File.ReadAllBytes(packagePath)),
            institutionProfileJson = CreatePassingHeadNeckInstitutionProfileJson(),
            rulePackId = "draft-governed-head-neck",
            syntheticCaseId = "head-neck-pass",
            runRegressionTests = true,
            importedBy = "physics"
        });
        using var publishRequest = CreateJsonRequest(HttpMethod.Post, "/api/rtpx/acceptance", payload);
        var publishResponse = await client.SendAsync(publishRequest);
        using var publishResult = JsonDocument.Parse(await publishResponse.Content.ReadAsStringAsync());
        var draftId = publishResult.RootElement.GetProperty("acceptance").GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Draft id was not returned.");

        using var earlyPromoteRequest = CreateJsonRequest(
            HttpMethod.Post,
            $"/api/rtpx/drafts/{draftId}/promote",
            """{"reviewedBy":"physics","note":"Too early."}""");
        var earlyPromoteResponse = await client.SendAsync(earlyPromoteRequest);
        using var reviewRequest = CreateJsonRequest(
            HttpMethod.Post,
            $"/api/rtpx/drafts/{draftId}/review",
            """{"reviewedBy":"physics","note":"Starting clinical review."}""");
        var reviewResponse = await client.SendAsync(reviewRequest);
        using var review = JsonDocument.Parse(await reviewResponse.Content.ReadAsStringAsync());
        using var acknowledgeRequest = CreateJsonRequest(
            HttpMethod.Post,
            $"/api/rtpx/drafts/{draftId}/acknowledge-diff",
            """{"reviewedBy":"physics","note":"Initial package reviewed."}""");
        var acknowledgeResponse = await client.SendAsync(acknowledgeRequest);
        using var acknowledged = JsonDocument.Parse(await acknowledgeResponse.Content.ReadAsStringAsync());
        using var approveRequest = CreateJsonRequest(
            HttpMethod.Post,
            $"/api/rtpx/drafts/{draftId}/approve",
            """{"reviewedBy":"physics","note":"Approved for local release."}""");
        var approveResponse = await client.SendAsync(approveRequest);
        using var approved = JsonDocument.Parse(await approveResponse.Content.ReadAsStringAsync());
        using var promoteRequest = CreateJsonRequest(
            HttpMethod.Post,
            $"/api/rtpx/drafts/{draftId}/promote",
            """{"reviewedBy":"physics","note":"Released after review."}""");
        var promoteResponse = await client.SendAsync(promoteRequest);
        using var promoted = JsonDocument.Parse(await promoteResponse.Content.ReadAsStringAsync());
        using var persisted = await GetJson(client, $"/api/rtpx/drafts/{draftId}");

        Assert.Equal(HttpStatusCode.Created, publishResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, earlyPromoteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
        Assert.Equal("InReview", review.RootElement.GetProperty("acceptance").GetProperty("reviewStatus").GetString());
        Assert.Equal(HttpStatusCode.OK, acknowledgeResponse.StatusCode);
        Assert.True(acknowledged.RootElement.GetProperty("isDiffAcknowledged").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        Assert.Equal("Approved", approved.RootElement.GetProperty("acceptance").GetProperty("reviewStatus").GetString());
        Assert.Equal(HttpStatusCode.OK, promoteResponse.StatusCode);
        Assert.Equal("Promoted", promoted.RootElement.GetProperty("acceptance").GetProperty("reviewStatus").GetString());
        Assert.True(promoted.RootElement.GetProperty("acceptance").GetProperty("promoted").GetBoolean());
        Assert.Equal("Promoted", persisted.RootElement.GetProperty("acceptance").GetProperty("reviewStatus").GetString());
    }

    [Fact]
    public async Task ProtocolComplianceEndpointsRunActiveRtpxProtocolAndAcceptVariance()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path);
        using var client = factory.CreateClient();
        var packagePath = CreatePassingHeadNeckRtpxPackage();
        var acceptPayload = JsonSerializer.Serialize(new
        {
            packageBase64 = Convert.ToBase64String(File.ReadAllBytes(packagePath)),
            institutionProfileJson = CreatePassingHeadNeckInstitutionProfileJson(),
            rulePackId = "http-compliance-head-neck",
            syntheticCaseId = "head-neck-pass",
            runRegressionTests = true,
            promote = true,
            importedBy = "physics"
        });
        using var acceptRequest = CreateJsonRequest(HttpMethod.Post, "/api/rtpx/acceptance", acceptPayload);
        var acceptResponse = await client.SendAsync(acceptRequest);
        using var acceptResult = JsonDocument.Parse(await acceptResponse.Content.ReadAsStringAsync());
        var acceptanceId = acceptResult.RootElement.GetProperty("acceptance").GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Acceptance id was not returned.");
        var compliancePayload = JsonSerializer.Serialize(new
        {
            syntheticCaseId = "head-neck-cord-fail",
            rtpxAcceptanceId = acceptanceId,
            inputSource = "http-test"
        });
        using var complianceRequest = CreateJsonRequest(HttpMethod.Post, "/api/protocol-compliance/runs", compliancePayload);
        var complianceResponse = await client.SendAsync(complianceRequest);
        using var compliance = JsonDocument.Parse(await complianceResponse.Content.ReadAsStringAsync());
        var runId = compliance.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Compliance run id was not returned.");
        using var reportJson = await GetJson(client, $"/api/protocol-compliance/runs/{runId}/report.json");
        var blockingFindingId = reportJson.RootElement
            .GetProperty("findings")
            .EnumerateArray()
            .First(finding => finding.GetProperty("status").GetString() is "Fail" or "NotEvaluable")
            .GetProperty("id")
            .GetString()
            ?? throw new InvalidOperationException("Blocking finding id was not returned.");
        using var varianceRequest = CreateJsonRequest(
            HttpMethod.Post,
            $"/api/protocol-compliance/runs/{runId}/variances",
            JsonSerializer.Serialize(new
            {
                findingId = blockingFindingId,
                acceptedBy = "physics",
                rationale = "Documented protocol-approved exception for endpoint test."
            }));
        var varianceResponse = await client.SendAsync(varianceRequest);
        using var variance = JsonDocument.Parse(await varianceResponse.Content.ReadAsStringAsync());
        using var runs = await GetJson(client, "/api/protocol-compliance/runs?limit=10");
        using var markdownRequest = CreateRequest(HttpMethod.Get, $"/api/protocol-compliance/runs/{runId}/report.md");
        var markdownResponse = await client.SendAsync(markdownRequest);
        var markdown = await markdownResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, acceptResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, complianceResponse.StatusCode);
        Assert.Equal("Fail", compliance.RootElement.GetProperty("status").GetString());
        Assert.Equal(acceptanceId, compliance.RootElement.GetProperty("rtpxAcceptanceId").GetString());
        Assert.Equal(HttpStatusCode.OK, varianceResponse.StatusCode);
        Assert.Equal(1, variance.RootElement.GetProperty("summary").GetProperty("acceptedVarianceCount").GetInt32());
        Assert.Contains(runs.RootElement.EnumerateArray(), run => run.GetProperty("id").GetString() == runId);
        Assert.Equal(HttpStatusCode.OK, markdownResponse.StatusCode);
        Assert.Contains("BeamKit Protocol Compliance Packet", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RtpxAcceptanceEndpointReturnsBadRequestForMissingServerPath()
    {
        using var database = TemporaryDatabase.Create();
        await using var factory = new TestCiServerFactory(database.Path);
        using var client = factory.CreateClient();
        var profilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.WriteAllText(profilePath, CreateInstitutionProfileJson());
        using var request = CreateJsonRequest(
            HttpMethod.Post,
            "/api/rtpx/acceptance",
            JsonSerializer.Serialize(new
            {
                packagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.rtpx.zip"),
                institutionProfilePath = profilePath
            }));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    private static string CreateWordRtpxPackage()
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beamkit-ci-server-http-rtpx", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var docxPath = System.IO.Path.Combine(directory, "protocol.docx");
        var packagePath = System.IO.Path.Combine(directory, "protocol.rtpx.zip");
        new RtpxWordTemplateGenerator().Create(docxPath);
        var result = new RtpxWordPackageStore().Create(docxPath, packagePath);
        Assert.True(result.WrotePackage);
        return packagePath;
    }

    private static string CreatePassingHeadNeckRtpxPackage()
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beamkit-ci-server-http-rtpx", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var packagePath = System.IO.Path.Combine(directory, "head-neck.rtpx.zip");
        var package = new RadiotherapyProtocolPackage(
            "rtpx.synthetic.head-neck",
            "Synthetic Head and Neck Protocol",
            "1.0",
            "Head and Neck",
            "Definitive",
            structures: new[]
            {
                new ProtocolStructureRequirement("ptv.7000", "PTV_7000", ProtocolStructureRole.Target),
                new ProtocolStructureRequirement("cord", "Cord", ProtocolStructureRole.OrganAtRisk)
            },
            prescriptions: new[]
            {
                new ProtocolPrescription("rx.primary", "PTV_7000", 70m, 35, technique: "VMAT", energy: "6X")
            },
            constraints: new[]
            {
                new ProtocolDoseConstraint(
                    "ptv.d95",
                    "PTV_7000",
                    "D95%",
                    GoalComparison.GreaterThanOrEqual,
                    66.5m,
                    "Gy",
                    description: "PTV D95 coverage objective."),
                new ProtocolDoseConstraint(
                    "cord.max",
                    "Cord",
                    "Max",
                    GoalComparison.LessThanOrEqual,
                    45m,
                    "Gy",
                    description: "Cord maximum dose limit.")
            });
        var manifest = new RtpxWordPackageManifest(
            "beamkit.rtpx.word-package/0.1",
            new DateTimeOffset(2026, 7, 12, 12, 0, 0, TimeSpan.Zero).ToString("O"),
            package.Id,
            package.Name,
            package.Version,
            package.SchemaVersion,
            "synthetic.docx",
            "sha256:synthetic",
            IncludesSourceDocument: false,
            new[]
            {
                RtpxWordPackageStore.RtpxEntryName,
                RtpxWordPackageStore.ManifestEntryName,
                RtpxWordPackageStore.ValidationEntryName
            });
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);
        WriteEntry(archive, RtpxWordPackageStore.RtpxEntryName, RadiotherapyProtocolPackageStore.ToJson(package));
        WriteEntry(archive, RtpxWordPackageStore.ManifestEntryName, JsonSerializer.Serialize(manifest, jsonOptions));
        WriteEntry(archive, RtpxWordPackageStore.ValidationEntryName, "{}");
        return packagePath;
    }

    private static string CreateWordProtocolDocument()
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beamkit-ci-server-http-rtpx-word", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var docxPath = System.IO.Path.Combine(directory, "protocol.docx");
        new RtpxWordTemplateGenerator().Create(docxPath);
        return docxPath;
    }

    private static string CreateInstitutionProfileJson()
    {
        return RtpxInstitutionProfileStore.ToJson(new RtpxInstitutionProfile(
            "Synthetic Hospital",
            new[]
            {
                new RtpxStructureMapping("PTV_5000", "PTV_Hospital"),
                new RtpxStructureMapping("Cord", "SpinalCord")
            },
            acceptedBy: "Physics Director",
            effectiveDate: new DateOnly(2026, 7, 12),
            reviewedBy: "Protocol Physicist"));
    }

    private static string CreatePassingHeadNeckInstitutionProfileJson()
    {
        return RtpxInstitutionProfileStore.ToJson(new RtpxInstitutionProfile(
            "Synthetic Hospital",
            new[]
            {
                new RtpxStructureMapping("PTV_7000", "PTV_7000"),
                new RtpxStructureMapping("Cord", "SpinalCord")
            },
            acceptedBy: "Physics Director",
            effectiveDate: new DateOnly(2026, 7, 12),
            reviewedBy: "Protocol Physicist",
            localPolicyReference: "Synthetic protocol committee"));
    }

    private static void WriteEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
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
