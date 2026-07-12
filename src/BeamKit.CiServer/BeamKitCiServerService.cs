using System.Globalization;
using BeamKit.ChangeDetection;
using BeamKit.Check;
using BeamKit.Core.Domain;
using BeamKit.Core.Serialization;
using BeamKit.Esapi;
using BeamKit.Intelligence;
using BeamKit.RulePacks;
using BeamKit.Safety;
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
        ArgumentNullException.ThrowIfNull(request);

        var rulePackId = NormalizeManagedRulePackId(request.RulePackId);
        var source = LoadRulePackImportSource(request);
        var rulePack = source.Bundle is null
            ? BeamKitRulePackLoader.FromJson(source.ManifestJson, source.BaseDirectory)
            : RulePackBundleLoader.ToRulePack(source.Bundle);
        var validation = client.ValidateRulePack(rulePack);
        var testReport = request.RunRegressionTests
            ? client.TestRulePack(rulePack, LoadRulePackTestCases(request.SyntheticCaseId))
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

    private sealed record RulePackImportSource(string ManifestJson, string BaseDirectory, string SourceKind, string Source, RulePackBundle? Bundle);

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

    private static DateOnly? ParseDueDate(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : DateOnly.Parse(value, CultureInfo.InvariantCulture);
    }
}
