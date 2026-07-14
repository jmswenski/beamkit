namespace BeamKit.CiServer;

/// <summary>
/// Stores hosted BeamKit CI run records and artifacts.
/// </summary>
public interface ICiRunStore
{
    /// <summary>
    /// Adds or replaces a run record.
    /// </summary>
    HostedCiRunRecord Save(HostedCiRunRecord record);

    /// <summary>
    /// Finds a run by id.
    /// </summary>
    HostedCiRunSummary? Find(string id);

    /// <summary>
    /// Finds the stored full artifact JSON for a run.
    /// </summary>
    string? FindArtifactJson(string id);

    /// <summary>
    /// Finds the stored vendor-neutral BeamKit plan snapshot JSON for a run.
    /// </summary>
    string? FindPlanSnapshotJson(string id);

    /// <summary>
    /// Lists runs matching the supplied query.
    /// </summary>
    IReadOnlyList<HostedCiRunSummary> List(CiRunQuery query);

    /// <summary>
    /// Adds or replaces a promoted baseline.
    /// </summary>
    CiRunBaseline SaveBaseline(CiRunBaseline baseline);

    /// <summary>
    /// Finds the promoted baseline for a case key.
    /// </summary>
    CiRunBaseline? FindBaseline(string caseId);

    /// <summary>
    /// Lists promoted baselines.
    /// </summary>
    IReadOnlyList<CiRunBaseline> ListBaselines();

    /// <summary>
    /// Adds or replaces a managed rule-pack version.
    /// </summary>
    CiServerManagedRulePackVersion SaveRulePackVersion(CiServerManagedRulePackVersion version);

    /// <summary>
    /// Finds a managed rule-pack version.
    /// </summary>
    CiServerManagedRulePackVersion? FindRulePackVersion(string rulePackId, string versionId);

    /// <summary>
    /// Finds the active managed version for a rule-pack id.
    /// </summary>
    CiServerManagedRulePackVersion? FindActiveRulePackVersion(string rulePackId);

    /// <summary>
    /// Lists managed rule-pack versions.
    /// </summary>
    IReadOnlyList<CiServerManagedRulePackVersionSummary> ListRulePackVersions(string? rulePackId = null);

    /// <summary>
    /// Promotes one managed rule-pack version as active.
    /// </summary>
    CiServerManagedRulePackVersion PromoteRulePackVersion(
        string rulePackId,
        string versionId,
        DateTimeOffset activatedAtUtc,
        string? activatedBy = null,
        string? note = null);

    /// <summary>
    /// Adds or replaces a managed naming-dictionary version.
    /// </summary>
    CiServerManagedNamingDictionaryVersion SaveNamingDictionaryVersion(CiServerManagedNamingDictionaryVersion version);

    /// <summary>
    /// Finds a managed naming-dictionary version.
    /// </summary>
    CiServerManagedNamingDictionaryVersion? FindNamingDictionaryVersion(string dictionaryId, string versionId);

    /// <summary>
    /// Finds the active managed version for a naming-dictionary id.
    /// </summary>
    CiServerManagedNamingDictionaryVersion? FindActiveNamingDictionaryVersion(string dictionaryId);

    /// <summary>
    /// Lists managed naming-dictionary versions.
    /// </summary>
    IReadOnlyList<CiServerManagedNamingDictionaryVersionSummary> ListNamingDictionaryVersions(string? dictionaryId = null);

    /// <summary>
    /// Promotes one managed naming-dictionary version as active.
    /// </summary>
    CiServerManagedNamingDictionaryVersion PromoteNamingDictionaryVersion(
        string dictionaryId,
        string versionId,
        DateTimeOffset activatedAtUtc,
        string? activatedBy = null,
        string? note = null);

    /// <summary>
    /// Adds or replaces a managed machine-profile version.
    /// </summary>
    CiServerManagedMachineProfileVersion SaveMachineProfileVersion(CiServerManagedMachineProfileVersion version);

