using System.Globalization;
using BeamKit.Check;
using BeamKit.Core.Domain;
using BeamKit.Core.Serialization;
using BeamKit.Deliverability;
using BeamKit.Esapi;
using BeamKit.PlanCheck;
using BeamKit.Samples;
using BeamKit.Sdk;
using BeamKit.Templates;
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

    /// <summary>
    /// Creates a hosted CI server service.
    /// </summary>
    public BeamKitCiServerService(BeamKitClient client, ICiRunStore store, TimeProvider? timeProvider = null)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.timeProvider = timeProvider ?? TimeProvider.System;
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
    public HostedCiRunRecord CreateRun(HostedCiRunRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var caseId = CiServerText.Optional(request.SyntheticCaseId) ?? "head-neck-pass";
        var clinicalCase = SyntheticClinicalCaseLibrary.Find(caseId);
        var rulePack = LoadRulePack(request.RulePackPath);
        return CreateRunForPlan(
            clinicalCase.Plan,
            rulePack,
            caseId: clinicalCase.Id,
            inputKind: CiRunInputKind.SyntheticCase,
            inputSource: $"case:{clinicalCase.Id}",
            request.Branch,
            request.Commit,
            request.BuildId);
    }

    /// <summary>
    /// Creates and stores a BeamKit CI run from uploaded vendor-neutral plan content.
    /// </summary>
    public HostedCiRunRecord CreateRunFromPlanSnapshot(HostedCiRunUploadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var uploaded = LoadUploadedPlan(request);
        return CreateRunForPlan(
            uploaded.Plan,
            LoadRulePack(request.RulePackPath),
            caseId: uploaded.Plan.Id,
            uploaded.InputKind,
            CiServerText.Optional(request.InputSource) ?? uploaded.DefaultInputSource,
            request.Branch,
            request.Commit,
            request.BuildId);
    }

    private HostedCiRunRecord CreateRunForPlan(
        Plan plan,
        BeamKitRulePack rulePack,
        string caseId,
        CiRunInputKind inputKind,
        string? inputSource,
        string? branch,
        string? commit,
        string? buildId)
    {
        var artifact = client.RunCiGate(new BeamKitCiRunRequest(
            plan,
            rulePack,
            inputSource: inputSource,
            branch: branch,
            commit: commit,
            buildId: buildId));
        var record = new HostedCiRunRecord(CreateServerRunId(), timeProvider.GetUtcNow(), caseId, artifact, inputKind);
        return store.Save(record);
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
    /// Validates a rule pack as policy-as-code.
    /// </summary>
    public RulePackValidationReport ValidateRulePack(RulePackValidationServerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return client.ValidateRulePack(LoadRulePack(request.RulePackPath));
    }

    /// <summary>
    /// Runs rule-pack regression tests.
    /// </summary>
    public RulePackTestReport TestRulePack(RulePackTestServerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return client.TestRulePack(LoadRulePack(request.RulePackPath), LoadRulePackTestCases(request.SyntheticCaseId));
    }

    /// <summary>
    /// Creates a planner assignment recommendation.
    /// </summary>
    public PlannerAssignmentRecommendation RecommendAssignment(AssignmentServerRequest request)
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

        return client.RecommendPlanner(assignmentRequest);
    }

    private static BeamKitRulePack LoadRulePack(string? rulePackPath)
    {
        return string.IsNullOrWhiteSpace(rulePackPath)
            ? CreateDefaultRulePack()
            : BeamKitRulePackLoader.FromFile(rulePackPath);
    }

    private static BeamKitRulePack CreateDefaultRulePack()
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
            description: "Synthetic default rule pack for hosted BeamKit CI server demos.",
            diseaseSite: "Head and Neck",
            tags: new[] { "synthetic", "head-neck", "ci-server" });
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

    private static DateOnly? ParseDueDate(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : DateOnly.Parse(value, CultureInfo.InvariantCulture);
    }
}
