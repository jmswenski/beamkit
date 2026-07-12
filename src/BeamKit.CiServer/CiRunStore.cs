using System.Collections.Concurrent;
using System.Text.Json;

namespace BeamKit.CiServer;

/// <summary>
/// In-memory run store for the first self-hosted BeamKit CI server slice.
/// </summary>
public sealed class CiRunStore : ICiRunStore
{
    private readonly ConcurrentDictionary<string, HostedCiRunRecord> records = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CiRunBaseline> baselines = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CiServerAuditEvent> auditEvents = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CiServerManagedRulePackVersion> rulePackVersions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CiServerRtpxAcceptanceRecord> rtpxAcceptances = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ProtocolComplianceRunRecord> protocolComplianceRuns = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CaseWorkItem> workItems = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds or replaces a run record.
    /// </summary>
    public HostedCiRunRecord Save(HostedCiRunRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        records[record.Id] = record;
        return record;
    }

    /// <summary>
    /// Finds a run by id.
    /// </summary>
    public HostedCiRunSummary? Find(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return records.GetValueOrDefault(id) is { } record ? HostedCiRunSummary.FromRecord(record) : null;
    }

    /// <summary>
    /// Finds the stored full artifact JSON for a run.
    /// </summary>
    public string? FindArtifactJson(string id)
    {
        return string.IsNullOrWhiteSpace(id) || records.GetValueOrDefault(id) is not { } record
            ? null
            : JsonSerializer.Serialize(record.Artifact, CiServerJson.Options);
    }

    /// <summary>
    /// Finds the stored vendor-neutral BeamKit plan snapshot JSON for a run.
    /// </summary>
    public string? FindPlanSnapshotJson(string id)
    {
        return string.IsNullOrWhiteSpace(id) || records.GetValueOrDefault(id) is not { } record
            ? null
            : record.PlanSnapshotJson;
    }

    /// <summary>
    /// Lists runs in reverse chronological order.
    /// </summary>
    public IReadOnlyList<HostedCiRunSummary> List(int limit = 50)
    {
        return List(new CiRunQuery { Limit = limit });
    }

    /// <summary>
    /// Lists runs matching the supplied query in reverse chronological order.
    /// </summary>
    public IReadOnlyList<HostedCiRunSummary> List(CiRunQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return records.Values
            .Where(record => query.Status is null || record.Status == query.Status)
            .Where(record => string.IsNullOrWhiteSpace(query.SyntheticCaseId)
                || string.Equals(record.CaseId, query.SyntheticCaseId, StringComparison.OrdinalIgnoreCase))
            .Where(record => string.IsNullOrWhiteSpace(query.Branch)
                || string.Equals(record.Artifact.Provenance.Branch, query.Branch, StringComparison.OrdinalIgnoreCase))
            .Where(record => query.CreatedFromUtc is null || record.CreatedAtUtc >= query.CreatedFromUtc)
            .Where(record => query.CreatedToUtc is null || record.CreatedAtUtc <= query.CreatedToUtc)
            .OrderByDescending(record => record.CreatedAtUtc)
            .ThenBy(record => record.Id, StringComparer.OrdinalIgnoreCase)
            .Take(query.ClampedLimit)
            .Select(HostedCiRunSummary.FromRecord)
            .ToArray();
    }

    /// <summary>
    /// Adds or replaces a promoted baseline.
    /// </summary>
    public CiRunBaseline SaveBaseline(CiRunBaseline baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);

