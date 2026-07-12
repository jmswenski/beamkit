namespace BeamKit.CiServer;

/// <summary>
/// Persisted protocol compliance run and export artifacts.
/// </summary>
public sealed record ProtocolComplianceRunRecord
{
    /// <summary>
    /// Creates a persisted protocol compliance run.
    /// </summary>
    public ProtocolComplianceRunRecord(
        string id,
        DateTimeOffset createdAtUtc,
        ProtocolComplianceStatus status,
        string planId,
        string patientId,
        string courseId,
        string? diseaseSite,
        CiRunInputKind inputKind,
        string inputSource,
        string rulePackId,
        string versionId,
        string rtpxAcceptanceId,
        string protocolId,
        string protocolName,
        string protocolVersion,
        string packageFingerprint,
        int passCount,
        int warningCount,
        int failCount,
        int notEvaluableCount,
        int acceptedVarianceCount,
        int unresolvedBlockingCount,
        string reportJson,
        string markdownReport,
        string planSnapshotJson)
    {
        Id = CiServerText.Required(id, nameof(id));
        CreatedAtUtc = createdAtUtc;
        Status = status;
        PlanId = CiServerText.Required(planId, nameof(planId));
        PatientId = CiServerText.Required(patientId, nameof(patientId));
        CourseId = CiServerText.Required(courseId, nameof(courseId));
        DiseaseSite = CiServerText.Optional(diseaseSite);
        InputKind = inputKind;
        InputSource = CiServerText.Required(inputSource, nameof(inputSource));
        RulePackId = CiServerText.Required(rulePackId, nameof(rulePackId));
        VersionId = CiServerText.Required(versionId, nameof(versionId));
        RtpxAcceptanceId = CiServerText.Required(rtpxAcceptanceId, nameof(rtpxAcceptanceId));
        ProtocolId = CiServerText.Required(protocolId, nameof(protocolId));
        ProtocolName = CiServerText.Required(protocolName, nameof(protocolName));
        ProtocolVersion = CiServerText.Required(protocolVersion, nameof(protocolVersion));
        PackageFingerprint = CiServerText.Required(packageFingerprint, nameof(packageFingerprint));
        PassCount = passCount;
        WarningCount = warningCount;
        FailCount = failCount;
        NotEvaluableCount = notEvaluableCount;
        AcceptedVarianceCount = acceptedVarianceCount;
        UnresolvedBlockingCount = unresolvedBlockingCount;
        ReportJson = CiServerText.Required(reportJson, nameof(reportJson));
        MarkdownReport = CiServerText.Required(markdownReport, nameof(markdownReport));
        PlanSnapshotJson = CiServerText.Required(planSnapshotJson, nameof(planSnapshotJson));
    }

    /// <summary>
    /// Compliance run id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// UTC timestamp when the run was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Effective compliance status after variances.
    /// </summary>
    public ProtocolComplianceStatus Status { get; init; }

    /// <summary>
    /// Evaluated plan id.
    /// </summary>
    public string PlanId { get; init; }

    /// <summary>
    /// Evaluated patient id.
    /// </summary>
    public string PatientId { get; init; }

    /// <summary>
    /// Evaluated course id.
    /// </summary>
    public string CourseId { get; init; }

    /// <summary>
    /// Disease site from the plan or protocol.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Source kind for the evaluated plan.
    /// </summary>
    public CiRunInputKind InputKind { get; init; }

    /// <summary>
    /// Source label for the evaluated plan.
    /// </summary>
    public string InputSource { get; init; }

    /// <summary>
    /// Active managed rule-pack id.
    /// </summary>
    public string RulePackId { get; init; }

    /// <summary>
    /// Active managed rule-pack version id.
    /// </summary>
    public string VersionId { get; init; }

    /// <summary>
    /// RT-PX acceptance id bound to this run.
    /// </summary>
    public string RtpxAcceptanceId { get; init; }

    /// <summary>
    /// Local protocol id.
    /// </summary>
    public string ProtocolId { get; init; }

    /// <summary>
    /// Protocol name.
    /// </summary>
    public string ProtocolName { get; init; }

    /// <summary>
    /// Protocol version.
    /// </summary>
    public string ProtocolVersion { get; init; }

    /// <summary>
    /// Accepted RT-PX package fingerprint.
    /// </summary>
    public string PackageFingerprint { get; init; }

    /// <summary>
    /// Number of passing findings.
    /// </summary>
    public int PassCount { get; init; }

    /// <summary>
    /// Number of warning findings.
    /// </summary>
    public int WarningCount { get; init; }

    /// <summary>
    /// Number of failing findings before variance application.
    /// </summary>
    public int FailCount { get; init; }

    /// <summary>
    /// Number of not-evaluable findings before variance application.
    /// </summary>
    public int NotEvaluableCount { get; init; }

    /// <summary>
    /// Number of accepted variances.
    /// </summary>
    public int AcceptedVarianceCount { get; init; }

    /// <summary>
    /// Number of unresolved blocking findings after variance application.
    /// </summary>
    public int UnresolvedBlockingCount { get; init; }

    /// <summary>
    /// Serialized protocol compliance report.
    /// </summary>
    public string ReportJson { get; init; }

    /// <summary>
    /// Markdown protocol compliance packet.
    /// </summary>
    public string MarkdownReport { get; init; }

    /// <summary>
    /// Vendor-neutral BeamKit plan snapshot JSON used for this run.
    /// </summary>
    public string PlanSnapshotJson { get; init; }
}
