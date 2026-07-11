using System.Globalization;
using BeamKit.ChangeDetection;
using BeamKit.Check;
using BeamKit.Core.Domain;
using BeamKit.Core.Serialization;
using BeamKit.Esapi;
using BeamKit.Samples;
using BeamKit.Sdk;
using BeamKit.Workflow;

namespace BeamKit.CiServer;

/// <summary>
/// Application service behind the hosted BeamKit CI server endpoints.
/// </summary>
public sealed class BeamKitCiServerService
{
    private readonly BeamKitClient client;
    private readonly ICiRunStore store;
    private readonly TimeProvider timeProvider;
    private readonly CiServerRulePackRegistry rulePacks;

    /// <summary>
    /// Creates a hosted CI server service.
    /// </summary>
    public BeamKitCiServerService(
        BeamKitClient client,
        ICiRunStore store,
        TimeProvider? timeProvider = null,
        CiServerRulePackRegistry? rulePacks = null)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.rulePacks = rulePacks ?? new CiServerRulePackRegistry(new CiServerRulePackRegistryOptions());
    }

    /// <summary>
    /// Lists built-in PHI-free synthetic cases.
    /// </summary>
    public IReadOnlyList<SyntheticCaseSummary> ListCases()
    {
        return SyntheticClinicalCaseLibrary.All()
            .Select(clinicalCase => new SyntheticCaseSummary(
                clinicalCase.Id,
                clinicalCase.Name,
                clinicalCase.DiseaseSite,
                clinicalCase.Description,
                clinicalCase.ExpectedToPass,
                clinicalCase.ExpectedFindings))
            .ToArray();
    }

    /// <summary>
    /// Creates and stores a BeamKit CI run.
    /// </summary>
    public HostedCiRunRecord CreateRun(HostedCiRunRequest request, CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var caseId = CiServerText.Optional(request.SyntheticCaseId) ?? "head-neck-pass";
        var clinicalCase = SyntheticClinicalCaseLibrary.Find(caseId);
        var rulePack = rulePacks.Load(request.RulePackId, request.RulePackPath);
        return CreateRunForPlan(
            clinicalCase.Plan,
            rulePack,
            caseId: clinicalCase.Id,
            inputKind: CiRunInputKind.SyntheticCase,
            inputSource: $"case:{clinicalCase.Id}",
            request.Branch,
            request.Commit,
            request.BuildId,
            auditContext);
    }

    /// <summary>
    /// Creates and stores a BeamKit CI run from uploaded vendor-neutral plan content.
    /// </summary>
    public HostedCiRunRecord CreateRunFromPlanSnapshot(HostedCiRunUploadRequest request, CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var uploaded = LoadUploadedPlan(request);
        var record = CreateRunForPlan(
            uploaded.Plan,
            rulePacks.Load(request.RulePackId, request.RulePackPath),
            caseId: uploaded.Plan.Id,
            uploaded.InputKind,
            CiServerText.Optional(request.InputSource) ?? uploaded.DefaultInputSource,
            request.Branch,
            request.Commit,
            request.BuildId,
            auditContext);
        Audit(
            "plan-snapshot.uploaded",
            auditContext,
            record.Id,
            record.CaseId,
            record.Status.ToString(),
            record.InputKind.ToString());
        return record;
    }

    private HostedCiRunRecord CreateRunForPlan(
        Plan plan,
        BeamKitRulePack rulePack,
        string caseId,
        CiRunInputKind inputKind,
        string? inputSource,
        string? branch,
        string? commit,
        string? buildId,
        CiServerAuditContext? auditContext)
    {
        var artifact = client.RunCiGate(new BeamKitCiRunRequest(
            plan,
            rulePack,
            inputSource: inputSource,
            branch: branch,
            commit: commit,
            buildId: buildId));
        var record = new HostedCiRunRecord(
            CreateServerRunId(),
            timeProvider.GetUtcNow(),
            caseId,
            artifact,
            inputKind,
            BeamKitPlanJson.ToJson(plan));
        var saved = store.Save(record);
        Audit(
            "run.created",
            auditContext,
            saved.Id,
            saved.CaseId,
            saved.Status.ToString(),
            saved.Artifact.Provenance.RulePackFingerprint);
        return saved;
    }

    /// <summary>
    /// Lists recent stored CI runs.
    /// </summary>
    public IReadOnlyList<HostedCiRunSummary> ListRuns(int limit = 50)
    {
        return ListRuns(new CiRunQuery { Limit = limit });
    }

    /// <summary>
    /// Lists stored CI runs matching the supplied query.
    /// </summary>
    public IReadOnlyList<HostedCiRunSummary> ListRuns(CiRunQuery query)
    {
        return store.List(query);
    }

    /// <summary>
    /// Finds a stored CI run.
    /// </summary>
    public HostedCiRunSummary? FindRun(string id)
    {
        return store.Find(id);
    }

    /// <summary>
    /// Finds the stored full artifact JSON for a CI run.
    /// </summary>
    public string? FindArtifactJson(string id)
    {
        return store.FindArtifactJson(id);
    }

    /// <summary>
    /// Promotes a stored CI run as the baseline for its case key.
    /// </summary>
    public CiRunBaseline PromoteBaseline(string runId, PromoteCiRunBaselineRequest request, CiServerAuditContext? auditContext = null)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("Run id is required.", nameof(runId));
        }

        ArgumentNullException.ThrowIfNull(request);

        var run = store.Find(runId) ?? throw new InvalidOperationException($"Run '{runId}' was not found.");
        var baseline = CiRunBaseline.FromRun(
            run,
            timeProvider.GetUtcNow(),
            request.PromotedBy,
            request.Note);
        var saved = store.SaveBaseline(baseline);
        Audit("baseline.promoted", auditContext, saved.BaselineRunId, saved.CaseId, saved.Status.ToString(), saved.PromotedBy);
        return saved;
    }

    /// <summary>
    /// Finds the promoted baseline for a case key.
    /// </summary>
    public CiRunBaseline? FindBaseline(string caseId)
    {
        return store.FindBaseline(caseId);
    }

    /// <summary>
    /// Lists promoted baselines.
    /// </summary>
    public IReadOnlyList<CiRunBaseline> ListBaselines()
    {
        return store.ListBaselines();
    }

    /// <summary>
    /// Compares a stored CI run to the promoted baseline for its case key.
    /// </summary>
    public CiRunBaselineComparisonReport CompareToBaseline(string runId, CiServerAuditContext? auditContext = null)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("Run id is required.", nameof(runId));
        }

        var run = store.Find(runId) ?? throw new InvalidOperationException($"Run '{runId}' was not found.");
        var baseline = store.FindBaseline(run.CaseId)
            ?? throw new InvalidOperationException($"No baseline has been promoted for case '{run.CaseId}'.");
        var planChanges = CompareStoredPlanSnapshots(baseline.BaselineRunId, run.Id);
        var report = CiRunBaselineComparisonReport.Create(baseline, run, timeProvider.GetUtcNow(), planChanges);
        Audit("baseline.compared", auditContext, run.Id, run.CaseId, report.MatchesBaseline ? "Match" : "Changed", report.BlockingCount.ToString(CultureInfo.InvariantCulture));
        return report;
    }

    private PlanChangeReport? CompareStoredPlanSnapshots(string baselineRunId, string comparisonRunId)
    {
        var baselineJson = store.FindPlanSnapshotJson(baselineRunId);
        var comparisonJson = store.FindPlanSnapshotJson(comparisonRunId);
        if (string.IsNullOrWhiteSpace(baselineJson) || string.IsNullOrWhiteSpace(comparisonJson))
        {
            return null;
        }

        var baselinePlan = BeamKitPlanJson.FromJson(baselineJson);
        var comparisonPlan = BeamKitPlanJson.FromJson(comparisonJson);
        return new PlanChangeDetector().Compare(baselinePlan, comparisonPlan);
    }

    /// <summary>
    /// Validates a rule pack as policy-as-code.
    /// </summary>
    public RulePackValidationReport ValidateRulePack(RulePackValidationServerRequest request, CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var report = client.ValidateRulePack(rulePacks.Load(request.RulePackId, request.RulePackPath));
        Audit("rule-pack.validated", auditContext, status: report.IsValid ? "Valid" : "Invalid", details: report.Fingerprint);
        return report;
    }

    /// <summary>
    /// Runs rule-pack regression tests.
    /// </summary>
    public RulePackTestReport TestRulePack(RulePackTestServerRequest request, CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var report = client.TestRulePack(rulePacks.Load(request.RulePackId, request.RulePackPath), LoadRulePackTestCases(request.SyntheticCaseId));
        Audit("rule-pack.tested", auditContext, status: report.Passed ? "Passed" : "Failed", details: report.RulePackVersion);
        return report;
    }

    /// <summary>
    /// Creates a planner assignment recommendation.
    /// </summary>
    public PlannerAssignmentRecommendation RecommendAssignment(AssignmentServerRequest request, CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var assignmentDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var dueDate = ParseDueDate(request.DueDate) ?? assignmentDate.AddDays(3);
        var requiredSkills = request.RequiredSkills is { Count: > 0 }
            ? request.RequiredSkills
            : new[] { "VMAT" };
        var assignmentRequest = new PlannerAssignmentRequest(
            CiServerText.Optional(request.CaseId) ?? "server-assignment",
            CiServerText.Optional(request.DiseaseSite) ?? "Head and Neck",
            dueDate,
            CreateSyntheticPlannerProfiles(assignmentDate),
            requiredSkills,
            request.ComplexityScore ?? 3,
            request.Priority ?? 3,
            request.Physician,
            assignmentDate);

        var recommendation = client.RecommendPlanner(assignmentRequest);
        Audit(
            "assignment.recommended",
            auditContext,
            caseId: assignmentRequest.CaseId,
            status: recommendation.RecommendedPlanner is null ? "NoRecommendation" : "Recommended",
            details: recommendation.RecommendedPlanner?.Planner.Id);
        return recommendation;
    }

    /// <summary>
    /// Lists registered rule packs.
    /// </summary>
    public IReadOnlyList<CiServerRulePackSummary> ListRulePacks()
    {
        return rulePacks.List();
    }

    /// <summary>
    /// Finds one registered rule pack.
    /// </summary>
    public CiServerRulePackDetail? FindRulePack(string id)
    {
        return rulePacks.Find(id);
    }

    /// <summary>
    /// Validates a registered rule pack.
    /// </summary>
    public RulePackValidationReport ValidateRulePack(string id, CiServerAuditContext? auditContext = null)
    {
        var report = client.ValidateRulePack(rulePacks.Load(id));
        Audit("rule-pack.validated", auditContext, status: report.IsValid ? "Valid" : "Invalid", details: $"{id}:{report.Fingerprint}");
        return report;
    }

    /// <summary>
    /// Runs regression tests for a registered rule pack.
    /// </summary>
    public RulePackTestReport TestRulePack(string id, RulePackTestServerRequest request, CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var report = client.TestRulePack(rulePacks.Load(id), LoadRulePackTestCases(request.SyntheticCaseId));
        Audit("rule-pack.tested", auditContext, status: report.Passed ? "Passed" : "Failed", details: $"{id}:{report.RulePackVersion}");
        return report;
    }

    /// <summary>
    /// Records artifact download audit evidence.
    /// </summary>
    public void RecordArtifactDownloaded(string runId, CiServerAuditContext? auditContext = null)
    {
        var run = store.Find(runId);
        Audit("artifact.downloaded", auditContext, run?.Id ?? runId, run?.CaseId, run?.Status.ToString(), run?.PlanFingerprint);
    }

    /// <summary>
    /// Lists stored audit events.
    /// </summary>
    public IReadOnlyList<CiServerAuditEvent> ListAuditEvents(CiServerAuditQuery query)
    {
        return store.ListAuditEvents(query);
    }

    private void Audit(
        string action,
        CiServerAuditContext? auditContext = null,
        string? runId = null,
        string? caseId = null,
        string? status = null,
        string? details = null)
    {
        var context = auditContext ?? CiServerAuditContext.Service;
        store.SaveAuditEvent(new CiServerAuditEvent(
            CreateAuditEventId(),
            timeProvider.GetUtcNow(),
            context.Actor,
            action,
            context.Endpoint,
            context.Method,
            runId,
            caseId,
            status,
            context.SourceIp,
            details));
    }

    private static IReadOnlyList<RulePackTestCase> LoadRulePackTestCases(string? syntheticCaseId)
    {
        if (!string.IsNullOrWhiteSpace(syntheticCaseId))
        {
            return new[] { CreateRulePackTestCase(SyntheticClinicalCaseLibrary.Find(syntheticCaseId)) };
        }

        return new[]
        {
            CreateRulePackTestCase(SyntheticClinicalCaseLibrary.Find("head-neck-pass")),
            CreateRulePackTestCase(SyntheticClinicalCaseLibrary.Find("head-neck-cord-fail")),
            CreateRulePackTestCase(SyntheticClinicalCaseLibrary.Find("head-neck-missing-structure"))
        };
    }

    private static RulePackTestCase CreateRulePackTestCase(SyntheticClinicalCase clinicalCase)
    {
        return new RulePackTestCase(
            clinicalCase.Id,
            clinicalCase.Description,
            clinicalCase.Plan,
            clinicalCase.ExpectedToPass ? BeamKitCheckStatus.Pass : BeamKitCheckStatus.Fail,
            ExpectedFindingIdsForCase(clinicalCase.Id));
    }

    private static IReadOnlyList<string> ExpectedFindingIdsForCase(string caseId)
    {
        return caseId.ToLowerInvariant() switch
        {
            "head-neck-cord-fail" => new[] { "cord.max" },
            "head-neck-missing-structure" => new[] { "lung.l.v20" },
            _ => Array.Empty<string>()
        };
    }

    private static UploadedPlan LoadUploadedPlan(HostedCiRunUploadRequest request)
    {
        var planJson = GetJson(request.Plan, request.PlanJson);
        var esapiSnapshotJson = GetJson(request.EsapiSnapshot, request.EsapiSnapshotJson);
        var format = ParseUploadFormat(request.Format, planJson, esapiSnapshotJson);

        return format switch
        {
            CiRunInputKind.BeamKitPlanJson => LoadBeamKitPlanJson(planJson),
            CiRunInputKind.EsapiSnapshotJson => LoadEsapiSnapshotJson(esapiSnapshotJson),
            _ => throw new ArgumentException("Uploaded CI runs require BeamKit plan JSON or ESAPI snapshot JSON.", nameof(request))
        };
    }

    private static UploadedPlan LoadBeamKitPlanJson(string? planJson)
    {
        if (string.IsNullOrWhiteSpace(planJson))
        {
            throw new ArgumentException("BeamKit plan JSON upload requires 'plan' or 'planJson'.", nameof(planJson));
        }

        var plan = BeamKitPlanJson.FromJson(planJson);
        return new UploadedPlan(plan, CiRunInputKind.BeamKitPlanJson, $"beamkit-plan-json:{plan.Id}");
    }

    private static UploadedPlan LoadEsapiSnapshotJson(string? esapiSnapshotJson)
    {
        if (string.IsNullOrWhiteSpace(esapiSnapshotJson))
        {
            throw new ArgumentException("ESAPI snapshot upload requires 'esapiSnapshot' or 'esapiSnapshotJson'.", nameof(esapiSnapshotJson));
        }

        var snapshot = EsapiPlanSnapshotJson.FromJson(esapiSnapshotJson);
        var validation = new EsapiSnapshotValidator().Validate(snapshot);
        if (validation.ErrorCount > 0)
        {
            var errors = string.Join("; ", validation.Issues
                .Where(issue => issue.Severity == EsapiSnapshotIssueSeverity.Error)
                .Take(3)
                .Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"ESAPI snapshot validation failed with {validation.ErrorCount} error(s): {errors}");
        }

        var plan = new EsapiPlanConverter().Convert(snapshot);
        return new UploadedPlan(plan, CiRunInputKind.EsapiSnapshotJson, $"esapi-snapshot:{snapshot.PlanId}");
    }

    private static CiRunInputKind ParseUploadFormat(string? format, string? planJson, string? esapiSnapshotJson)
    {
        var hasPlan = !string.IsNullOrWhiteSpace(planJson);
        var hasEsapiSnapshot = !string.IsNullOrWhiteSpace(esapiSnapshotJson);
        if (hasPlan && hasEsapiSnapshot)
        {
            throw new ArgumentException("Use only one of 'plan'/'planJson' or 'esapiSnapshot'/'esapiSnapshotJson'.", nameof(format));
        }

        if (string.IsNullOrWhiteSpace(format))
        {
            if (hasPlan)
            {
                return CiRunInputKind.BeamKitPlanJson;
            }

            if (hasEsapiSnapshot)
            {
                return CiRunInputKind.EsapiSnapshotJson;
            }

            throw new ArgumentException("Uploaded CI runs require a plan or ESAPI snapshot payload.", nameof(format));
        }

        return format.Trim().ToLowerInvariant() switch
        {
            "beamkit" or "beamkit-plan" or "beamkit-plan-json" or "plan" or "plan-json" => CiRunInputKind.BeamKitPlanJson,
            "esapi" or "esapi-snapshot" or "esapi-snapshot-json" => CiRunInputKind.EsapiSnapshotJson,
            _ => throw new ArgumentException($"Unsupported uploaded plan format '{format}'.", nameof(format))
        };
    }

    private static string? GetJson(System.Text.Json.JsonElement? element, string? rawJson)
    {
        if (!string.IsNullOrWhiteSpace(rawJson))
        {
            return rawJson;
        }

        if (element is null || element.Value.ValueKind is System.Text.Json.JsonValueKind.Null or System.Text.Json.JsonValueKind.Undefined)
        {
            return null;
        }

        return element.Value.GetRawText();
    }

    private sealed record UploadedPlan(Plan Plan, CiRunInputKind InputKind, string DefaultInputSource);

    private static IReadOnlyList<PlannerProfile> CreateSyntheticPlannerProfiles(DateOnly assignmentDate)
    {
        return new[]
        {
            new PlannerProfile(
                "planner-jane",
                "Jane Doe",
                new[] { "VMAT", "SBRT", "Head and Neck" },
                new[] { "Head and Neck", "Lung" },
                activeCaseCount: 2,
                maxActiveCaseCount: 8),
            new PlannerProfile(
                "planner-alex",
                "Alex Kim",
                new[] { "VMAT", "Prostate" },
                new[] { "Prostate" },
                activeCaseCount: 6,
                maxActiveCaseCount: 8),
            new PlannerProfile(
                "planner-priya",
                "Priya Shah",
                new[] { "VMAT", "SRS", "Head and Neck" },
                new[] { "Head and Neck", "Brain" },
                activeCaseCount: 4,
                maxActiveCaseCount: 8,
                ptoUntil: assignmentDate.AddDays(1)),
            new PlannerProfile(
                "planner-sam",
                "Sam Rivera",
                new[] { "3D", "Breast" },
                new[] { "Breast" },
                activeCaseCount: 1,
                maxActiveCaseCount: 8)
        };
    }

    private string CreateServerRunId()
    {
        return $"run-{timeProvider.GetUtcNow():yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..37];
    }

    private string CreateAuditEventId()
    {
        return $"audit-{timeProvider.GetUtcNow():yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..39];
    }

    private static DateOnly? ParseDueDate(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : DateOnly.Parse(value, CultureInfo.InvariantCulture);
    }
}
