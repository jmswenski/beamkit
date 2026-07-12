using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BeamKit.ChangeDetection;
using BeamKit.Check;
using BeamKit.Core.Domain;
using BeamKit.Core.Serialization;
using BeamKit.Esapi;
using BeamKit.Intelligence;
using BeamKit.Naming;
using BeamKit.PlanCheck;
using BeamKit.Protocols;
using BeamKit.Protocols.Acceptance;
using BeamKit.Protocols.Word;
using BeamKit.RulePacks;
using BeamKit.Rules;
using BeamKit.Safety;
using BeamKit.Samples;
using BeamKit.Sdk;
using BeamKit.Workflow;
using Microsoft.Extensions.Options;

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
    private readonly CiServerRtpxAuthoringOptions rtpxAuthoringOptions;

    /// <summary>
    /// Creates a hosted CI server service.
    /// </summary>
    public BeamKitCiServerService(
        BeamKitClient client,
        ICiRunStore store,
        TimeProvider? timeProvider = null,
        CiServerRulePackRegistry? rulePacks = null,
        IOptions<CiServerRtpxAuthoringOptions>? rtpxAuthoringOptions = null)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.rulePacks = rulePacks ?? new CiServerRulePackRegistry(new CiServerRulePackRegistryOptions());
        this.rtpxAuthoringOptions = rtpxAuthoringOptions?.Value ?? new CiServerRtpxAuthoringOptions();
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
    /// Loads the configured RT-PX authoring template library.
    /// </summary>
    public RtpxAuthoringTemplateLibrary GetRtpxAuthoringTemplates()
    {
        var path = ResolveRtpxAuthoringLibraryPath(
            rtpxAuthoringOptions.TemplateLibraryPath,
            "rtpx-templates.json");
        var library = JsonSerializer.Deserialize<RtpxAuthoringTemplateLibrary>(File.ReadAllText(path), CiServerJson.Options)
            ?? throw new InvalidOperationException($"RT-PX template library '{path}' did not produce a library.");
        if (library.Templates.Count == 0)
        {
            throw new InvalidOperationException($"RT-PX template library '{path}' does not contain any templates.");
        }

        return library;
    }

    /// <summary>
    /// Loads the configured RT-PX authoring snippet library.
    /// </summary>
    public RtpxAuthoringSnippetLibrary GetRtpxAuthoringSnippets()
    {
        var path = ResolveRtpxAuthoringLibraryPath(
            rtpxAuthoringOptions.SnippetLibraryPath,
            "rtpx-snippets.json");
        var library = JsonSerializer.Deserialize<RtpxAuthoringSnippetLibrary>(File.ReadAllText(path), CiServerJson.Options)
            ?? throw new InvalidOperationException($"RT-PX snippet library '{path}' did not produce a library.");
        if (library.Snippets.Count == 0)
        {
            throw new InvalidOperationException($"RT-PX snippet library '{path}' does not contain any snippets.");
        }

        return library;
    }

    /// <summary>
    /// Creates and stores a BeamKit CI run.
    /// </summary>
    public HostedCiRunRecord CreateRun(HostedCiRunRequest request, CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var caseId = CiServerText.Optional(request.SyntheticCaseId) ?? "head-neck-pass";
        var clinicalCase = SyntheticClinicalCaseLibrary.Find(caseId);
        var rulePack = LoadRulePack(request.RulePackId, request.RulePackPath);
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
            LoadRulePack(request.RulePackId, request.RulePackPath),
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

        var report = client.ValidateRulePack(LoadRulePack(request.RulePackId, request.RulePackPath));
        Audit("rule-pack.validated", auditContext, status: report.IsValid ? "Valid" : "Invalid", details: report.Fingerprint);
        return report;
    }

    /// <summary>
    /// Runs rule-pack regression tests.
    /// </summary>
    public RulePackTestReport TestRulePack(RulePackTestServerRequest request, CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var report = client.TestRulePack(LoadRulePack(request.RulePackId, request.RulePackPath), LoadRulePackTestCases(request.SyntheticCaseId));
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
        var context = CreateAssignmentRequestContext(request, assignmentDate, includeTeamRoles: false);

        var recommendation = client.RecommendPlanner(context.Request) with { Intelligence = context.Intelligence };
        Audit(
            "assignment.recommended",
            auditContext,
            caseId: context.Request.CaseId,
            status: recommendation.RecommendedPlanner is null ? "NoRecommendation" : "Recommended",
            details: recommendation.RecommendedPlanner?.Planner.Id);
        return recommendation;
    }

    /// <summary>
    /// Creates a dosimetrist/physicist staffing recommendation.
    /// </summary>
    public PlanStaffingRecommendation RecommendStaffing(AssignmentServerRequest request, CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var assignmentDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var context = CreateAssignmentRequestContext(request, assignmentDate, includeTeamRoles: true);
        var recommendation = client.RecommendPlanningTeam(context.Request) with { Intelligence = context.Intelligence };
        Audit(
            "assignment.team-recommended",
            auditContext,
            caseId: context.Request.CaseId,
            status: recommendation.IsFullyStaffed ? "FullyStaffed" : "NeedsReview",
            details: string.Join(", ", recommendation.RoleRecommendations.Select(role => $"{role.Role}:{role.RecommendedCandidate?.Planner.Id ?? "none"}")));
        return recommendation;
    }

    /// <summary>
    /// Creates a persistent case work item for hosted queue and assignment workflows.
    /// </summary>
    public CaseWorkItem CreateWorkItem(CreateCaseWorkItemRequest request, CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var assignmentDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var assignmentContext = CreateAssignmentRequestContext(new AssignmentServerRequest
        {
            CaseId = request.CaseId,
            SyntheticCaseId = request.SyntheticCaseId,
            DiseaseSite = request.DiseaseSite,
            DueDate = request.DueDate,
            Priority = request.Priority,
            Physician = request.Physician,
            Plan = request.Plan,
            PlanJson = request.PlanJson,
            EsapiSnapshot = request.EsapiSnapshot,
            EsapiSnapshotJson = request.EsapiSnapshotJson,
            UseLiveWorkload = false
        }, assignmentDate, includeTeamRoles: true);
        var linkedRun = CiServerText.Optional(request.LastRunId) is { } runId ? store.Find(runId) : null;
        var now = timeProvider.GetUtcNow();
        var status = request.Status ?? CaseWorkItemStatus.NeedsAssignment;
        var workItem = new CaseWorkItem
        {
            Id = CreateWorkItemId(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CaseId = assignmentContext.Request.CaseId,
            SyntheticCaseId = CiServerText.Optional(request.SyntheticCaseId),
            DiseaseSite = assignmentContext.Request.DiseaseSite,
            DueDate = assignmentContext.Request.DueDate,
            Priority = assignmentContext.Request.Priority,
            Status = status,
            Physician = assignmentContext.Request.Physician,
            RulePackId = CiServerText.Optional(request.RulePackId),
            LastRunId = CiServerText.Optional(request.LastRunId),
            LastCheckStatus = linkedRun?.Status,
            Intelligence = assignmentContext.Intelligence,
            AssignmentHistory = new[]
            {
                CreateWorkItemHistoryEvent(
                    status,
                    auditContext,
                    "created",
                    note: request.SyntheticCaseId is null ? "Work item created." : $"Work item created from synthetic case {request.SyntheticCaseId}.")
            }
        };

        var saved = store.SaveWorkItem(workItem);
        Audit("work-item.created", auditContext, caseId: saved.CaseId, status: saved.Status.ToString(), details: saved.Id);
        return saved;
    }

    /// <summary>
    /// Finds one case work item.
    /// </summary>
    public CaseWorkItem? FindWorkItem(string id)
    {
        return store.FindWorkItem(id);
    }

    /// <summary>
    /// Lists persistent case work items.
    /// </summary>
    public IReadOnlyList<CaseWorkItem> ListWorkItems(CaseWorkItemQuery query)
    {
        return store.ListWorkItems(query);
    }

    /// <summary>
    /// Creates a dosimetrist/physicist staffing recommendation for a queued case work item.
    /// </summary>
    public PlanStaffingRecommendation RecommendWorkItemAssignment(
        string id,
        AssignmentServerRequest request,
        CiServerAuditContext? auditContext = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Work item id is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(request);

        var workItem = store.FindWorkItem(id) ?? throw new InvalidOperationException($"Work item '{id}' was not found.");
        var assignmentDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var context = CreateAssignmentRequestContext(CreateWorkItemAssignmentRequest(workItem, request), assignmentDate, includeTeamRoles: true);
        var intelligence = context.Intelligence ?? workItem.Intelligence;
        var recommendation = client.RecommendPlanningTeam(context.Request) with { Intelligence = intelligence };
        var details = string.Join(", ", recommendation.RoleRecommendations.Select(role => $"{role.Role}:{role.RecommendedCandidate?.Planner.Id ?? "none"}"));
        var updated = workItem with
        {
            UpdatedAtUtc = timeProvider.GetUtcNow(),
            Intelligence = intelligence,
            AssignmentHistory = AppendHistory(workItem, CreateWorkItemHistoryEvent(
                workItem.Status,
                auditContext,
                "recommended",
                note: details))
        };
        store.SaveWorkItem(updated);
        Audit(
            "work-item.assignment-recommended",
            auditContext,
            caseId: workItem.CaseId,
            status: recommendation.IsFullyStaffed ? "FullyStaffed" : "NeedsReview",
            details: $"{workItem.Id}: {details}");
        return recommendation;
    }

    /// <summary>
    /// Explicitly assigns staff to a queued case work item.
    /// </summary>
    public CaseWorkItem AssignWorkItem(string id, AssignCaseWorkItemRequest request, CiServerAuditContext? auditContext = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Work item id is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(request);

        var workItem = store.FindWorkItem(id) ?? throw new InvalidOperationException($"Work item '{id}' was not found.");
        var dosimetristId = CiServerText.Optional(request.DosimetristId) ?? workItem.AssignedDosimetristId;
        var dosimetristName = CiServerText.Optional(request.DosimetristName) ?? workItem.AssignedDosimetristName;
        var physicistId = CiServerText.Optional(request.PhysicistId) ?? workItem.AssignedPhysicistId;
        var physicistName = CiServerText.Optional(request.PhysicistName) ?? workItem.AssignedPhysicistName;
        var hasAssignment = dosimetristId != workItem.AssignedDosimetristId
            || dosimetristName != workItem.AssignedDosimetristName
            || physicistId != workItem.AssignedPhysicistId
            || physicistName != workItem.AssignedPhysicistName;
        if (!hasAssignment && request.Status is null && string.IsNullOrWhiteSpace(request.Note))
        {
            throw new ArgumentException("At least one assignment, status, or note is required.", nameof(request));
        }

        var status = request.Status
            ?? (hasAssignment && workItem.Status is CaseWorkItemStatus.Intake or CaseWorkItemStatus.NeedsAssignment
                ? CaseWorkItemStatus.Assigned
                : workItem.Status);
        var updated = workItem with
        {
            UpdatedAtUtc = timeProvider.GetUtcNow(),
            Status = status,
            AssignedDosimetristId = dosimetristId,
            AssignedDosimetristName = dosimetristName,
            AssignedPhysicistId = physicistId,
            AssignedPhysicistName = physicistName,
            AssignmentHistory = AppendHistory(workItem, CreateWorkItemHistoryEvent(
                status,
                auditContext,
                "assigned",
                dosimetristId,
                dosimetristName,
                physicistId,
                physicistName,
                request.Note))
        };
        var saved = store.SaveWorkItem(updated);
        Audit("work-item.assigned", auditContext, caseId: saved.CaseId, status: saved.Status.ToString(), details: saved.Id);
        return saved;
    }

    /// <summary>
    /// Updates the queue status for a case work item.
    /// </summary>
    public CaseWorkItem UpdateWorkItemStatus(string id, UpdateCaseWorkItemStatusRequest request, CiServerAuditContext? auditContext = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Work item id is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(request);

        var workItem = store.FindWorkItem(id) ?? throw new InvalidOperationException($"Work item '{id}' was not found.");
        var updated = workItem with
        {
            UpdatedAtUtc = timeProvider.GetUtcNow(),
            Status = request.Status,
            AssignmentHistory = AppendHistory(workItem, CreateWorkItemHistoryEvent(
                request.Status,
                auditContext,
                "status-changed",
                workItem.AssignedDosimetristId,
                workItem.AssignedDosimetristName,
                workItem.AssignedPhysicistId,
                workItem.AssignedPhysicistName,
                request.Note))
        };
        var saved = store.SaveWorkItem(updated);
        Audit("work-item.status-changed", auditContext, caseId: saved.CaseId, status: saved.Status.ToString(), details: saved.Id);
        return saved;
    }

    /// <summary>
    /// Lists registered rule packs.
    /// </summary>
    public IReadOnlyList<CiServerRulePackSummary> ListRulePacks()
    {
        return rulePacks.List()
            .Concat(store.ListRulePackVersions()
                .Where(version => version.IsActive)
                .Select(CreateManagedRulePackSummary))
            .OrderBy(summary => summary.Id, StringComparer.OrdinalIgnoreCase)
            .ThenBy(summary => summary.SourceKind, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Finds one registered rule pack.
    /// </summary>
    public CiServerRulePackDetail? FindRulePack(string id)
    {
        var managed = store.FindActiveRulePackVersion(id);
        if (managed is not null)
        {
            return CreateManagedRulePackDetail(managed);
        }

        return rulePacks.Find(id);
    }

    /// <summary>
    /// Validates a registered rule pack.
    /// </summary>
    public RulePackValidationReport ValidateRulePack(string id, CiServerAuditContext? auditContext = null)
    {
        var report = client.ValidateRulePack(LoadRulePack(id));
        Audit("rule-pack.validated", auditContext, status: report.IsValid ? "Valid" : "Invalid", details: $"{id}:{report.Fingerprint}");
        return report;
    }

    /// <summary>
    /// Runs regression tests for a registered rule pack.
    /// </summary>
    public RulePackTestReport TestRulePack(string id, RulePackTestServerRequest request, CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var report = client.TestRulePack(LoadRulePack(id), LoadRulePackTestCases(request.SyntheticCaseId));
        Audit("rule-pack.tested", auditContext, status: report.Passed ? "Passed" : "Failed", details: $"{id}:{report.RulePackVersion}");
        return report;
    }

    /// <summary>
    /// Imports a managed rule-pack version into CI-server storage.
    /// </summary>
    public CiServerRulePackImportResult ImportRulePack(RulePackImportServerRequest request, CiServerAuditContext? auditContext = null)
    {
        return ImportRulePack(request, auditContext, useCompleteSyntheticReadiness: false);
    }

    private CiServerRulePackImportResult ImportRulePack(
        RulePackImportServerRequest request,
        CiServerAuditContext? auditContext,
        bool useCompleteSyntheticReadiness)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rulePackId = NormalizeManagedRulePackId(request.RulePackId);
        var source = LoadRulePackImportSource(request);
        var rulePack = source.Bundle is null
            ? BeamKitRulePackLoader.FromJson(source.ManifestJson, source.BaseDirectory)
            : RulePackBundleLoader.ToRulePack(source.Bundle);
        var validation = client.ValidateRulePack(rulePack);
        var testReport = request.RunRegressionTests
            ? client.TestRulePack(rulePack, LoadRulePackTestCases(request.SyntheticCaseId, useCompleteSyntheticReadiness))
            : source.Bundle?.TestReport;
        var importedBy = CiServerText.Optional(request.ImportedBy) ?? auditContext?.Actor;
        var bundle = source.Bundle ?? new RulePackBundleBuilder(timeProvider).FromJson(
            source.ManifestJson,
            source.BaseDirectory,
            source.Source,
            importedBy,
            testReport);
        var version = new CiServerManagedRulePackVersion(
            rulePackId,
            CreateRulePackVersionId(validation.Fingerprint),
            timeProvider.GetUtcNow(),
            importedBy,
            source.SourceKind,
            source.Source,
            source.BaseDirectory,
            source.ManifestJson,
            rulePack.Name,
            rulePack.Version,
            rulePack.Owner,
            rulePack.Description,
            rulePack.DiseaseSite,
            rulePack.Tags,
            validation.Fingerprint,
            validation,
            testReport,
            bundleJson: RulePackBundleStore.ToJson(bundle));
        var saved = store.SaveRulePackVersion(version);
        Audit(
            "rule-pack.imported",
            auditContext,
            caseId: saved.RulePackId,
            status: validation.IsValid ? "Valid" : "Invalid",
            details: $"{saved.VersionId}:{saved.Fingerprint}");

        var activated = false;
        if (request.Promote)
        {
            saved = PromoteManagedRulePackVersion(
                saved.RulePackId,
                saved.VersionId,
                new RulePackPromotionServerRequest
                {
                    PromotedBy = importedBy,
                    Note = request.Note,
                    SafetyEvidence = request.SafetyEvidence
                },
                auditContext);
            activated = true;
        }

        return new CiServerRulePackImportResult(saved.ToSummary(), validation, testReport, activated);
    }

    /// <summary>
    /// Lists managed rule-pack versions.
    /// </summary>
    public IReadOnlyList<CiServerManagedRulePackVersionSummary> ListManagedRulePackVersions(string? rulePackId = null)
    {
        return store.ListRulePackVersions(rulePackId);
    }

    /// <summary>
    /// Finds one managed rule-pack version.
    /// </summary>
    public CiServerManagedRulePackVersionDetail? FindManagedRulePackVersion(string rulePackId, string versionId)
    {
        var version = store.FindRulePackVersion(rulePackId, versionId);
        return version is null ? null : new CiServerManagedRulePackVersionDetail(version);
    }

    /// <summary>
    /// Finds stored safety evidence for a managed rule-pack version.
    /// </summary>
    public ValidationEvidencePackage? FindManagedRulePackSafetyEvidence(string rulePackId, string versionId)
    {
        var version = store.FindRulePackVersion(rulePackId, versionId);
        return version is null ? null : DeserializeSafetyEvidence(version.SafetyEvidenceJson);
    }

    /// <summary>
    /// Reviews safety evidence for a managed rule-pack version without promoting it.
    /// </summary>
    public SafetyEvidenceReviewResult ReviewManagedRulePackSafetyEvidence(
        string rulePackId,
        string versionId,
        ValidationEvidencePackage evidence,
        CiServerAuditContext? auditContext = null)
    {
        var version = FindRequiredManagedRulePackVersion(rulePackId, versionId);
        var review = new SafetyEvidenceReviewer().ReviewRulePackPromotion(
            evidence,
            version.RulePackId,
            version.VersionId,
            version.Fingerprint);
        Audit(
            "rule-pack.safety-evidence.reviewed",
            auditContext,
            caseId: version.RulePackId,
            status: review.IsAcceptable ? "Acceptable" : "Blocked",
            details: $"{version.VersionId}:{review.BlockingFindings.Count}");
        return review;
    }

    /// <summary>
    /// Compares two managed rule-pack versions.
    /// </summary>
    public RulePackDiffReport CompareManagedRulePackVersions(
        string rulePackId,
        string oldVersionId,
        string newVersionId,
        CiServerAuditContext? auditContext = null)
    {
        var oldVersion = FindRequiredManagedRulePackVersion(rulePackId, oldVersionId);
        var newVersion = FindRequiredManagedRulePackVersion(rulePackId, newVersionId);
        var report = new RulePackDiffer().Compare(
            RulePackManifestStore.FromJson(oldVersion.ManifestJson),
            LoadManagedRulePack(oldVersion),
            RulePackManifestStore.FromJson(newVersion.ManifestJson),
            LoadManagedRulePack(newVersion));
        Audit(
            "rule-pack.version.diffed",
            auditContext,
            caseId: rulePackId,
            status: report.HasPolicyRelevantChanges ? "Changed" : "Unchanged",
            details: $"{oldVersionId}->{newVersionId}:{report.PolicyRelevantCount}");
        return report;
    }

    /// <summary>
    /// Reviews a draft rule pack against the active version without importing it.
    /// </summary>
    public CiServerRulePackDraftReviewResult ReviewRulePackDraft(
        string rulePackId,
        RulePackImportServerRequest request,
        CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedRulePackId = CiServerText.Required(rulePackId, nameof(rulePackId));
        var draftSource = LoadRulePackImportSource(request);
        var draftManifest = RulePackManifestStore.FromJson(draftSource.ManifestJson);
        var draftRulePack = draftSource.Bundle is null
            ? BeamKitRulePackLoader.FromJson(draftSource.ManifestJson, draftSource.BaseDirectory)
            : RulePackBundleLoader.ToRulePack(draftSource.Bundle);
        var activeVersion = store.FindActiveRulePackVersion(normalizedRulePackId);
        var baselineManifest = activeVersion is null ? null : RulePackManifestStore.FromJson(activeVersion.ManifestJson);
        var baselineRulePack = activeVersion is null
            ? LoadRulePack(normalizedRulePackId)
            : LoadManagedRulePack(activeVersion);
        var baselineVersionId = activeVersion?.VersionId ?? "registered-active";
        var validation = client.ValidateRulePack(draftRulePack);
        var testReport = request.RunRegressionTests
            ? client.TestRulePack(draftRulePack, LoadRulePackTestCases(request.SyntheticCaseId))
            : null;
        var diff = new RulePackDiffer().Compare(baselineManifest, baselineRulePack, draftManifest, draftRulePack);
        var result = new CiServerRulePackDraftReviewResult(normalizedRulePackId, baselineVersionId, validation, testReport, diff);
        Audit(
            "rule-pack.draft.reviewed",
            auditContext,
            caseId: normalizedRulePackId,
            status: result.IsPromotable ? "Promotable" : "Blocked",
            details: $"{baselineVersionId}->{validation.Fingerprint}:{diff.PolicyRelevantCount}");
        return result;
    }

    /// <summary>
    /// Revalidates a managed rule-pack version.
    /// </summary>
    public RulePackValidationReport ValidateManagedRulePackVersion(string rulePackId, string versionId, CiServerAuditContext? auditContext = null)
    {
        var version = FindRequiredManagedRulePackVersion(rulePackId, versionId);
        var rulePack = LoadManagedRulePack(version);
        var report = client.ValidateRulePack(rulePack);
        var saved = store.SaveRulePackVersion(version with { ValidationReport = report });
        Audit(
            "rule-pack.version.validated",
            auditContext,
            caseId: saved.RulePackId,
            status: report.IsValid ? "Valid" : "Invalid",
            details: $"{saved.VersionId}:{report.Fingerprint}");
        return report;
    }

    /// <summary>
    /// Runs regression tests for a managed rule-pack version and stores the report.
    /// </summary>
    public RulePackTestReport TestManagedRulePackVersion(
        string rulePackId,
        string versionId,
        RulePackVersionTestServerRequest request,
        CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var version = FindRequiredManagedRulePackVersion(rulePackId, versionId);
        var rulePack = LoadManagedRulePack(version);
        var report = client.TestRulePack(rulePack, LoadRulePackTestCases(request.SyntheticCaseId));
        var saved = store.SaveRulePackVersion(version with { TestReport = report });
        Audit(
            "rule-pack.version.tested",
            auditContext,
            caseId: saved.RulePackId,
            status: report.Passed ? "Passed" : "Failed",
            details: $"{saved.VersionId}:{report.PassedCount}/{report.Results.Count}");
        return report;
    }

    /// <summary>
    /// Promotes a managed rule-pack version as active.
    /// </summary>
    public CiServerManagedRulePackVersion PromoteManagedRulePackVersion(
        string rulePackId,
        string versionId,
        RulePackPromotionServerRequest request,
        CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var version = FindRequiredManagedRulePackVersion(rulePackId, versionId);
        if (!version.ValidationReport.IsValid)
        {
            throw new InvalidOperationException($"Rule pack version '{rulePackId}/{versionId}' cannot be promoted because validation has {version.ValidationReport.ErrorCount} error(s).");
        }

        if (version.TestReport is null)
        {
            throw new InvalidOperationException($"Rule pack version '{rulePackId}/{versionId}' cannot be promoted before regression tests pass.");
        }

        if (!version.TestReport.Passed)
        {
            throw new InvalidOperationException($"Rule pack version '{rulePackId}/{versionId}' cannot be promoted because {version.TestReport.FailedCount} regression test(s) failed.");
        }

        LoadManagedRulePack(version);
        var safetyEvidence = request.SafetyEvidence ?? DeserializeSafetyEvidence(version.SafetyEvidenceJson);
        if (safetyEvidence is null)
        {
            throw new InvalidOperationException($"Rule pack version '{rulePackId}/{versionId}' cannot be promoted without safety and validation evidence.");
        }

        var safetyReview = new SafetyEvidenceReviewer().ReviewRulePackPromotion(
            safetyEvidence,
            version.RulePackId,
            version.VersionId,
            version.Fingerprint);
        if (!safetyReview.IsAcceptable)
        {
            var details = string.Join("; ", safetyReview.BlockingFindings.Select(finding => $"{finding.Code}: {finding.Message}"));
            throw new InvalidOperationException($"Rule pack version '{rulePackId}/{versionId}' cannot be promoted because safety evidence is incomplete: {details}");
        }

        version = store.SaveRulePackVersion(version with
        {
            SafetyEvidenceJson = SerializeSafetyEvidence(safetyEvidence)
        });
        var promotedBy = CiServerText.Optional(request.PromotedBy) ?? auditContext?.Actor;
        var promoted = store.PromoteRulePackVersion(
            version.RulePackId,
            version.VersionId,
            timeProvider.GetUtcNow(),
            promotedBy,
            request.Note);
        Audit(
            "rule-pack.version.promoted",
            auditContext,
            caseId: promoted.RulePackId,
            status: "Active",
            details: $"{promoted.VersionId}:{promoted.Fingerprint}");
        return promoted;
    }

    /// <summary>
    /// Extracts and validates RT-PX protocol intent from a Word document upload.
    /// </summary>
    public RtpxWordAuthoringServerResult ExtractRtpxWordProtocol(
        RtpxWordAuthoringServerRequest request,
        CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authoringId = CreateRtpxWordAuthoringId();
        var createdAtUtc = timeProvider.GetUtcNow();
        var outputDirectory = ResolveRtpxWordAuthoringOutputDirectory(request.OutputDirectory, authoringId);
        Directory.CreateDirectory(outputDirectory);

        var source = ResolveRtpxWordDocxSource(request, outputDirectory);
        var extraction = new RtpxWordProtocolExtractor().Extract(source.Path);
        string? rtpxJson = null;
        if (extraction.Package is not null)
        {
            rtpxJson = RadiotherapyProtocolPackageStore.ToJson(extraction.Package);
            File.WriteAllText(Path.Combine(outputDirectory, "rtpx.json"), rtpxJson);
        }

        string? packageBase64 = null;
        string? packageFileName = null;
        string? packageFingerprint = null;
        if (request.GeneratePackage && extraction.IsValid && extraction.Package is not null)
        {
            packageFileName = $"{Slug(extraction.Package.Id)}.rtpx.zip";
            var packagePath = Path.Combine(outputDirectory, packageFileName);
            var packageResult = new RtpxWordPackageStore().Create(
                source.Path,
                packagePath,
                request.IncludeSourceDocument,
                overwrite: true);
            if (packageResult.WrotePackage)
            {
                var packageBytes = File.ReadAllBytes(packagePath);
                packageBase64 = Convert.ToBase64String(packageBytes);
                packageFingerprint = HashBytes(packageBytes);
            }
        }

        Audit(
            "rtpx.word.extracted",
            auditContext,
            runId: authoringId,
            caseId: extraction.Package?.Id,
            status: extraction.IsValid ? "Valid" : "Invalid",
            details: $"{source.Fingerprint}:{extraction.ErrorCount}/{extraction.WarningCount}");

        return new RtpxWordAuthoringServerResult(
            authoringId,
            createdAtUtc,
            source.FileName,
            source.Fingerprint,
            outputDirectory,
            extraction,
            rtpxJson,
            packageBase64,
            packageFileName,
            packageFingerprint);
    }

    /// <summary>
    /// Extracts a Word-authored RT-PX protocol and publishes it as a draft managed rule-pack version.
    /// </summary>
    public RtpxWordDraftPublishServerResult PublishRtpxWordDraft(
        RtpxWordDraftPublishServerRequest request,
        CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var publishId = CreateRtpxWordAuthoringId();
        var createdAtUtc = timeProvider.GetUtcNow();
        var outputDirectory = ResolveRtpxWordAuthoringOutputDirectory(request.OutputDirectory, publishId);
        Directory.CreateDirectory(outputDirectory);

        var source = ResolveRtpxWordDocxSource(request, outputDirectory);
        var extraction = new RtpxWordProtocolExtractor().Extract(source.Path);
        if (!extraction.IsValid || extraction.Package is null)
        {
            Audit(
                "rtpx.word-draft.rejected",
                auditContext,
                runId: publishId,
                caseId: extraction.Package?.Id,
                status: "Invalid",
                details: $"{source.Fingerprint}:{extraction.ErrorCount}/{extraction.WarningCount}");
            return new RtpxWordDraftPublishServerResult(
                publishId,
                createdAtUtc,
                source.FileName,
                source.Fingerprint,
                extraction,
                Acceptance: null,
                RulePackImport: null,
                SafetyEvidence: null,
                SafetyReview: null,
                ProtocolDiff: null,
                DashboardUrl: null);
        }

        var packageFileName = $"{Slug(extraction.Package.Id)}.rtpx.zip";
        var packagePath = Path.Combine(outputDirectory, packageFileName);
        var packageResult = new RtpxWordPackageStore().Create(
            source.Path,
            packagePath,
            request.IncludeSourceDocument,
            overwrite: true);
        if (!packageResult.WrotePackage)
        {
            throw new InvalidOperationException($"RT-PX draft package '{packagePath}' was not written.");
        }

        var acceptanceRequest = new RtpxAcceptanceServerRequest
        {
            PackagePath = packagePath,
            InstitutionProfilePath = request.InstitutionProfilePath,
            InstitutionProfile = request.InstitutionProfile,
            InstitutionProfileJson = ResolveDraftInstitutionProfileJson(request, extraction.Package),
            RulePackId = request.RulePackId,
            ImportedBy = CiServerText.Optional(request.ImportedBy) ?? auditContext?.Actor,
            RunRegressionTests = request.RunRegressionTests,
            SyntheticCaseId = request.SyntheticCaseId,
            Promote = false,
            Note = CiServerText.Optional(request.Note) ?? "Published as a draft from BeamKit Word authoring.",
            OutputDirectory = Path.Combine(outputDirectory, "acceptance"),
            Overwrite = true
        };
        var acceptance = AcceptRtpxPackage(acceptanceRequest, auditContext);
        var rulePackId = acceptance.RulePackImport?.Version.RulePackId ?? acceptance.Acceptance.RulePackId ?? acceptance.Report.LocalPackage.Id;
        var diff = CompareRtpxProtocolDraft(rulePackId, acceptance.Report.LocalPackage);

        Audit(
            "rtpx.word-draft.published",
            auditContext,
            runId: acceptance.Acceptance.Id,
            caseId: rulePackId,
            status: acceptance.Acceptance.Accepted ? "Draft" : "Rejected",
            details: $"{acceptance.Acceptance.PackageFingerprint}:{diff.ChangeCount}");

        return new RtpxWordDraftPublishServerResult(
            publishId,
            createdAtUtc,
            source.FileName,
            source.Fingerprint,
            extraction,
            acceptance.Acceptance,
            acceptance.RulePackImport,
            acceptance.SafetyEvidence,
            acceptance.SafetyReview,
            diff,
            $"/#rtpx-draft-{acceptance.Acceptance.Id}");
    }

    /// <summary>
    /// Accepts a portable RT-PX package, persists acceptance evidence, imports the generated rule pack, and optionally promotes it.
    /// </summary>
    public RtpxAcceptanceServerResult AcceptRtpxPackage(
        RtpxAcceptanceServerRequest request,
        CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var acceptanceId = CreateRtpxAcceptanceId();
        var createdAtUtc = timeProvider.GetUtcNow();
        var outputDirectory = ResolveRtpxAcceptanceOutputDirectory(request.OutputDirectory, acceptanceId);
        Directory.CreateDirectory(outputDirectory);

        var packageSource = ResolveRtpxPackageSource(request, outputDirectory);
        var profileSource = ResolveRtpxInstitutionProfile(request);
        var esapiSource = ResolveOptionalRtpxEsapiSnapshot(request);

        var report = new RtpxPackageAcceptanceEngine().Accept(new RtpxAcceptanceRequest(
            packageSource.Path,
            profileSource.Profile,
            outputDirectory,
            esapiSource?.Snapshot,
            esapiSource?.Path,
            request.Overwrite,
            createdAtUtc));
        var reportJson = RtpxAcceptanceReportWriter.ToJson(report);
        var record = SaveRtpxAcceptanceRecord(
            acceptanceId,
            createdAtUtc,
            report,
            packageSource,
            profileSource,
            esapiSource,
            reportJson,
            rulePackId: null,
            versionId: null,
            promoted: false,
            safetyEvidenceJson: null);
        Audit(
            "rtpx.acceptance.created",
            auditContext,
            runId: record.Id,
            caseId: record.LocalProtocolId,
            status: report.IsAccepted ? "Accepted" : "Rejected",
            details: $"{record.PackageFingerprint}:{record.ErrorCount}/{record.WarningCount}");

        CiServerRulePackImportResult? importResult = null;
        ValidationEvidencePackage? safetyEvidence = null;
        SafetyEvidenceReviewResult? safetyReview = null;
        CiServerManagedRulePackVersionSummary? promotedVersion = null;

        if (!report.IsAccepted)
        {
            return new RtpxAcceptanceServerResult(
                new CiServerRtpxAcceptanceSummary(record),
                report,
                importResult,
                promotedVersion,
                safetyEvidence,
                safetyReview);
        }

        var rulePackManifestPath = Path.Combine(outputDirectory, "rule-pack", "beamkit-rule-pack.json");
        if (!File.Exists(rulePackManifestPath))
        {
            throw new InvalidOperationException($"Accepted RT-PX package did not produce expected rule-pack manifest '{rulePackManifestPath}'.");
        }

        var importedBy = CiServerText.Optional(request.ImportedBy) ?? auditContext?.Actor;
        importResult = ImportRulePack(
            new RulePackImportServerRequest
            {
                RulePackId = CiServerText.Optional(request.RulePackId) ?? report.LocalPackage.Id,
                ManifestPath = rulePackManifestPath,
                ImportedBy = importedBy,
                RunRegressionTests = request.RunRegressionTests,
                SyntheticCaseId = request.SyntheticCaseId,
                Source = $"rtpx-acceptance:{acceptanceId}",
                Promote = false,
                Note = request.Note
            },
            auditContext,
            useCompleteSyntheticReadiness: true);
        safetyEvidence = CreateRtpxAcceptanceSafetyEvidence(
            acceptanceId,
            report,
            profileSource.Profile,
            outputDirectory,
            importResult.Version,
            importResult.TestReport,
            esapiSource is not null);
        var importedVersion = FindRequiredManagedRulePackVersion(importResult.Version.RulePackId, importResult.Version.VersionId);
        store.SaveRulePackVersion(importedVersion with { SafetyEvidenceJson = SerializeSafetyEvidence(safetyEvidence) });
        safetyReview = new SafetyEvidenceReviewer().ReviewRulePackPromotion(
            safetyEvidence,
            importResult.Version.RulePackId,
            importResult.Version.VersionId,
            importResult.Version.Fingerprint);
        record = SaveRtpxAcceptanceRecord(
            acceptanceId,
            createdAtUtc,
            report,
            packageSource,
            profileSource,
            esapiSource,
            reportJson,
            importResult.Version.RulePackId,
            importResult.Version.VersionId,
            promoted: false,
            SerializeSafetyEvidence(safetyEvidence));
        Audit(
            "rtpx.acceptance.rule-pack-imported",
            auditContext,
            runId: record.Id,
            caseId: importResult.Version.RulePackId,
            status: importResult.Validation.IsValid ? "Valid" : "Invalid",
            details: $"{importResult.Version.VersionId}:{importResult.Version.Fingerprint}");

        if (request.Promote)
        {
            try
            {
                var promoted = PromoteManagedRulePackVersion(
                    importResult.Version.RulePackId,
                    importResult.Version.VersionId,
                    new RulePackPromotionServerRequest
                    {
                        PromotedBy = importedBy,
                        Note = request.Note,
                        SafetyEvidence = safetyEvidence
                    },
                    auditContext);
                promotedVersion = promoted.ToSummary();
                record = SaveRtpxAcceptanceRecord(
                    acceptanceId,
                    createdAtUtc,
                    report,
                    packageSource,
                    profileSource,
                    esapiSource,
                    reportJson,
                    importResult.Version.RulePackId,
                    importResult.Version.VersionId,
                    promoted: true,
                    SerializeSafetyEvidence(safetyEvidence));
                Audit(
                    "rtpx.acceptance.promoted",
                    auditContext,
                    runId: record.Id,
                    caseId: promoted.RulePackId,
                    status: "Active",
                    details: promoted.VersionId);
            }
            catch (InvalidOperationException)
            {
                Audit(
                    "rtpx.acceptance.promotion-blocked",
                    auditContext,
                    runId: record.Id,
                    caseId: importResult.Version.RulePackId,
                    status: "Blocked",
                    details: importResult.Version.VersionId);
                throw;
            }
        }

        return new RtpxAcceptanceServerResult(
            new CiServerRtpxAcceptanceSummary(record),
            report,
            importResult,
            promotedVersion,
            safetyEvidence,
            safetyReview);
    }

    /// <summary>
    /// Lists recent RT-PX acceptance records.
    /// </summary>
    public IReadOnlyList<CiServerRtpxAcceptanceSummary> ListRtpxAcceptances(int limit = 50)
    {
        return store.ListRtpxAcceptances(limit);
    }

    /// <summary>
    /// Finds one RT-PX acceptance record.
    /// </summary>
    public CiServerRtpxAcceptanceDetail? FindRtpxAcceptance(string id)
    {
        var record = store.FindRtpxAcceptance(id);
        return record is null ? null : new CiServerRtpxAcceptanceDetail(record);
    }

    /// <summary>
    /// Runs an active RT-PX protocol compliance check against a synthetic case or uploaded plan snapshot.
    /// </summary>
    public ProtocolComplianceRunRecord CreateProtocolComplianceRun(
        ProtocolComplianceRunRequest request,
        CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var uploaded = LoadProtocolCompliancePlan(request);
        var binding = ResolveProtocolComplianceBinding(request);
        var rulePack = LoadManagedRulePack(binding.Version);
        var inputSource = CiServerText.Optional(request.InputSource) ?? uploaded.DefaultInputSource;
        var checkReport = new BeamKitCheckEngine(timeProvider).Evaluate(new BeamKitCheckRequest(
            uploaded.Plan,
            rulePack,
            CreateCompleteSyntheticReadinessInput(uploaded.Plan),
            inputSource: inputSource));
        var findings = CreateProtocolComplianceFindings(checkReport);
        var acceptedVariances = CreateProtocolComplianceVariances(request.Variances, findings, auditContext);
        var runId = CreateProtocolComplianceRunId();
        var createdAtUtc = timeProvider.GetUtcNow();
        var report = new ProtocolComplianceReport(
            runId,
            createdAtUtc,
            uploaded.Plan.Id,
            uploaded.Plan.Patient.Id,
            uploaded.Plan.CourseId,
            uploaded.Plan.DiseaseSite ?? checkReport.DiseaseSite ?? binding.Protocol.DiseaseSite,
            uploaded.InputKind.ToString(),
            inputSource,
            binding.Version.RulePackId,
            binding.Version.VersionId,
            binding.Acceptance.Id,
            binding.Protocol.Id,
            binding.Protocol.Name,
            binding.Protocol.Version,
            binding.Acceptance.PackageFingerprint,
            findings,
            acceptedVariances,
            checkReport);
        var saved = store.SaveProtocolComplianceRun(CreateProtocolComplianceRunRecord(
            report,
            BeamKitPlanJson.ToJson(uploaded.Plan)));
        Audit(
            "protocol-compliance.created",
            auditContext,
            runId: saved.Id,
            caseId: saved.PlanId,
            status: saved.Status.ToString(),
            details: $"{saved.ProtocolId}:{saved.RulePackId}/{saved.VersionId}");
        return saved;
    }

    /// <summary>
    /// Lists recent active protocol compliance runs.
    /// </summary>
    public IReadOnlyList<ProtocolComplianceRunSummary> ListProtocolComplianceRuns(int limit = 50)
    {
        return store.ListProtocolComplianceRuns(limit);
    }

    /// <summary>
    /// Finds one active protocol compliance run with its export artifacts.
    /// </summary>
    public ProtocolComplianceRunDetail? FindProtocolComplianceRun(string id)
    {
        var record = store.FindProtocolComplianceRun(id);
        return record is null ? null : new ProtocolComplianceRunDetail(record);
    }

    /// <summary>
    /// Finds the serialized compliance report JSON for a stored run.
    /// </summary>
    public string? FindProtocolComplianceReportJson(string id)
    {
        return store.FindProtocolComplianceRun(id)?.ReportJson;
    }

    /// <summary>
    /// Finds the Markdown compliance packet for a stored run.
    /// </summary>
    public string? FindProtocolComplianceMarkdownReport(string id)
    {
        return store.FindProtocolComplianceRun(id)?.MarkdownReport;
    }

    /// <summary>
    /// Accepts or replaces a variance for one blocking protocol compliance finding.
    /// </summary>
    public ProtocolComplianceRunDetail AcceptProtocolComplianceVariance(
        string id,
        ProtocolComplianceVarianceRequest request,
        CiServerAuditContext? auditContext = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Protocol compliance run id is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(request);
        var record = store.FindProtocolComplianceRun(id)
            ?? throw new InvalidOperationException($"Protocol compliance run '{id}' was not found.");
        var report = ProtocolComplianceReportWriter.FromJson(record.ReportJson);
        var variance = CreateProtocolComplianceVariance(request, report.Findings, auditContext);
        var variances = report.AcceptedVariances
            .Where(existing => !string.Equals(existing.FindingId, variance.FindingId, StringComparison.OrdinalIgnoreCase))
            .Concat(new[] { variance })
            .OrderBy(existing => existing.FindingId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var updatedReport = new ProtocolComplianceReport(
            report.RunId,
            report.CreatedAtUtc,
            report.PlanId,
            report.PatientId,
            report.CourseId,
            report.DiseaseSite,
            report.InputKind,
            report.InputSource,
            report.RulePackId,
            report.VersionId,
            report.RtpxAcceptanceId,
            report.ProtocolId,
            report.ProtocolName,
            report.ProtocolVersion,
            report.PackageFingerprint,
            report.Findings,
            variances,
            report.CheckReport);
        var saved = store.SaveProtocolComplianceRun(CreateProtocolComplianceRunRecord(updatedReport, record.PlanSnapshotJson));
        Audit(
            "protocol-compliance.variance-accepted",
            auditContext,
            runId: saved.Id,
            caseId: saved.PlanId,
            status: saved.Status.ToString(),
            details: variance.FindingId);
        return new ProtocolComplianceRunDetail(saved);
    }

    /// <summary>
    /// Lists RT-PX draft review records.
    /// </summary>
    public IReadOnlyList<RtpxDraftReviewSummary> ListRtpxDraftReviews(int limit = 50)
    {
        return store.ListRtpxAcceptances(Math.Clamp(limit, 1, 500))
            .Select(summary => store.FindRtpxAcceptance(summary.Id))
            .Where(record => record is not null && record.Accepted)
            .Select(record => CreateRtpxDraftReview(record!))
            .ToArray();
    }

    /// <summary>
    /// Finds one RT-PX draft review record.
    /// </summary>
    public RtpxDraftReviewSummary? FindRtpxDraftReview(string id)
    {
        var record = store.FindRtpxAcceptance(id);
        return record is null ? null : CreateRtpxDraftReview(record);
    }

    /// <summary>
    /// Marks an RT-PX draft as actively under review.
    /// </summary>
    public RtpxDraftReviewSummary StartRtpxDraftReview(
        string id,
        RtpxDraftReviewActionRequest request,
        CiServerAuditContext? auditContext = null)
    {
        return TransitionRtpxDraft(
            id,
            request,
            RtpxDraftReviewStatus.InReview,
            "rtpx.draft.review-started",
            auditContext);
    }

    /// <summary>
    /// Persists acknowledged protocol diff changes for an RT-PX draft.
    /// </summary>
    public RtpxDraftReviewSummary AcknowledgeRtpxDraftDiff(
        string id,
        RtpxDraftReviewActionRequest request,
        CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        var record = FindRequiredRtpxAcceptance(id);
        if (record.ReviewStatus == RtpxDraftReviewStatus.Promoted)
        {
            throw new InvalidOperationException($"RT-PX draft '{id}' has already been promoted and cannot acknowledge additional diff items.");
        }

        var review = CreateRtpxDraftReview(record);
        var requestedIds = request.DiffChangeIds is { Count: > 0 }
            ? request.DiffChangeIds
            : review.AcknowledgementRequiredChanges.Select(change => change.Id).ToArray();
        var validIds = review.ProtocolDiff.Changes.Select(change => change.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknownIds = requestedIds
            .Where(idValue => !validIds.Contains(idValue))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unknownIds.Length > 0)
        {
            throw new InvalidOperationException($"RT-PX draft '{id}' cannot acknowledge unknown diff change id(s): {string.Join(", ", unknownIds)}.");
        }

        var acknowledged = record.AcknowledgedDiffChangeIds
            .Concat(requestedIds)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var now = timeProvider.GetUtcNow();
        var updated = store.SaveRtpxAcceptance(record with
        {
            ReviewStatus = record.ReviewStatus == RtpxDraftReviewStatus.Draft
                ? RtpxDraftReviewStatus.InReview
                : record.ReviewStatus,
            ReviewUpdatedAtUtc = now,
            ReviewedBy = CiServerText.Optional(request.ReviewedBy) ?? auditContext?.Actor,
            ReviewNote = CiServerText.Optional(request.Note) ?? record.ReviewNote,
            DiffAcknowledgedBy = CiServerText.Optional(request.ReviewedBy) ?? auditContext?.Actor,
            DiffAcknowledgedAtUtc = now,
            AcknowledgedDiffChangeIds = acknowledged
        });
        Audit(
            "rtpx.draft.diff-acknowledged",
            auditContext,
            runId: updated.Id,
            caseId: updated.RulePackId,
            status: updated.ReviewStatus.ToString(),
            details: $"{acknowledged.Length}/{review.ProtocolDiff.Changes.Count}");
        return CreateRtpxDraftReview(updated);
    }

    /// <summary>
    /// Requests protocol changes for an RT-PX draft.
    /// </summary>
    public RtpxDraftReviewSummary RequestRtpxDraftChanges(
        string id,
        RtpxDraftReviewActionRequest request,
        CiServerAuditContext? auditContext = null)
    {
        return TransitionRtpxDraft(
            id,
            request,
            RtpxDraftReviewStatus.ChangesRequested,
            "rtpx.draft.changes-requested",
            auditContext);
    }

    /// <summary>
    /// Approves an RT-PX draft for promotion.
    /// </summary>
    public RtpxDraftReviewSummary ApproveRtpxDraft(
        string id,
        RtpxDraftReviewActionRequest request,
        CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        var record = FindRequiredRtpxAcceptance(id);
        var review = CreateRtpxDraftReview(record);
        if (!review.IsApprovable)
        {
            var pending = review.PendingAcknowledgementChanges.Count;
            throw new InvalidOperationException($"RT-PX draft '{id}' cannot be approved until it is accepted, valid, has safety evidence, and all review-relevant diff items are acknowledged. Pending diff items: {pending}.");
        }

        var now = timeProvider.GetUtcNow();
        var actor = CiServerText.Optional(request.ReviewedBy) ?? auditContext?.Actor;
        var updated = store.SaveRtpxAcceptance(record with
        {
            ReviewStatus = RtpxDraftReviewStatus.Approved,
            ReviewUpdatedAtUtc = now,
            ReviewedBy = actor,
            ReviewNote = CiServerText.Optional(request.Note) ?? record.ReviewNote,
            ApprovedBy = actor,
            ApprovedAtUtc = now,
            ApprovalNote = CiServerText.Optional(request.Note)
        });
        Audit(
            "rtpx.draft.approved",
            auditContext,
            runId: updated.Id,
            caseId: updated.RulePackId,
            status: updated.ReviewStatus.ToString(),
            details: updated.VersionId);
        return CreateRtpxDraftReview(updated);
    }

    /// <summary>
    /// Promotes an RT-PX draft's managed rule-pack version active.
    /// </summary>
    public RtpxDraftReviewSummary PromoteRtpxDraft(
        string id,
        RtpxDraftReviewActionRequest request,
        CiServerAuditContext? auditContext = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        var record = FindRequiredRtpxAcceptance(id);
        var review = CreateRtpxDraftReview(record);
        if (!review.IsPromotable)
        {
            throw new InvalidOperationException($"RT-PX draft '{id}' cannot be promoted until it is approved, valid, regression-tested, safety-evidenced, and its protocol diff is acknowledged.");
        }

        if (string.IsNullOrWhiteSpace(record.RulePackId) || string.IsNullOrWhiteSpace(record.VersionId))
        {
            throw new InvalidOperationException($"RT-PX draft '{id}' does not have an imported managed rule-pack version.");
        }

        var evidence = DeserializeSafetyEvidence(record.SafetyEvidenceJson)
            ?? throw new InvalidOperationException($"RT-PX draft '{id}' cannot be promoted without safety evidence.");
        PromoteManagedRulePackVersion(
            record.RulePackId,
            record.VersionId,
            new RulePackPromotionServerRequest
            {
                PromotedBy = CiServerText.Optional(request.ReviewedBy) ?? auditContext?.Actor,
                Note = CiServerText.Optional(request.Note) ?? "Promoted from RT-PX draft review.",
                SafetyEvidence = evidence
            },
            auditContext);

        var now = timeProvider.GetUtcNow();
        var promotedRecord = store.SaveRtpxAcceptance(record with
        {
            Promoted = true,
            ReviewStatus = RtpxDraftReviewStatus.Promoted,
            ReviewUpdatedAtUtc = now,
            ReviewedBy = CiServerText.Optional(request.ReviewedBy) ?? auditContext?.Actor,
            ReviewNote = CiServerText.Optional(request.Note) ?? record.ReviewNote
        });
        Audit(
            "rtpx.draft.promoted",
            auditContext,
            runId: promotedRecord.Id,
            caseId: promotedRecord.RulePackId,
            status: "Active",
            details: promotedRecord.VersionId);
        return CreateRtpxDraftReview(promotedRecord);
    }

    /// <summary>
    /// Rejects an RT-PX draft.
    /// </summary>
    public RtpxDraftReviewSummary RejectRtpxDraft(
        string id,
        RtpxDraftReviewActionRequest request,
        CiServerAuditContext? auditContext = null)
    {
        return TransitionRtpxDraft(
            id,
            request,
            RtpxDraftReviewStatus.Rejected,
            "rtpx.draft.rejected",
            auditContext);
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

    private UploadedPlan LoadProtocolCompliancePlan(ProtocolComplianceRunRequest request)
    {
        var planJson = GetJson(request.Plan, request.PlanJson);
        var esapiSnapshotJson = GetJson(request.EsapiSnapshot, request.EsapiSnapshotJson);
        var sourceCount = new[]
        {
            !string.IsNullOrWhiteSpace(request.SyntheticCaseId),
            !string.IsNullOrWhiteSpace(planJson),
            !string.IsNullOrWhiteSpace(esapiSnapshotJson)
        }.Count(value => value);
        if (sourceCount != 1)
        {
            throw new ArgumentException("Protocol compliance requires exactly one of 'syntheticCaseId', 'plan'/'planJson', or 'esapiSnapshot'/'esapiSnapshotJson'.", nameof(request));
        }

        if (!string.IsNullOrWhiteSpace(request.SyntheticCaseId))
        {
            var clinicalCase = SyntheticClinicalCaseLibrary.Find(request.SyntheticCaseId);
            return new UploadedPlan(clinicalCase.Plan, CiRunInputKind.SyntheticCase, $"case:{clinicalCase.Id}");
        }

        return LoadUploadedPlan(new HostedCiRunUploadRequest
        {
            Format = request.Format,
            Plan = request.Plan,
            PlanJson = request.PlanJson,
            EsapiSnapshot = request.EsapiSnapshot,
            EsapiSnapshotJson = request.EsapiSnapshotJson
        });
    }

    private ProtocolComplianceBinding ResolveProtocolComplianceBinding(ProtocolComplianceRunRequest request)
    {
        var acceptanceId = CiServerText.Optional(request.RtpxAcceptanceId);
        if (acceptanceId is not null)
        {
            var acceptance = FindRequiredRtpxAcceptance(acceptanceId);
            EnsureProtocolComplianceAcceptanceIsActive(acceptance);
            if (!string.IsNullOrWhiteSpace(request.RulePackId)
                && !string.Equals(acceptance.RulePackId, request.RulePackId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"RT-PX acceptance '{acceptance.Id}' is bound to rule pack '{acceptance.RulePackId}', not '{request.RulePackId}'.");
            }

            if (!string.IsNullOrWhiteSpace(request.VersionId)
                && !string.Equals(acceptance.VersionId, request.VersionId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"RT-PX acceptance '{acceptance.Id}' is bound to version '{acceptance.VersionId}', not '{request.VersionId}'.");
            }

            var boundVersion = FindRequiredManagedRulePackVersion(acceptance.RulePackId!, acceptance.VersionId!);
            return new ProtocolComplianceBinding(acceptance, boundVersion, DeserializeRtpxAcceptanceReport(acceptance.ReportJson).LocalPackage);
        }

        var rulePackId = CiServerText.Required(request.RulePackId, nameof(request.RulePackId));
        var version = string.IsNullOrWhiteSpace(request.VersionId)
            ? store.FindActiveRulePackVersion(rulePackId)
                ?? throw new InvalidOperationException($"Rule pack '{rulePackId}' does not have an active managed version.")
            : FindRequiredManagedRulePackVersion(rulePackId, request.VersionId.Trim());
        if (!version.IsActive)
        {
            throw new InvalidOperationException($"Rule pack version '{version.RulePackId}/{version.VersionId}' is not active.");
        }

        var activeAcceptance = FindRtpxAcceptanceForRulePackVersion(version.RulePackId, version.VersionId)
            ?? throw new InvalidOperationException($"Rule pack version '{version.RulePackId}/{version.VersionId}' does not have an accepted RT-PX provenance record.");
        EnsureProtocolComplianceAcceptanceIsActive(activeAcceptance);
        return new ProtocolComplianceBinding(activeAcceptance, version, DeserializeRtpxAcceptanceReport(activeAcceptance.ReportJson).LocalPackage);
    }

    private void EnsureProtocolComplianceAcceptanceIsActive(CiServerRtpxAcceptanceRecord acceptance)
    {
        if (!acceptance.Accepted)
        {
            throw new InvalidOperationException($"RT-PX acceptance '{acceptance.Id}' was rejected and cannot be used for protocol compliance.");
        }

        if (string.IsNullOrWhiteSpace(acceptance.RulePackId) || string.IsNullOrWhiteSpace(acceptance.VersionId))
        {
            throw new InvalidOperationException($"RT-PX acceptance '{acceptance.Id}' does not have an imported managed rule-pack version.");
        }

        var activeVersion = store.FindActiveRulePackVersion(acceptance.RulePackId);
        if (!string.Equals(activeVersion?.VersionId, acceptance.VersionId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"RT-PX acceptance '{acceptance.Id}' is not the active promoted protocol for rule pack '{acceptance.RulePackId}'.");
        }

        if (acceptance.ReviewStatus is not RtpxDraftReviewStatus.Promoted && !acceptance.Promoted)
        {
            throw new InvalidOperationException($"RT-PX acceptance '{acceptance.Id}' has not completed draft review promotion.");
        }
    }

    private CiServerRtpxAcceptanceRecord? FindRtpxAcceptanceForRulePackVersion(string rulePackId, string versionId)
    {
        return store.ListRtpxAcceptances(500)
            .Select(summary => store.FindRtpxAcceptance(summary.Id))
            .Where(record => record is not null && record.Accepted)
            .Select(record => record!)
            .Where(record => string.Equals(record.RulePackId, rulePackId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(record.VersionId, versionId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(record => record.CreatedAtUtc)
            .ThenBy(record => record.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private IReadOnlyList<ProtocolComplianceFinding> CreateProtocolComplianceFindings(BeamKitCheckReport report)
    {
        var findings = new List<ProtocolComplianceFinding>();
        foreach (var result in report.PlanCheckReport.Results)
        {
            findings.Add(new ProtocolComplianceFinding(
                CreateProtocolComplianceFindingId("PlanCheck", result.CheckId),
                "PlanCheck",
                result.CheckId,
                MapPlanCheckStatus(result.Status),
                result.Message,
                result.Severity.ToString(),
                FormatEvidence(result.Evidence),
                result.Reference));
        }

        foreach (var result in report.ClinicalGoalReport.Results)
        {
            findings.Add(new ProtocolComplianceFinding(
                CreateProtocolComplianceFindingId("ClinicalGoal", result.RuleId),
                "ClinicalGoal",
                result.RuleId,
                MapEvaluationStatus(result.Status),
                result.Message,
                result.Status == EvaluationStatus.Error ? "Error" : null,
                FormatClinicalGoalEvidence(result),
                result.StructureName));
        }

        if (report.NamingReport is not null)
        {
            foreach (var result in report.NamingReport.Results)
            {
                findings.Add(new ProtocolComplianceFinding(
                    CreateProtocolComplianceFindingId("StructureNaming", result.OriginalName),
                    "StructureNaming",
                    result.OriginalName,
                    MapNamingStatus(result.Status),
                    result.Message,
                    result.Status.ToString(),
                    FormatNamingEvidence(result),
                    result.CanonicalName));
            }

            foreach (var missing in report.NamingReport.MissingStructures)
            {
                findings.Add(new ProtocolComplianceFinding(
                    CreateProtocolComplianceFindingId("MissingStructure", missing.CanonicalName),
                    "MissingStructure",
                    missing.CanonicalName,
                    ProtocolComplianceStatus.Fail,
                    missing.Message,
                    "Failure"));
            }
        }

        foreach (var item in report.ReadinessState.Items)
        {
            findings.Add(new ProtocolComplianceFinding(
                CreateProtocolComplianceFindingId("Readiness", item.Key),
                "Readiness",
                item.Key,
                MapReadinessStatus(item.Status),
                string.IsNullOrWhiteSpace(item.Details) ? item.Label : $"{item.Label}: {item.Details}",
                item.Status.ToString(),
                item.Details,
                item.Label));
        }

        if (report.WriteUpManifest is not null)
        {
            foreach (var item in report.WriteUpManifest.Checklist)
            {
                findings.Add(new ProtocolComplianceFinding(
                    CreateProtocolComplianceFindingId("WriteUp", item.Key),
                    "WriteUp",
                    item.Key,
                    MapReadinessStatus(item.Status),
                    string.IsNullOrWhiteSpace(item.Details) ? item.Label : $"{item.Label}: {item.Details}",
                    item.Status.ToString(),
                    item.Details,
                    item.Label));
            }
        }

        return findings
            .OrderBy(finding => finding.Section, StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.Subject, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<ProtocolComplianceVariance> CreateProtocolComplianceVariances(
        IReadOnlyList<ProtocolComplianceVarianceRequest>? requests,
        IReadOnlyList<ProtocolComplianceFinding> findings,
        CiServerAuditContext? auditContext)
    {
        if (requests is null || requests.Count == 0)
        {
            return Array.Empty<ProtocolComplianceVariance>();
        }

        return requests
            .Select(request => CreateProtocolComplianceVariance(request, findings, auditContext))
            .GroupBy(variance => variance.FindingId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .OrderBy(variance => variance.FindingId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private ProtocolComplianceVariance CreateProtocolComplianceVariance(
        ProtocolComplianceVarianceRequest request,
        IReadOnlyList<ProtocolComplianceFinding> findings,
        CiServerAuditContext? auditContext)
    {
        var findingId = CiServerText.Required(request.FindingId, nameof(request.FindingId));
        var finding = findings.FirstOrDefault(candidate => string.Equals(candidate.Id, findingId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Protocol compliance finding '{findingId}' was not found.");
        if (!finding.IsBlocking)
        {
            throw new InvalidOperationException($"Protocol compliance finding '{findingId}' is not blocking and does not require a variance.");
        }

        return new ProtocolComplianceVariance(
            finding.Id,
            CiServerText.Optional(request.AcceptedBy) ?? auditContext?.Actor ?? "unknown",
            timeProvider.GetUtcNow(),
            CiServerText.Required(request.Rationale, nameof(request.Rationale)));
    }

    private static ProtocolComplianceRunRecord CreateProtocolComplianceRunRecord(
        ProtocolComplianceReport report,
        string planSnapshotJson)
    {
        var reportJson = ProtocolComplianceReportWriter.ToJson(report);
        var markdownReport = ProtocolComplianceReportWriter.ToMarkdown(report);
        return new ProtocolComplianceRunRecord(
            report.RunId,
            report.CreatedAtUtc,
            report.Summary.Status,
            report.PlanId,
            report.PatientId,
            report.CourseId,
            report.DiseaseSite,
            Enum.Parse<CiRunInputKind>(report.InputKind),
            report.InputSource,
            report.RulePackId,
            report.VersionId,
            report.RtpxAcceptanceId,
            report.ProtocolId,
            report.ProtocolName,
            report.ProtocolVersion,
            report.PackageFingerprint,
            report.Summary.PassCount,
            report.Summary.WarningCount,
            report.Summary.FailCount,
            report.Summary.NotEvaluableCount,
            report.Summary.AcceptedVarianceCount,
            report.Summary.UnresolvedBlockingCount,
            reportJson,
            markdownReport,
            planSnapshotJson);
    }

    private static ProtocolComplianceStatus MapPlanCheckStatus(PlanCheckStatus status)
    {
        return status switch
        {
            PlanCheckStatus.Pass => ProtocolComplianceStatus.Pass,
            PlanCheckStatus.Warning => ProtocolComplianceStatus.Warning,
            PlanCheckStatus.Fail => ProtocolComplianceStatus.Fail,
            PlanCheckStatus.NotEvaluable => ProtocolComplianceStatus.NotEvaluable,
            _ => ProtocolComplianceStatus.NotEvaluable
        };
    }

    private static ProtocolComplianceStatus MapEvaluationStatus(EvaluationStatus status)
    {
        return status switch
        {
            EvaluationStatus.Pass => ProtocolComplianceStatus.Pass,
            EvaluationStatus.Warning => ProtocolComplianceStatus.Warning,
            EvaluationStatus.Fail => ProtocolComplianceStatus.Fail,
            EvaluationStatus.NotEvaluable or EvaluationStatus.Error => ProtocolComplianceStatus.NotEvaluable,
            _ => ProtocolComplianceStatus.NotEvaluable
        };
    }

    private static ProtocolComplianceStatus MapNamingStatus(NormalizationStatus status)
    {
        return status switch
        {
            NormalizationStatus.AlreadyCanonical => ProtocolComplianceStatus.Pass,
            NormalizationStatus.Normalized => ProtocolComplianceStatus.Warning,
            NormalizationStatus.Ambiguous or NormalizationStatus.Unmapped => ProtocolComplianceStatus.NotEvaluable,
            _ => ProtocolComplianceStatus.NotEvaluable
        };
    }

    private static ProtocolComplianceStatus MapReadinessStatus(ReadinessItemStatus status)
    {
        return status switch
        {
            ReadinessItemStatus.Complete or ReadinessItemStatus.NotApplicable => ProtocolComplianceStatus.Pass,
            ReadinessItemStatus.Pending => ProtocolComplianceStatus.NotEvaluable,
            ReadinessItemStatus.Blocked => ProtocolComplianceStatus.Fail,
            _ => ProtocolComplianceStatus.NotEvaluable
        };
    }

    private static string CreateProtocolComplianceFindingId(string section, string subject)
    {
        return $"{Slug(section)}:{Slug(subject)}";
    }

    private static string? FormatEvidence(IReadOnlyDictionary<string, string> evidence)
    {
        return evidence.Count == 0
            ? null
            : string.Join("; ", evidence.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static string? FormatClinicalGoalEvidence(EvaluationResult result)
    {
        var evidence = new List<string>();
        if (result.StructureName is not null)
        {
            evidence.Add($"structure={result.StructureName}");
        }

        if (result.ObservedValue.HasValue)
        {
            evidence.Add($"observed={result.ObservedValue.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        if (result.ExpectedValue.HasValue)
        {
            evidence.Add($"expected={result.ExpectedValue.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        if (result.Unit is not null)
        {
            evidence.Add($"unit={result.Unit}");
        }

        return evidence.Count == 0 ? null : string.Join("; ", evidence);
    }

    private static string? FormatNamingEvidence(StructureNameNormalizationResult result)
    {
        var evidence = new List<string>();
        if (result.CanonicalName is not null)
        {
            evidence.Add($"canonical={result.CanonicalName}");
        }

        if (result.Candidates.Count > 0)
        {
            evidence.Add($"candidates={string.Join(", ", result.Candidates)}");
        }

        evidence.Add($"confidence={result.Confidence}");
        evidence.Add($"source={result.Source}");
        return string.Join("; ", evidence);
    }

    private BeamKitRulePack LoadRulePack(string? rulePackId = null, string? rulePackPath = null)
    {
        if (!string.IsNullOrWhiteSpace(rulePackPath))
        {
            return rulePacks.Load(rulePackPath: rulePackPath);
        }

        if (!string.IsNullOrWhiteSpace(rulePackId))
        {
            var managed = store.FindActiveRulePackVersion(rulePackId);
            if (managed is not null)
            {
                return LoadManagedRulePack(managed);
            }
        }

        return rulePacks.Load(rulePackId);
    }

    private static BeamKitRulePack LoadManagedRulePack(CiServerManagedRulePackVersion version)
    {
        var rulePack = string.IsNullOrWhiteSpace(version.BundleJson)
            ? BeamKitRulePackLoader.FromJson(version.ManifestJson, version.BaseDirectory)
            : RulePackBundleLoader.ToRulePack(RulePackBundleStore.FromJson(version.BundleJson));
        var currentFingerprint = RulePackFingerprint.Compute(rulePack);
        if (!string.Equals(currentFingerprint, version.Fingerprint, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Managed rule pack version '{version.RulePackId}/{version.VersionId}' no longer matches its imported fingerprint. Expected {version.Fingerprint}; loaded {currentFingerprint}.");
        }

        return rulePack;
    }

    private static string SerializeSafetyEvidence(ValidationEvidencePackage evidence)
    {
        return System.Text.Json.JsonSerializer.Serialize(evidence, CiServerJson.Options);
    }

    private static ValidationEvidencePackage? DeserializeSafetyEvidence(string? json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? null
            : System.Text.Json.JsonSerializer.Deserialize<ValidationEvidencePackage>(json, CiServerJson.Options);
    }

    private CiServerRtpxAcceptanceRecord SaveRtpxAcceptanceRecord(
        string acceptanceId,
        DateTimeOffset createdAtUtc,
        RtpxAcceptanceReport report,
        RtpxPackageSource packageSource,
        RtpxInstitutionProfileSource profileSource,
        RtpxEsapiSnapshotSource? esapiSource,
        string reportJson,
        string? rulePackId,
        string? versionId,
        bool promoted,
        string? safetyEvidenceJson)
    {
        var record = new CiServerRtpxAcceptanceRecord(
            acceptanceId,
            createdAtUtc,
            report.Institution,
            packageSource.Path,
            report.OutputDirectory,
            report.IsAccepted,
            promoted,
            rulePackId,
            versionId,
            report.SourcePackage.Id,
            report.SourcePackage.Name,
            report.SourcePackage.Version,
            report.LocalPackage.Id,
            packageSource.Fingerprint,
            profileSource.Fingerprint,
            esapiSource?.Fingerprint,
            report.EsapiEvidence is not null,
            report.ErrorCount,
            report.WarningCount,
            reportJson,
            safetyEvidenceJson);
        return store.SaveRtpxAcceptance(record);
    }

    private RtpxDraftReviewSummary CreateRtpxDraftReview(CiServerRtpxAcceptanceRecord record)
    {
        var version = string.IsNullOrWhiteSpace(record.RulePackId) || string.IsNullOrWhiteSpace(record.VersionId)
            ? null
            : store.FindRulePackVersion(record.RulePackId, record.VersionId);
        var report = DeserializeRtpxAcceptanceReport(record.ReportJson);
        var rulePackId = record.RulePackId ?? report.LocalPackage.Id;
        var diff = CompareRtpxProtocolDraft(rulePackId, report.LocalPackage);
        return new RtpxDraftReviewSummary(
            new CiServerRtpxAcceptanceSummary(record),
            version?.ToSummary(),
            version?.ValidationReport,
            version?.TestReport,
            DeserializeSafetyEvidence(record.SafetyEvidenceJson),
            diff);
    }

    private CiServerRtpxAcceptanceRecord FindRequiredRtpxAcceptance(string id)
    {
        return store.FindRtpxAcceptance(id)
            ?? throw new InvalidOperationException($"RT-PX draft '{id}' was not found.");
    }

    private RtpxDraftReviewSummary TransitionRtpxDraft(
        string id,
        RtpxDraftReviewActionRequest request,
        RtpxDraftReviewStatus status,
        string auditAction,
        CiServerAuditContext? auditContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        var record = FindRequiredRtpxAcceptance(id);
        if (record.ReviewStatus == RtpxDraftReviewStatus.Promoted)
        {
            throw new InvalidOperationException($"RT-PX draft '{id}' has already been promoted and cannot transition to {status}.");
        }

        var now = timeProvider.GetUtcNow();
        var actor = CiServerText.Optional(request.ReviewedBy) ?? auditContext?.Actor;
        var updated = record with
        {
            ReviewStatus = status,
            ReviewUpdatedAtUtc = now,
            ReviewedBy = actor,
            ReviewNote = CiServerText.Optional(request.Note) ?? record.ReviewNote
        };
        if (status == RtpxDraftReviewStatus.Rejected)
        {
            updated = updated with
            {
                RejectedBy = actor,
                RejectedAtUtc = now,
                RejectionNote = CiServerText.Optional(request.Note)
            };
        }

        var saved = store.SaveRtpxAcceptance(updated);
        Audit(
            auditAction,
            auditContext,
            runId: saved.Id,
            caseId: saved.RulePackId,
            status: saved.ReviewStatus.ToString(),
            details: CiServerText.Optional(request.Note) ?? saved.VersionId);
        return CreateRtpxDraftReview(saved);
    }

    private bool IsRtpxAcceptanceActive(CiServerRtpxAcceptanceRecord? record)
    {
        if (record is null || string.IsNullOrWhiteSpace(record.RulePackId) || string.IsNullOrWhiteSpace(record.VersionId))
        {
            return false;
        }

        var activeVersion = store.FindActiveRulePackVersion(record.RulePackId);
        return string.Equals(activeVersion?.VersionId, record.VersionId, StringComparison.OrdinalIgnoreCase)
            || record.Promoted;
    }

    private RtpxProtocolDiffReport CompareRtpxProtocolDraft(string rulePackId, RadiotherapyProtocolPackage draft)
    {
        var activeRecord = FindActiveRtpxAcceptanceForRulePack(rulePackId);
        if (activeRecord is null)
        {
            return new RtpxProtocolDiffReport(
                draft.Id,
                ComparedToAcceptanceId: null,
                ComparedToVersionId: null,
                ComparedToFingerprint: null,
                new[]
                {
                    new RtpxProtocolDiffChange(
                        "Package",
                        draft.Id,
                        "Initial",
                        "Info",
                        "No active accepted RT-PX package exists for this rule pack.",
                        Before: null,
                        After: ProtocolPackageSummary(draft))
                });
        }

        var activeReport = DeserializeRtpxAcceptanceReport(activeRecord.ReportJson);
        var changes = new List<RtpxProtocolDiffChange>();
        AddMetadataChange(changes, "Name", activeReport.LocalPackage.Name, draft.Name, "Review");
        AddMetadataChange(changes, "Version", activeReport.LocalPackage.Version, draft.Version, "Review");
        AddMetadataChange(changes, "Disease Site", activeReport.LocalPackage.DiseaseSite, draft.DiseaseSite, "ClinicalImpact");
        AddMetadataChange(changes, "Intent", activeReport.LocalPackage.Intent, draft.Intent, "ClinicalImpact");
        AddMetadataChange(changes, "Status", activeReport.LocalPackage.Status.ToString(), draft.Status.ToString(), "Review");
        AddCollectionChanges(
            changes,
            "Structure",
            activeReport.LocalPackage.Structures,
            draft.Structures,
            item => item.Id,
            item => $"{item.Name} | {item.Role} | {item.Level} | contours={(item.MustHaveContours ? "yes" : "no")}",
            changedSeverity: "ClinicalImpact",
            removedSeverity: "Blocking");
        AddCollectionChanges(
            changes,
            "Prescription",
            activeReport.LocalPackage.Prescriptions,
            draft.Prescriptions,
            item => item.Id,
            item => $"{item.Target} | {item.TotalDoseGy} Gy | {item.FractionCount} fx | {item.Technique} | {item.Energy}",
            changedSeverity: "ClinicalImpact",
            removedSeverity: "Blocking");
        AddCollectionChanges(
            changes,
            "DoseConstraint",
            activeReport.LocalPackage.Constraints,
            draft.Constraints,
            item => item.Id,
            item => $"{item.Structure} {item.Metric} {item.Comparison} {item.Value} {item.Unit} | {item.Level} | active={(item.IsActive ? "yes" : "no")}",
            changedSeverity: "ClinicalImpact",
            removedSeverity: "Blocking");
        AddCollectionChanges(
            changes,
            "PlanCheck",
            activeReport.LocalPackage.PlanChecks,
            draft.PlanChecks,
            item => item.Id,
            item => $"{item.Title} | {item.Type} | {item.Level} | active={(item.IsActive ? "yes" : "no")}",
            changedSeverity: "Review",
            removedSeverity: "Review");
        AddCollectionChanges(
            changes,
            "Workflow",
            activeReport.LocalPackage.Workflow,
            draft.Workflow,
            item => item.Id,
            item => $"{item.Title} | {item.Type} | {item.Level} | active={(item.IsActive ? "yes" : "no")}",
            changedSeverity: "Review",
            removedSeverity: "Review");

        return new RtpxProtocolDiffReport(
            draft.Id,
            activeRecord.Id,
            activeRecord.VersionId,
            activeRecord.PackageFingerprint,
            changes);
    }

    private CiServerRtpxAcceptanceRecord? FindActiveRtpxAcceptanceForRulePack(string rulePackId)
    {
        var activeVersion = store.FindActiveRulePackVersion(rulePackId);
        var records = store.ListRtpxAcceptances(500)
            .Select(summary => store.FindRtpxAcceptance(summary.Id))
            .Where(record => record is not null && record.Accepted)
            .Select(record => record!)
            .Where(record => string.Equals(record.RulePackId, rulePackId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (activeVersion is not null)
        {
            var byActiveVersion = records.FirstOrDefault(record =>
                string.Equals(record.VersionId, activeVersion.VersionId, StringComparison.OrdinalIgnoreCase));
            if (byActiveVersion is not null)
            {
                return byActiveVersion;
            }
        }

        return records.FirstOrDefault(record => record.Promoted);
    }

    private static void AddMetadataChange(
        ICollection<RtpxProtocolDiffChange> changes,
        string key,
        string? before,
        string? after,
        string severity)
    {
        if (string.Equals(before, after, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        changes.Add(new RtpxProtocolDiffChange(
            "Metadata",
            key,
            "Changed",
            severity,
            $"{key} changed.",
            before,
            after));
    }

    private static void AddCollectionChanges<T>(
        ICollection<RtpxProtocolDiffChange> changes,
        string category,
        IReadOnlyList<T> before,
        IReadOnlyList<T> after,
        Func<T, string> keySelector,
        Func<T, string> describe,
        string changedSeverity,
        string removedSeverity)
    {
        var beforeByKey = before
            .Where(item => !string.IsNullOrWhiteSpace(keySelector(item)))
            .ToDictionary(keySelector, StringComparer.OrdinalIgnoreCase);
        var afterByKey = after
            .Where(item => !string.IsNullOrWhiteSpace(keySelector(item)))
            .ToDictionary(keySelector, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, beforeItem) in beforeByKey.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!afterByKey.TryGetValue(key, out var afterItem))
            {
                changes.Add(new RtpxProtocolDiffChange(
                    category,
                    key,
                    "Removed",
                    removedSeverity,
                    $"{category} '{key}' was removed.",
                    describe(beforeItem),
                    null));
                continue;
            }

            var beforeDescription = describe(beforeItem);
            var afterDescription = describe(afterItem);
            if (!string.Equals(beforeDescription, afterDescription, StringComparison.Ordinal))
            {
                changes.Add(new RtpxProtocolDiffChange(
                    category,
                    key,
                    "Changed",
                    changedSeverity,
                    $"{category} '{key}' changed.",
                    beforeDescription,
                    afterDescription));
            }
        }

        foreach (var (key, afterItem) in afterByKey.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (beforeByKey.ContainsKey(key))
            {
                continue;
            }

            changes.Add(new RtpxProtocolDiffChange(
                category,
                key,
                "Added",
                changedSeverity,
                $"{category} '{key}' was added.",
                null,
                describe(afterItem)));
        }
    }

    private static string ProtocolPackageSummary(RadiotherapyProtocolPackage package)
    {
        return $"{package.Name} {package.Version} | {package.DiseaseSite} | {package.Intent} | structures={package.Structures.Count} | prescriptions={package.Prescriptions.Count} | constraints={package.Constraints.Count}";
    }

    private static RtpxAcceptanceReport DeserializeRtpxAcceptanceReport(string json)
    {
        return JsonSerializer.Deserialize<RtpxAcceptanceReport>(json, CiServerJson.Options)
            ?? throw new InvalidOperationException("Stored RT-PX acceptance report could not be deserialized.");
    }

    private static string? ResolveDraftInstitutionProfileJson(
        RtpxWordDraftPublishServerRequest request,
        RadiotherapyProtocolPackage package)
    {
        if (!string.IsNullOrWhiteSpace(request.InstitutionProfilePath)
            || !string.IsNullOrWhiteSpace(GetJson(request.InstitutionProfile, request.InstitutionProfileJson)))
        {
            return request.InstitutionProfileJson;
        }

        var mappings = package.Structures.Select(structure => new RtpxStructureMapping(
            structure.Name,
            structure.Name,
            structure.Aliases,
            "Draft one-to-one mapping generated from Word authoring."));
        var profile = new RtpxInstitutionProfile(
            "BeamKit Draft Review",
            mappings,
            requireExplicitStructureMappings: false,
            owner: package.Owner ?? "BeamKit Draft Review",
            tags: new[] { "draft", "word-authoring" });
        return RtpxInstitutionProfileStore.ToJson(profile);
    }

    private static RtpxPackageSource ResolveRtpxPackageSource(RtpxAcceptanceServerRequest request, string outputDirectory)
    {
        var hasPath = !string.IsNullOrWhiteSpace(request.PackagePath);
        var hasBase64 = !string.IsNullOrWhiteSpace(request.PackageBase64);
        if (new[] { hasPath, hasBase64 }.Count(value => value) != 1)
        {
            throw new ArgumentException("RT-PX acceptance requires exactly one of 'packagePath' or 'packageBase64'.", nameof(request));
        }

        if (hasPath)
        {
            var fullPath = ResolveServerLocalFilePath(request.PackagePath!);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"RT-PX package '{fullPath}' was not found.", fullPath);
            }

            return new RtpxPackageSource(fullPath, HashFile(fullPath));
        }

        var packageBytes = Convert.FromBase64String(request.PackageBase64!.Trim());
        var packagePath = Path.Combine(outputDirectory, "incoming.rtpx.zip");
        if (File.Exists(packagePath) && !request.Overwrite)
        {
            throw new IOException($"RT-PX package upload target '{packagePath}' already exists. Use overwrite to replace it.");
        }

        File.WriteAllBytes(packagePath, packageBytes);
        return new RtpxPackageSource(packagePath, HashBytes(packageBytes));
    }

    private static RtpxInstitutionProfileSource ResolveRtpxInstitutionProfile(RtpxAcceptanceServerRequest request)
    {
        var profileJson = GetJson(request.InstitutionProfile, request.InstitutionProfileJson);
        var hasPath = !string.IsNullOrWhiteSpace(request.InstitutionProfilePath);
        var hasJson = !string.IsNullOrWhiteSpace(profileJson);
        if (new[] { hasPath, hasJson }.Count(value => value) != 1)
        {
            throw new ArgumentException("RT-PX acceptance requires exactly one of 'institutionProfilePath', 'institutionProfile', or 'institutionProfileJson'.", nameof(request));
        }

        if (hasPath)
        {
            var fullPath = ResolveServerLocalFilePath(request.InstitutionProfilePath!);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"RT-PX institution profile '{fullPath}' was not found.", fullPath);
            }

            profileJson = File.ReadAllText(fullPath);
        }

        var profile = RtpxInstitutionProfileStore.FromJson(profileJson ?? throw new ArgumentException("Institution profile JSON is required.", nameof(request)));
        return new RtpxInstitutionProfileSource(profile, HashText(RtpxInstitutionProfileStore.ToJson(profile)));
    }

    private static RtpxEsapiSnapshotSource? ResolveOptionalRtpxEsapiSnapshot(RtpxAcceptanceServerRequest request)
    {
        var snapshotJson = GetJson(request.EsapiSnapshot, request.EsapiSnapshotJson);
        var hasPath = !string.IsNullOrWhiteSpace(request.EsapiSnapshotPath);
        var hasJson = !string.IsNullOrWhiteSpace(snapshotJson);
        var sourceCount = new[] { hasPath, hasJson }.Count(value => value);
        if (sourceCount == 0)
        {
            return null;
        }

        if (sourceCount != 1)
        {
            throw new ArgumentException("Use only one of 'esapiSnapshotPath', 'esapiSnapshot', or 'esapiSnapshotJson'.", nameof(request));
        }

        string? fullPath = null;
        if (hasPath)
        {
            fullPath = ResolveServerLocalFilePath(request.EsapiSnapshotPath!);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"ESAPI snapshot '{fullPath}' was not found.", fullPath);
            }

            snapshotJson = File.ReadAllText(fullPath);
        }

        var json = snapshotJson ?? throw new ArgumentException("ESAPI snapshot JSON is required.", nameof(request));
        return new RtpxEsapiSnapshotSource(EsapiPlanSnapshotJson.FromJson(json), fullPath, HashText(json));
    }

    private static RtpxWordDocxSource ResolveRtpxWordDocxSource(RtpxWordAuthoringServerRequest request, string outputDirectory)
    {
        return ResolveRtpxWordDocxSource(request.DocxPath, request.DocxBase64, request.FileName, outputDirectory);
    }

    private static RtpxWordDocxSource ResolveRtpxWordDocxSource(RtpxWordDraftPublishServerRequest request, string outputDirectory)
    {
        return ResolveRtpxWordDocxSource(request.DocxPath, request.DocxBase64, request.FileName, outputDirectory);
    }

    private static RtpxWordDocxSource ResolveRtpxWordDocxSource(
        string? docxPath,
        string? docxBase64,
        string? fileName,
        string outputDirectory)
    {
        var hasPath = !string.IsNullOrWhiteSpace(docxPath);
        var hasBase64 = !string.IsNullOrWhiteSpace(docxBase64);
        if (new[] { hasPath, hasBase64 }.Count(value => value) != 1)
        {
            throw new ArgumentException("RT-PX Word extraction requires exactly one of 'docxPath' or 'docxBase64'.");
        }

        if (hasPath)
        {
            var fullPath = ResolveServerLocalFilePath(docxPath!);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"RT-PX Word document '{fullPath}' was not found.", fullPath);
            }

            return new RtpxWordDocxSource(fullPath, Path.GetFileName(fullPath), HashFile(fullPath));
        }

        var docxBytes = Convert.FromBase64String(docxBase64!.Trim());
        var safeFileName = SafeDocxFileName(fileName);
        var uploadPath = Path.Combine(outputDirectory, safeFileName);
        File.WriteAllBytes(uploadPath, docxBytes);
        return new RtpxWordDocxSource(uploadPath, safeFileName, HashBytes(docxBytes));
    }

    private ValidationEvidencePackage CreateRtpxAcceptanceSafetyEvidence(
        string acceptanceId,
        RtpxAcceptanceReport report,
        RtpxInstitutionProfile profile,
        string outputDirectory,
        CiServerManagedRulePackVersionSummary version,
        RulePackTestReport? testReport,
        bool esapiSnapshotWasSupplied)
    {
        var generatedAtUtc = timeProvider.GetUtcNow();
        var regressionStatus = testReport is null
            ? ValidationEvidenceStatus.NotRun
            : testReport.Passed ? ValidationEvidenceStatus.Pass : ValidationEvidenceStatus.Fail;
        var clinicalStatus = report.LocalPackage.Status == ProtocolPackageStatus.Approved
            ? ValidationEvidenceStatus.Pass
            : ValidationEvidenceStatus.NotRun;
        var evidenceItems = new List<ValidationEvidenceItem>
        {
            new(
                "EV-RTPX-ACCEPTANCE",
                "RT-PX local acceptance review",
                ValidationEvidenceKind.ClinicalReview,
                clinicalStatus,
                report.AcceptedAtUtc,
                Path.Combine(outputDirectory, "acceptance-report.md"),
                reviewedBy: profile.ReviewedBy ?? profile.AcceptedBy,
                summary: clinicalStatus == ValidationEvidenceStatus.Pass
                    ? "Institution profile supplied reviewer, approver, and effective-date metadata."
                    : "Institution profile is accepted for review artifacts but lacks complete local approval metadata.",
                linkedControlIds: new[] { "CTRL-RTPX-CLINICAL-REVIEW" }),
            new(
                "EV-RTPX-REGRESSION",
                "Generated rule-pack regression tests",
                ValidationEvidenceKind.RegressionTest,
                regressionStatus,
                generatedAtUtc,
                "BeamKit.CiServer managed rule-pack regression tests",
                summary: testReport is null
                    ? "Regression tests were not requested during RT-PX package import."
                    : $"{testReport.PassedCount}/{testReport.Results.Count} regression case(s) passed.",
                linkedControlIds: new[] { "CTRL-RTPX-REGRESSION" })
        };

        if (report.EsapiEvidence is not null)
        {
            var esapiStatus = report.EsapiEvidence.SnapshotValidation.ErrorCount == 0
                && report.Issues.All(issue => !issue.Code.StartsWith("rtpx.acceptance.esapi.", StringComparison.OrdinalIgnoreCase)
                    || issue.Severity != RtpxAcceptanceIssueSeverity.Error)
                    ? ValidationEvidenceStatus.Pass
                    : ValidationEvidenceStatus.Fail;
            evidenceItems.Add(new ValidationEvidenceItem(
                "EV-RTPX-ESAPI",
                "Optional ESAPI plan snapshot acceptance evidence",
                ValidationEvidenceKind.IntegrationTest,
                esapiStatus,
                generatedAtUtc,
                report.EsapiEvidence.SnapshotPath,
                summary: $"{report.EsapiEvidence.StructureChecks.Count} structure check(s), {report.EsapiEvidence.PrescriptionChecks.Count} prescription check(s).",
                linkedControlIds: new[] { "CTRL-RTPX-ESAPI" }));
        }

        var controls = new List<SafetyControl>
        {
            new(
                "CTRL-RTPX-PACKAGE-ACCEPTED",
                "RT-PX package accepted",
                "The package accepted without blocking structure, validation, or optional ESAPI evidence errors.",
                SafetyControlType.Verification,
                isSatisfied: report.IsAccepted,
                evidenceIds: new[] { "EV-RTPX-ACCEPTANCE" }),
            new(
                "CTRL-RTPX-REGRESSION",
                "Generated rule pack regression tests pass",
                "The generated rule pack was tested against BeamKit synthetic regression cases before promotion.",
                SafetyControlType.Verification,
                isSatisfied: regressionStatus == ValidationEvidenceStatus.Pass,
                evidenceIds: new[] { "EV-RTPX-REGRESSION" }),
            new(
                "CTRL-RTPX-CLINICAL-REVIEW",
                "Local clinical acceptance metadata is complete",
                "The institution profile includes reviewer, approver, and effective-date metadata for local use.",
                SafetyControlType.Process,
                isSatisfied: clinicalStatus == ValidationEvidenceStatus.Pass,
                evidenceIds: new[] { "EV-RTPX-ACCEPTANCE" })
        };
        if (esapiSnapshotWasSupplied)
        {
            controls.Add(new SafetyControl(
                "CTRL-RTPX-ESAPI",
                "Optional ESAPI snapshot evidence evaluated",
                "The acceptance workflow compared the package against an ESAPI-exported plan snapshot.",
                SafetyControlType.Verification,
                isRequired: false,
                isSatisfied: report.EsapiEvidence is not null,
                evidenceIds: report.EsapiEvidence is null ? Array.Empty<string>() : new[] { "EV-RTPX-ESAPI" }));
        }

        return new ValidationEvidencePackage(
            $"evidence-{acceptanceId}",
            "RulePack",
            version.RulePackId,
            version.VersionId,
            version.Fingerprint,
            generatedAtUtc,
            ClinicalUseClassification.ClinicalDecisionSupport,
            evidenceItems,
            new SafetyControlChecklist("RT-PX acceptance promotion controls", "1", controls),
            owner: report.LocalPackage.Owner ?? profile.Owner ?? report.Institution,
            reviewer: profile.ReviewedBy ?? profile.AcceptedBy,
            summary: $"Safety evidence generated from RT-PX acceptance record {acceptanceId}.");
    }

    private static string ResolveRtpxWordAuthoringOutputDirectory(string? outputDirectory, string authoringId)
    {
        var value = CiServerText.Optional(outputDirectory)
            ?? Path.Combine("artifacts", "beamkit-ci-server", "rtpx-word", authoringId);
        return Path.GetFullPath(value);
    }

    private static string ResolveRtpxAcceptanceOutputDirectory(string? outputDirectory, string acceptanceId)
    {
        var value = CiServerText.Optional(outputDirectory)
            ?? Path.Combine("artifacts", "beamkit-ci-server", "rtpx-acceptance", acceptanceId);
        return Path.GetFullPath(value);
    }

    private static string ResolveRtpxAuthoringLibraryPath(string? configuredPath, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var fullConfiguredPath = ResolveServerLocalFilePath(configuredPath);
            if (!File.Exists(fullConfiguredPath))
            {
                throw new FileNotFoundException($"RT-PX authoring library '{fullConfiguredPath}' was not found.", fullConfiguredPath);
            }

            return fullConfiguredPath;
        }

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "authoring", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "authoring", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "BeamKit.CiServer", "authoring", fileName)
        };

        foreach (var candidate in candidates.Select(Path.GetFullPath))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"Default RT-PX authoring library '{fileName}' was not found.");
    }

    private static string SafeDocxFileName(string? fileName)
    {
        var raw = Path.GetFileName(CiServerText.Optional(fileName) ?? "protocol.docx");
        var stem = Path.GetFileNameWithoutExtension(raw);
        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "protocol";
        }

        return $"{Slug(stem)}.docx";
    }

    private static string Slug(string value)
    {
        var characters = value.Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();
        var slug = string.Join('-', new string(characters).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(slug) ? "protocol" : slug;
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return HashStream(stream);
    }

    private static string HashText(string text)
    {
        return HashBytes(Encoding.UTF8.GetBytes(text));
    }

    private static string HashBytes(byte[] bytes)
    {
        return "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static string HashStream(Stream stream)
    {
        return "sha256:" + Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private CiServerManagedRulePackVersion FindRequiredManagedRulePackVersion(string rulePackId, string versionId)
    {
        return store.FindRulePackVersion(rulePackId, versionId)
            ?? throw new InvalidOperationException($"Rule pack version '{rulePackId}/{versionId}' was not found.");
    }

    private static CiServerRulePackSummary CreateManagedRulePackSummary(CiServerManagedRulePackVersionSummary version)
    {
        return new CiServerRulePackSummary(
            version.RulePackId,
            "Managed",
            $"ci-store:{version.VersionId}",
            version.Name,
            version.Version,
            version.Owner,
            version.Description,
            version.DiseaseSite,
            version.Tags,
            version.Fingerprint,
            isLoadable: true,
            version.IsValid,
            version.ValidationErrorCount,
            version.ValidationWarningCount);
    }

    private static CiServerRulePackDetail CreateManagedRulePackDetail(CiServerManagedRulePackVersion version)
    {
        return new CiServerRulePackDetail(CreateManagedRulePackSummary(version.ToSummary()), version.ValidationReport);
    }

    private static string NormalizeManagedRulePackId(string? value)
    {
        var rulePackId = CiServerText.Required(value, nameof(value));
        if (CiServerRulePackRegistry.IsBuiltInRulePackId(rulePackId))
        {
            throw new ArgumentException($"Rule-pack id '{rulePackId}' is reserved.", nameof(value));
        }

        if (rulePackId.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_' and not '.'))
        {
            throw new ArgumentException("Rule-pack id may only contain letters, numbers, dashes, underscores, and periods.", nameof(value));
        }

        return rulePackId;
    }

    private static RulePackImportSource LoadRulePackImportSource(RulePackImportServerRequest request)
    {
        var manifestJson = GetJson(request.Manifest, request.ManifestJson);
        var bundleJson = GetJson(request.Bundle, request.BundleJson);
        var sourceCount = new[]
        {
            !string.IsNullOrWhiteSpace(request.ManifestPath),
            !string.IsNullOrWhiteSpace(manifestJson),
            !string.IsNullOrWhiteSpace(request.BundlePath),
            !string.IsNullOrWhiteSpace(bundleJson)
        }.Count(value => value);
        if (sourceCount != 1)
        {
            throw new ArgumentException("Rule-pack import requires exactly one of 'manifestPath', 'manifest'/'manifestJson', 'bundlePath', or 'bundle'/'bundleJson'.", nameof(request));
        }

        if (!string.IsNullOrWhiteSpace(request.BundlePath))
        {
            var fullPath = Path.GetFullPath(request.BundlePath);
            var bundle = VerifyBundleSource(RulePackBundleStore.FromFile(fullPath));
            return new RulePackImportSource(
                bundle.ManifestJson,
                Directory.GetCurrentDirectory(),
                "BundleFile",
                fullPath,
                bundle);
        }

        if (!string.IsNullOrWhiteSpace(bundleJson))
        {
            var bundle = VerifyBundleSource(RulePackBundleStore.FromJson(bundleJson));
            return new RulePackImportSource(
                bundle.ManifestJson,
                Directory.GetCurrentDirectory(),
                "InlineBundleJson",
                CiServerText.Optional(request.Source) ?? "inline-bundle-json",
                bundle);
        }

        if (!string.IsNullOrWhiteSpace(request.ManifestPath))
        {
            var fullPath = Path.GetFullPath(request.ManifestPath);
            return new RulePackImportSource(
                File.ReadAllText(fullPath),
                Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory(),
                "File",
                fullPath,
                null);
        }

        return new RulePackImportSource(
            manifestJson ?? throw new ArgumentException("Manifest JSON is required.", nameof(request)),
            Path.GetFullPath(CiServerText.Optional(request.BaseDirectory) ?? Directory.GetCurrentDirectory()),
            "InlineJson",
            CiServerText.Optional(request.Source) ?? "inline-json",
            null);
    }

    private static RulePackBundle VerifyBundleSource(RulePackBundle bundle)
    {
        var report = new RulePackBundleVerifier().Verify(bundle);
        if (!report.IsValid)
        {
            var details = string.Join("; ", report.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new InvalidOperationException($"Rule-pack bundle failed verification: {details}");
        }

        return bundle;
    }

    private string CreateRulePackVersionId(string fingerprint)
    {
        var fingerprintSuffix = fingerprint.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
            ? fingerprint["sha256:".Length..Math.Min(fingerprint.Length, "sha256:".Length + 12)]
            : fingerprint[..Math.Min(fingerprint.Length, 12)];
        return $"rpv-{timeProvider.GetUtcNow():yyyyMMddHHmmss}-{fingerprintSuffix}-{Guid.NewGuid():N}"[..59];
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

    private static IReadOnlyList<RulePackTestCase> LoadRulePackTestCases(string? syntheticCaseId, bool useCompleteSyntheticReadiness = false)
    {
        if (!string.IsNullOrWhiteSpace(syntheticCaseId))
        {
            return new[] { CreateRulePackTestCase(SyntheticClinicalCaseLibrary.Find(syntheticCaseId), useCompleteSyntheticReadiness) };
        }

        return new[]
        {
            CreateRulePackTestCase(SyntheticClinicalCaseLibrary.Find("head-neck-pass"), useCompleteSyntheticReadiness),
            CreateRulePackTestCase(SyntheticClinicalCaseLibrary.Find("head-neck-cord-fail"), useCompleteSyntheticReadiness),
            CreateRulePackTestCase(SyntheticClinicalCaseLibrary.Find("head-neck-missing-structure"), useCompleteSyntheticReadiness)
        };
    }

    private static RulePackTestCase CreateRulePackTestCase(SyntheticClinicalCase clinicalCase, bool useCompleteSyntheticReadiness)
    {
        return new RulePackTestCase(
            clinicalCase.Id,
            clinicalCase.Description,
            clinicalCase.Plan,
            clinicalCase.ExpectedToPass ? BeamKitCheckStatus.Pass : BeamKitCheckStatus.Fail,
            ExpectedFindingIdsForCase(clinicalCase.Id),
            useCompleteSyntheticReadiness ? CreateCompleteSyntheticReadinessInput(clinicalCase.Plan) : null);
    }

    private static PlanReadinessInput CreateCompleteSyntheticReadinessInput(Plan plan)
    {
        return new PlanReadinessInput(plan)
        {
            CtImported = true,
            OptimizationFinished = true,
            PhysicsQaComplete = true,
            PhysicianApprovalComplete = true,
            TreatmentReady = true
        };
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

    private sealed record RulePackImportSource(string ManifestJson, string BaseDirectory, string SourceKind, string Source, RulePackBundle? Bundle);

    private sealed record RtpxWordDocxSource(string Path, string FileName, string Fingerprint);

    private sealed record RtpxPackageSource(string Path, string Fingerprint);

    private sealed record RtpxInstitutionProfileSource(RtpxInstitutionProfile Profile, string Fingerprint);

    private sealed record RtpxEsapiSnapshotSource(EsapiPlanSnapshot Snapshot, string? Path, string Fingerprint);

    private sealed record ProtocolComplianceBinding(
        CiServerRtpxAcceptanceRecord Acceptance,
        CiServerManagedRulePackVersion Version,
        RadiotherapyProtocolPackage Protocol);

    private sealed record AssignmentRequestContext(PlannerAssignmentRequest Request, AssignmentIntelligenceSummary? Intelligence);

    private AssignmentRequestContext CreateAssignmentRequestContext(AssignmentServerRequest request, DateOnly assignmentDate, bool includeTeamRoles)
    {
        var dueDate = ParseDueDate(request.DueDate) ?? assignmentDate.AddDays(3);
        var requiredRoles = ResolveAssignmentRoles(request.RequiredRoles, includeTeamRoles);
        var planners = ApplyLiveWorkload(LoadPlannerProfiles(request, assignmentDate, dueDate), request.UseLiveWorkload);
        var plan = TryLoadAssignmentPlan(request);
        var intelligenceReport = plan is null
            ? null
            : new CasePlanIntelligenceService().Analyze(new CasePlanIntelligenceRequest(
                plan,
                dueDate,
                assignmentDate,
                request.Priority));
        var diseaseSite = CiServerText.Optional(request.DiseaseSite) ?? intelligenceReport?.DiseaseSite ?? plan?.DiseaseSite ?? "Head and Neck";
        var requiredSkills = request.RequiredSkills is { Count: > 0 }
            ? request.RequiredSkills
            : InferRequiredAssignmentSkills(plan, intelligenceReport, diseaseSite);
        var complexityScore = request.ComplexityScore ?? MapAssignmentComplexityScore(intelligenceReport?.ComplexityScore);
        var caseId = CiServerText.Optional(request.CaseId)
            ?? CiServerText.Optional(request.SyntheticCaseId)
            ?? plan?.Id
            ?? "server-assignment";
        var summary = intelligenceReport is null
            ? null
            : CreateAssignmentIntelligenceSummary(intelligenceReport, complexityScore, requiredSkills);

        var assignmentRequest = new PlannerAssignmentRequest(
            caseId,
            diseaseSite,
            dueDate,
            planners,
            requiredSkills,
            complexityScore,
            request.Priority ?? 3,
            request.Physician,
            assignmentDate,
            requiredRoles[0],
            requiredRoles);

        return new AssignmentRequestContext(assignmentRequest, summary);
    }

    private IReadOnlyList<PlannerProfile> ApplyLiveWorkload(IReadOnlyList<PlannerProfile> planners, bool useLiveWorkload)
    {
        if (!useLiveWorkload)
        {
            return planners;
        }

        var activeWorkItems = store.ListWorkItems(new CaseWorkItemQuery { ActiveOnly = true, Limit = 500 });
        if (activeWorkItems.Count == 0)
        {
            return planners;
        }

        var assignedCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var workItem in activeWorkItems)
        {
            AddAssignedWorkload(assignedCounts, workItem.AssignedDosimetristId);
            AddAssignedWorkload(assignedCounts, workItem.AssignedPhysicistId);
        }

        return planners
            .Select(planner => assignedCounts.TryGetValue(planner.Id, out var activeCount)
                ? planner with { ActiveCaseCount = planner.ActiveCaseCount + activeCount }
                : planner)
            .ToArray();
    }

    private static void AddAssignedWorkload(IDictionary<string, int> assignedCounts, string? staffId)
    {
        if (string.IsNullOrWhiteSpace(staffId))
        {
            return;
        }

        assignedCounts[staffId] = assignedCounts.TryGetValue(staffId, out var current) ? current + 1 : 1;
    }

    private static AssignmentServerRequest CreateWorkItemAssignmentRequest(CaseWorkItem workItem, AssignmentServerRequest request)
    {
        var requestHasPlanSource = HasAssignmentPlanSource(request);
        return new AssignmentServerRequest
        {
            CaseId = CiServerText.Optional(request.CaseId) ?? workItem.CaseId,
            SyntheticCaseId = requestHasPlanSource
                ? CiServerText.Optional(request.SyntheticCaseId)
                : CiServerText.Optional(request.SyntheticCaseId) ?? workItem.SyntheticCaseId,
            DiseaseSite = CiServerText.Optional(request.DiseaseSite) ?? workItem.DiseaseSite,
            RequiredSkills = request.RequiredSkills is { Count: > 0 }
                ? request.RequiredSkills
                : workItem.Intelligence?.SuggestedSkills,
            RequiredRoles = request.RequiredRoles,
            DueDate = CiServerText.Optional(request.DueDate) ?? FormatDueDate(workItem.DueDate),
            ComplexityScore = request.ComplexityScore ?? workItem.Intelligence?.AppliedAssignmentComplexityScore,
            Priority = request.Priority ?? workItem.Priority,
            Physician = CiServerText.Optional(request.Physician) ?? workItem.Physician,
            Plan = request.Plan,
            PlanJson = request.PlanJson,
            EsapiSnapshot = request.EsapiSnapshot,
            EsapiSnapshotJson = request.EsapiSnapshotJson,
            Roster = request.Roster,
            RosterJson = request.RosterJson,
            RosterPath = request.RosterPath,
            UseLiveWorkload = request.UseLiveWorkload
        };
    }

    private static bool HasAssignmentPlanSource(AssignmentServerRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.PlanJson)
            || !string.IsNullOrWhiteSpace(request.EsapiSnapshotJson)
            || HasJsonValue(request.Plan)
            || HasJsonValue(request.EsapiSnapshot);
    }

    private static bool HasJsonValue(System.Text.Json.JsonElement? element)
    {
        return element.HasValue && element.Value.ValueKind is not System.Text.Json.JsonValueKind.Null and not System.Text.Json.JsonValueKind.Undefined;
    }

    private static string? FormatDueDate(DateOnly? dueDate)
    {
        return dueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private CaseWorkItemAssignmentEvent CreateWorkItemHistoryEvent(
        CaseWorkItemStatus status,
        CiServerAuditContext? auditContext,
        string action,
        string? dosimetristId = null,
        string? dosimetristName = null,
        string? physicistId = null,
        string? physicistName = null,
        string? note = null)
    {
        var context = auditContext ?? CiServerAuditContext.Service;
        return new CaseWorkItemAssignmentEvent
        {
            Id = CreateWorkItemEventId(),
            OccurredAtUtc = timeProvider.GetUtcNow(),
            Actor = context.Actor,
            Action = action,
            Status = status,
            DosimetristId = CiServerText.Optional(dosimetristId),
            DosimetristName = CiServerText.Optional(dosimetristName),
            PhysicistId = CiServerText.Optional(physicistId),
            PhysicistName = CiServerText.Optional(physicistName),
            Note = CiServerText.Optional(note)
        };
    }

    private static IReadOnlyList<CaseWorkItemAssignmentEvent> AppendHistory(
        CaseWorkItem workItem,
        CaseWorkItemAssignmentEvent assignmentEvent)
    {
        return workItem.AssignmentHistory.Concat(new[] { assignmentEvent }).ToArray();
    }

    private static Plan? TryLoadAssignmentPlan(AssignmentServerRequest request)
    {
        var planJson = GetJson(request.Plan, request.PlanJson);
        var esapiSnapshotJson = GetJson(request.EsapiSnapshot, request.EsapiSnapshotJson);
        var sources = new[]
        {
            !string.IsNullOrWhiteSpace(request.SyntheticCaseId),
            !string.IsNullOrWhiteSpace(planJson),
            !string.IsNullOrWhiteSpace(esapiSnapshotJson)
        }.Count(value => value);
        if (sources > 1)
        {
            throw new ArgumentException("Use only one of 'syntheticCaseId', 'plan'/'planJson', or 'esapiSnapshot'/'esapiSnapshotJson' for assignment inference.", nameof(request));
        }

        if (!string.IsNullOrWhiteSpace(request.SyntheticCaseId))
        {
            return SyntheticClinicalCaseLibrary.Find(request.SyntheticCaseId).Plan;
        }

        if (!string.IsNullOrWhiteSpace(planJson))
        {
            return LoadBeamKitPlanJson(planJson).Plan;
        }

        if (!string.IsNullOrWhiteSpace(esapiSnapshotJson))
        {
            return LoadEsapiSnapshotJson(esapiSnapshotJson).Plan;
        }

        return null;
    }

    private static IReadOnlyList<string> InferRequiredAssignmentSkills(
        Plan? plan,
        CasePlanIntelligenceReport? intelligenceReport,
        string diseaseSite)
    {
        var skills = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        if (plan is not null)
        {
            AddTechniqueSkill(skills, plan.Prescription.RequestedTechniqueId);
            foreach (var beam in plan.Beams.Where(beam => !beam.IsSetupField))
            {
                AddTechniqueSkill(skills, beam.TechniqueId);
                AddTechniqueSkill(skills, beam.Modality);
            }

            if (IsSbrtLike(plan, diseaseSite))
            {
                skills.Add("SBRT");
            }

            if (IsSrsLike(plan, diseaseSite))
            {
                skills.Add("SRS");
            }
        }

        if (skills.Count == 0)
        {
            skills.Add("VMAT");
        }

        if (intelligenceReport?.ComplexityLevel is CaseComplexityLevel.High or CaseComplexityLevel.VeryHigh)
        {
            skills.Add(diseaseSite);
        }

        return skills.ToArray();
    }

    private static void AddTechniqueSkill(ISet<string> skills, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (ContainsAny(value, "VMAT", "RapidArc"))
        {
            skills.Add("VMAT");
        }
        else if (ContainsAny(value, "IMRT"))
        {
            skills.Add("IMRT");
        }
        else if (ContainsAny(value, "SRS"))
        {
            skills.Add("SRS");
        }
        else if (ContainsAny(value, "SBRT", "SABR"))
        {
            skills.Add("SBRT");
        }
        else if (ContainsAny(value, "3D"))
        {
            skills.Add("3D");
        }
    }

    private static bool IsSbrtLike(Plan plan, string diseaseSite)
    {
        return ContainsAny(diseaseSite, "lung", "sbrt", "sabr")
            && (plan.Prescription.FractionCount <= 5 || plan.Prescription.DosePerFractionGy >= 5m);
    }

    private static bool IsSrsLike(Plan plan, string diseaseSite)
    {
        return ContainsAny(diseaseSite, "brain", "srs")
            && (plan.Prescription.FractionCount <= 5 || ContainsAny(plan.Id, "srs"));
    }

    private static int MapAssignmentComplexityScore(decimal? predictiveComplexityScore)
    {
        if (!predictiveComplexityScore.HasValue)
        {
            return 3;
        }

        return predictiveComplexityScore.Value switch
        {
            >= 80m => 5,
            >= 60m => 4,
            >= 40m => 3,
            >= 20m => 2,
            _ => 1
        };
    }

    private static AssignmentIntelligenceSummary CreateAssignmentIntelligenceSummary(
        CasePlanIntelligenceReport report,
        int appliedAssignmentComplexityScore,
        IReadOnlyList<string> suggestedSkills)
    {
        return new AssignmentIntelligenceSummary(
            report.PlanId,
            report.DiseaseSite,
            report.ComplexityScore,
            report.ComplexityLevel.ToString(),
            report.QaRiskScore,
            report.QaRiskLevel.ToString(),
            report.EstimatedPlanningHours,
            report.EstimatedPhysicsReviewMinutes,
            appliedAssignmentComplexityScore,
            suggestedSkills,
            report.Signals.Take(5).Select(signal => $"{signal.Severity}: {signal.Category} - {signal.Name}").ToArray(),
            report.Recommendations.Take(5).ToArray());
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<PlannerProfile> LoadPlannerProfiles(AssignmentServerRequest request, DateOnly assignmentDate, DateOnly dueDate)
    {
        if (request.Roster is not null)
        {
            return request.Roster.ToPlannerProfiles(assignmentDate, dueDate);
        }

        if (!string.IsNullOrWhiteSpace(request.RosterJson))
        {
            return StaffRosterLoader.FromJson(request.RosterJson).ToPlannerProfiles(assignmentDate, dueDate);
        }

        if (!string.IsNullOrWhiteSpace(request.RosterPath))
        {
            return StaffRosterLoader.FromFile(ResolveServerLocalFilePath(request.RosterPath)).ToPlannerProfiles(assignmentDate, dueDate);
        }

        return CreateSyntheticPlannerProfiles(assignmentDate);
    }

    private static string ResolveServerLocalFilePath(string path)
    {
        var trimmed = CiServerText.Required(path, nameof(path));
        if (Path.IsPathRooted(trimmed))
        {
            return Path.GetFullPath(trimmed);
        }

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.GetFullPath(Path.Combine(directory.FullName, trimmed));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.GetFullPath(trimmed);
    }

    private static IReadOnlyList<PlannerProfile> CreateSyntheticPlannerProfiles(DateOnly assignmentDate)
    {
        return new[]
        {
            new PlannerProfile(
                "planner-jane",
                "Jane Doe",
                new[] { "VMAT", "SBRT", "Head and Neck", "Lung" },
                new[] { "Head and Neck", "Lung" },
                activeCaseCount: 2,
                maxActiveCaseCount: 8,
                role: PlanningStaffRole.Dosimetrist,
                preferredPhysicians: new[] { "Dr Smith" },
                blockedPhysicians: new[] { "Dr Gray" },
                schedule: CreateSyntheticSchedule(assignmentDate, 0, 1, 1, 2)),
            new PlannerProfile(
                "planner-alex",
                "Alex Kim",
                new[] { "VMAT", "Prostate" },
                new[] { "Prostate" },
                activeCaseCount: 6,
                maxActiveCaseCount: 8,
                role: PlanningStaffRole.Dosimetrist,
                maxComplexityScore: 4,
                schedule: CreateSyntheticSchedule(assignmentDate, 1, 1, 1, 1)),
            new PlannerProfile(
                "planner-priya",
                "Priya Shah",
                new[] { "VMAT", "SRS", "Head and Neck" },
                new[] { "Head and Neck", "Brain" },
                activeCaseCount: 4,
                maxActiveCaseCount: 8,
                ptoUntil: assignmentDate.AddDays(1),
                role: PlanningStaffRole.Dosimetrist,
                maxComplexityScore: 5,
                preferredPhysicians: new[] { "Dr Gray" },
                schedule: CreateSyntheticSchedule(assignmentDate, 0, 0, 1, 1)),
            new PlannerProfile(
                "planner-sam",
                "Sam Rivera",
                new[] { "3D", "Breast" },
                new[] { "Breast" },
                activeCaseCount: 1,
                maxActiveCaseCount: 8,
                role: PlanningStaffRole.Dosimetrist,
                maxComplexityScore: 3,
                schedule: CreateSyntheticSchedule(assignmentDate, 1, 1, 0, 1)),
            new PlannerProfile(
                "physicist-morgan",
                "Morgan Lee",
                new[] { "VMAT", "SBRT", "SRS", "Lung", "Machine QA" },
                new[] { "Head and Neck", "Lung", "Brain" },
                activeCaseCount: 5,
                maxActiveCaseCount: 10,
                role: PlanningStaffRole.Physicist,
                maxComplexityScore: 5,
                preferredPhysicians: new[] { "Dr Smith", "Dr Gray" },
                schedule: CreateSyntheticSchedule(assignmentDate, 1, 1, 2, 1)),
            new PlannerProfile(
                "physicist-taylor",
                "Taylor Chen",
                new[] { "VMAT", "Prostate", "Breast" },
                new[] { "Prostate", "Breast" },
                activeCaseCount: 3,
                maxActiveCaseCount: 10,
                role: PlanningStaffRole.Physicist,
                maxComplexityScore: 4,
                blockedPhysicians: new[] { "Dr Gray" },
                schedule: CreateSyntheticSchedule(assignmentDate, 0, 0, 1, 1))
        };
    }

    private static IReadOnlyList<PlannerScheduleDay> CreateSyntheticSchedule(DateOnly startDate, params int[] assignedCases)
    {
        return assignedCases
            .Select((assigned, index) => new PlannerScheduleDay(startDate.AddDays(index), assigned, capacity: 2))
            .ToArray();
    }

    private static IReadOnlyList<PlanningStaffRole> ResolveAssignmentRoles(IReadOnlyList<string>? values, bool includeTeamRoles)
    {
        if (values is { Count: > 0 })
        {
            return values.Select(ParsePlanningStaffRole).Distinct().ToArray();
        }

        return includeTeamRoles
            ? new[] { PlanningStaffRole.Dosimetrist, PlanningStaffRole.Physicist }
            : new[] { PlanningStaffRole.Dosimetrist };
    }

    private static PlanningStaffRole ParsePlanningStaffRole(string value)
    {
        if (Enum.TryParse<PlanningStaffRole>(value, ignoreCase: true, out var role))
        {
            return role;
        }

        throw new ArgumentException($"Unsupported assignment role '{value}'. Use Dosimetrist or Physicist.");
    }

    private string CreateServerRunId()
    {
        return $"run-{timeProvider.GetUtcNow():yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..37];
    }

    private string CreateWorkItemId()
    {
        return $"work-{timeProvider.GetUtcNow():yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..38];
    }

    private string CreateWorkItemEventId()
    {
        return $"wie-{timeProvider.GetUtcNow():yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..37];
    }

    private string CreateAuditEventId()
    {
        return $"audit-{timeProvider.GetUtcNow():yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..39];
    }

    private string CreateRtpxAcceptanceId()
    {
        return $"rtpx-{timeProvider.GetUtcNow():yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..38];
    }

    private string CreateProtocolComplianceRunId()
    {
        return $"pcr-{timeProvider.GetUtcNow():yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..37];
    }

    private string CreateRtpxWordAuthoringId()
    {
        return $"rtpxw-{timeProvider.GetUtcNow():yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..39];
    }

    private static DateOnly? ParseDueDate(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : DateOnly.Parse(value, CultureInfo.InvariantCulture);
    }
}