        baselines[baseline.CaseId] = baseline;
        return baseline;
    }

    /// <summary>
    /// Finds the promoted baseline for a case key.
    /// </summary>
    public CiRunBaseline? FindBaseline(string caseId)
    {
        return string.IsNullOrWhiteSpace(caseId) ? null : baselines.GetValueOrDefault(caseId);
    }

    /// <summary>
    /// Lists promoted baselines.
    /// </summary>
    public IReadOnlyList<CiRunBaseline> ListBaselines()
    {
        return baselines.Values
            .OrderBy(baseline => baseline.CaseId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Adds or replaces a managed rule-pack version.
    /// </summary>
    public CiServerManagedRulePackVersion SaveRulePackVersion(CiServerManagedRulePackVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        rulePackVersions[CreateRulePackVersionKey(version.RulePackId, version.VersionId)] = version;
        return version;
    }

    /// <summary>
    /// Finds a managed rule-pack version.
    /// </summary>
    public CiServerManagedRulePackVersion? FindRulePackVersion(string rulePackId, string versionId)
    {
        return string.IsNullOrWhiteSpace(rulePackId) || string.IsNullOrWhiteSpace(versionId)
            ? null
            : rulePackVersions.GetValueOrDefault(CreateRulePackVersionKey(rulePackId, versionId));
    }

    /// <summary>
    /// Finds the active managed version for a rule-pack id.
    /// </summary>
    public CiServerManagedRulePackVersion? FindActiveRulePackVersion(string rulePackId)
    {
        if (string.IsNullOrWhiteSpace(rulePackId))
        {
            return null;
        }

        return rulePackVersions.Values
            .Where(version => version.IsActive)
            .Where(version => string.Equals(version.RulePackId, rulePackId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(version => version.ActivatedAtUtc ?? version.ImportedAtUtc)
            .ThenBy(version => version.VersionId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    /// <summary>
    /// Lists managed rule-pack versions.
    /// </summary>
    public IReadOnlyList<CiServerManagedRulePackVersionSummary> ListRulePackVersions(string? rulePackId = null)
    {
        return rulePackVersions.Values
            .Where(version => string.IsNullOrWhiteSpace(rulePackId)
                || string.Equals(version.RulePackId, rulePackId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(version => version.RulePackId, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(version => version.ImportedAtUtc)
            .ThenBy(version => version.VersionId, StringComparer.OrdinalIgnoreCase)
            .Select(version => version.ToSummary())
            .ToArray();
    }

    /// <summary>
    /// Promotes one managed rule-pack version as active.
    /// </summary>
    public CiServerManagedRulePackVersion PromoteRulePackVersion(
        string rulePackId,
        string versionId,
        DateTimeOffset activatedAtUtc,
        string? activatedBy = null,
        string? note = null)
    {
        var version = FindRulePackVersion(rulePackId, versionId)
            ?? throw new InvalidOperationException($"Rule pack version '{rulePackId}/{versionId}' was not found.");

        foreach (var existing in rulePackVersions.Values.Where(existing => string.Equals(existing.RulePackId, rulePackId, StringComparison.OrdinalIgnoreCase)))
        {
            rulePackVersions[CreateRulePackVersionKey(existing.RulePackId, existing.VersionId)] = existing with
            {
                IsActive = false,
                ActivatedAtUtc = null,
                ActivatedBy = null,
                ActivationNote = null
            };
        }

        var promoted = version with
        {
            IsActive = true,
            ActivatedAtUtc = activatedAtUtc,
            ActivatedBy = CiServerText.Optional(activatedBy),
            ActivationNote = CiServerText.Optional(note)
        };
        rulePackVersions[CreateRulePackVersionKey(rulePackId, versionId)] = promoted;
        return promoted;
    }

    /// <summary>
    /// Adds or replaces an RT-PX package acceptance record.
    /// </summary>
    public CiServerRtpxAcceptanceRecord SaveRtpxAcceptance(CiServerRtpxAcceptanceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        rtpxAcceptances[record.Id] = record;
        return record;
    }

    /// <summary>
    /// Finds an RT-PX package acceptance record.
    /// </summary>
    public CiServerRtpxAcceptanceRecord? FindRtpxAcceptance(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? null : rtpxAcceptances.GetValueOrDefault(id);
    }

    /// <summary>
    /// Lists recent RT-PX package acceptance records.
    /// </summary>
    public IReadOnlyList<CiServerRtpxAcceptanceSummary> ListRtpxAcceptances(int limit = 50)
    {
        var clampedLimit = Math.Clamp(limit, 1, 500);
        return rtpxAcceptances.Values
            .OrderByDescending(record => record.CreatedAtUtc)
            .ThenBy(record => record.Id, StringComparer.OrdinalIgnoreCase)
            .Take(clampedLimit)
            .Select(record => new CiServerRtpxAcceptanceSummary(record))
            .ToArray();
    }

    /// <summary>
    /// Adds or replaces a protocol compliance run.
    /// </summary>
    public ProtocolComplianceRunRecord SaveProtocolComplianceRun(ProtocolComplianceRunRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        protocolComplianceRuns[record.Id] = record;
        return record;
    }

    /// <summary>
    /// Finds a protocol compliance run by id.
    /// </summary>
    public ProtocolComplianceRunRecord? FindProtocolComplianceRun(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? null : protocolComplianceRuns.GetValueOrDefault(id);
    }

    /// <summary>
    /// Lists recent protocol compliance runs.
    /// </summary>
    public IReadOnlyList<ProtocolComplianceRunSummary> ListProtocolComplianceRuns(int limit = 50)
    {
        var clampedLimit = Math.Clamp(limit, 1, 500);
        return protocolComplianceRuns.Values
            .OrderByDescending(record => record.CreatedAtUtc)
            .ThenBy(record => record.Id, StringComparer.OrdinalIgnoreCase)
            .Take(clampedLimit)
            .Select(record => new ProtocolComplianceRunSummary(record))
            .ToArray();
    }

    /// <summary>
    /// Adds or replaces a case work item.
    /// </summary>
    public CaseWorkItem SaveWorkItem(CaseWorkItem workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        if (string.IsNullOrWhiteSpace(workItem.Id))
        {
            throw new ArgumentException("Work item id is required.", nameof(workItem));
        }

        if (string.IsNullOrWhiteSpace(workItem.CaseId))
        {
            throw new ArgumentException("Work item case id is required.", nameof(workItem));
        }

        workItems[workItem.Id] = workItem;
        return workItem;
    }

    /// <summary>
    /// Finds a case work item by id.
    /// </summary>
    public CaseWorkItem? FindWorkItem(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? null : workItems.GetValueOrDefault(id);
    }

    /// <summary>
    /// Lists case work items matching the supplied query.
    /// </summary>
    public IReadOnlyList<CaseWorkItem> ListWorkItems(CaseWorkItemQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return workItems.Values
            .Where(workItem => query.Status is null || workItem.Status == query.Status)
            .Where(workItem => !query.ActiveOnly || workItem.IsActiveWorkload)
            .Where(workItem => string.IsNullOrWhiteSpace(query.CaseId)
                || string.Equals(workItem.CaseId, query.CaseId, StringComparison.OrdinalIgnoreCase))
            .Where(workItem => string.IsNullOrWhiteSpace(query.DiseaseSite)
                || string.Equals(workItem.DiseaseSite, query.DiseaseSite, StringComparison.OrdinalIgnoreCase))
            .Where(workItem => string.IsNullOrWhiteSpace(query.AssignedStaffId)
                || string.Equals(workItem.AssignedDosimetristId, query.AssignedStaffId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(workItem.AssignedPhysicistId, query.AssignedStaffId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(workItem => workItem.DueDate is null)
            .ThenBy(workItem => workItem.DueDate)
            .ThenByDescending(workItem => workItem.UpdatedAtUtc)
            .ThenBy(workItem => workItem.Id, StringComparer.OrdinalIgnoreCase)
            .Take(query.ClampedLimit)
            .ToArray();
    }

    /// <summary>
    /// Adds an audit event.
    /// </summary>
    public CiServerAuditEvent SaveAuditEvent(CiServerAuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        auditEvents[auditEvent.Id] = auditEvent;
        return auditEvent;
    }

    /// <summary>
    /// Lists stored audit events.
    /// </summary>
    public IReadOnlyList<CiServerAuditEvent> ListAuditEvents(CiServerAuditQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return auditEvents.Values
            .Where(auditEvent => string.IsNullOrWhiteSpace(query.Action)
                || string.Equals(auditEvent.Action, query.Action, StringComparison.OrdinalIgnoreCase))
            .Where(auditEvent => string.IsNullOrWhiteSpace(query.RunId)
                || string.Equals(auditEvent.RunId, query.RunId, StringComparison.OrdinalIgnoreCase))
            .Where(auditEvent => string.IsNullOrWhiteSpace(query.CaseId)
                || string.Equals(auditEvent.CaseId, query.CaseId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(auditEvent => auditEvent.OccurredAtUtc)
            .ThenBy(auditEvent => auditEvent.Id, StringComparer.OrdinalIgnoreCase)
            .Take(query.ClampedLimit)
            .ToArray();
    }

    private static string CreateRulePackVersionKey(string rulePackId, string versionId)
    {
        return $"{rulePackId.Trim()}::{versionId.Trim()}";
    }
}
