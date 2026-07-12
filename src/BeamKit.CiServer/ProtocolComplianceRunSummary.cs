namespace BeamKit.CiServer;

/// <summary>
/// API-safe summary of a protocol compliance run.
/// </summary>
public sealed record ProtocolComplianceRunSummary
{
    /// <summary>
    /// Creates a summary from a persisted run.
    /// </summary>
    public ProtocolComplianceRunSummary(ProtocolComplianceRunRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        Id = record.Id;
        CreatedAtUtc = record.CreatedAtUtc;
        Status = record.Status;
        PlanId = record.PlanId;
        PatientId = record.PatientId;
        CourseId = record.CourseId;
        DiseaseSite = record.DiseaseSite;
        InputKind = record.InputKind;
        InputSource = record.InputSource;
        RulePackId = record.RulePackId;
        VersionId = record.VersionId;
        RtpxAcceptanceId = record.RtpxAcceptanceId;
        ProtocolId = record.ProtocolId;
        ProtocolName = record.ProtocolName;
        ProtocolVersion = record.ProtocolVersion;
        PackageFingerprint = record.PackageFingerprint;
        PassCount = record.PassCount;
        WarningCount = record.WarningCount;
        FailCount = record.FailCount;
        NotEvaluableCount = record.NotEvaluableCount;
        AcceptedVarianceCount = record.AcceptedVarianceCount;
        UnresolvedBlockingCount = record.UnresolvedBlockingCount;
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
    /// Effective status after accepted variances.
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
    /// Optional disease-site label.
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
    /// RT-PX acceptance id that produced the rule-pack version.
    /// </summary>
    public string RtpxAcceptanceId { get; init; }

    /// <summary>
    /// Local accepted protocol id.
    /// </summary>
    public string ProtocolId { get; init; }

    /// <summary>
    /// Local accepted protocol name.
    /// </summary>
    public string ProtocolName { get; init; }

    /// <summary>
    /// Local accepted protocol version.
    /// </summary>
    public string ProtocolVersion { get; init; }

    /// <summary>
    /// Fingerprint of the accepted RT-PX package.
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
}
