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
    /// Adds an audit event.
    /// </summary>
    CiServerAuditEvent SaveAuditEvent(CiServerAuditEvent auditEvent);

    /// <summary>
    /// Lists stored audit events.
    /// </summary>
    IReadOnlyList<CiServerAuditEvent> ListAuditEvents(CiServerAuditQuery query);
}