    /// <summary>
    /// Finds a managed machine-profile version.
    /// </summary>
    CiServerManagedMachineProfileVersion? FindMachineProfileVersion(string machineProfileId, string versionId);

    /// <summary>
    /// Finds the active managed version for a machine-profile id.
    /// </summary>
    CiServerManagedMachineProfileVersion? FindActiveMachineProfileVersion(string machineProfileId);

    /// <summary>
    /// Lists managed machine-profile versions.
    /// </summary>
    IReadOnlyList<CiServerManagedMachineProfileVersionSummary> ListMachineProfileVersions(string? machineProfileId = null);

    /// <summary>
    /// Promotes one managed machine-profile version as active.
    /// </summary>
    CiServerManagedMachineProfileVersion PromoteMachineProfileVersion(
        string machineProfileId,
        string versionId,
        DateTimeOffset activatedAtUtc,
        string? activatedBy = null,
        string? note = null);

    /// <summary>
    /// Adds or replaces a clinical policy-set version.
    /// </summary>
    CiServerClinicalPolicySetVersion SaveClinicalPolicySetVersion(CiServerClinicalPolicySetVersion version);

    /// <summary>
    /// Finds a clinical policy-set version.
    /// </summary>
    CiServerClinicalPolicySetVersion? FindClinicalPolicySetVersion(string policySetId, string versionId);

    /// <summary>
    /// Finds the active clinical policy-set version for a policy-set id.
    /// </summary>
    CiServerClinicalPolicySetVersion? FindActiveClinicalPolicySetVersion(string policySetId);

    /// <summary>
    /// Lists clinical policy-set versions.
    /// </summary>
    IReadOnlyList<CiServerClinicalPolicySetVersionSummary> ListClinicalPolicySetVersions(string? policySetId = null);

    /// <summary>
    /// Promotes one clinical policy-set version as active.
    /// </summary>
    CiServerClinicalPolicySetVersion PromoteClinicalPolicySetVersion(
        string policySetId,
        string versionId,
        DateTimeOffset activatedAtUtc,
        string? activatedBy = null,
        string? note = null);

    /// <summary>
    /// Adds or replaces an RT-PX package acceptance record.
    /// </summary>
    CiServerRtpxAcceptanceRecord SaveRtpxAcceptance(CiServerRtpxAcceptanceRecord record);

    /// <summary>
    /// Finds an RT-PX package acceptance record.
    /// </summary>
    CiServerRtpxAcceptanceRecord? FindRtpxAcceptance(string id);

    /// <summary>
    /// Lists recent RT-PX package acceptance records.
    /// </summary>
    IReadOnlyList<CiServerRtpxAcceptanceSummary> ListRtpxAcceptances(int limit = 50);

    /// <summary>
    /// Adds or replaces a protocol compliance run.
    /// </summary>
    ProtocolComplianceRunRecord SaveProtocolComplianceRun(ProtocolComplianceRunRecord record);

    /// <summary>
    /// Finds a protocol compliance run by id.
    /// </summary>
    ProtocolComplianceRunRecord? FindProtocolComplianceRun(string id);

    /// <summary>
    /// Lists recent protocol compliance runs.
    /// </summary>
    IReadOnlyList<ProtocolComplianceRunSummary> ListProtocolComplianceRuns(int limit = 50);

    /// <summary>
    /// Adds or replaces a case work item.
    /// </summary>
    CaseWorkItem SaveWorkItem(CaseWorkItem workItem);

    /// <summary>
    /// Finds a case work item by id.
    /// </summary>
    CaseWorkItem? FindWorkItem(string id);

    /// <summary>
    /// Lists case work items matching the supplied query.
    /// </summary>
    IReadOnlyList<CaseWorkItem> ListWorkItems(CaseWorkItemQuery query);

    /// <summary>
    /// Adds an audit event.
    /// </summary>
    CiServerAuditEvent SaveAuditEvent(CiServerAuditEvent auditEvent);

    /// <summary>
    /// Lists stored audit events.
    /// </summary>
    IReadOnlyList<CiServerAuditEvent> ListAuditEvents(CiServerAuditQuery query);
}
