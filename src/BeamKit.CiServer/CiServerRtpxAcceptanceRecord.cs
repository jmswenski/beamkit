namespace BeamKit.CiServer;

/// <summary>
/// Persisted RT-PX package acceptance evidence captured by the CI server.
/// </summary>
public sealed record CiServerRtpxAcceptanceRecord
{
    /// <summary>
    /// Creates a persisted RT-PX acceptance record.
    /// </summary>
    public CiServerRtpxAcceptanceRecord(
        string id,
        DateTimeOffset createdAtUtc,
        string institution,
        string packagePath,
        string outputDirectory,
        bool accepted,
        bool promoted,
        string? rulePackId,
        string? versionId,
        string sourceProtocolId,
        string sourceProtocolName,
        string sourceProtocolVersion,
        string localProtocolId,
        string packageFingerprint,
        string institutionProfileFingerprint,
        string? esapiSnapshotFingerprint,
        bool hasEsapiEvidence,
        int errorCount,
        int warningCount,
        string reportJson,
        string? safetyEvidenceJson)
    {
        Id = CiServerText.Required(id, nameof(id));
        CreatedAtUtc = createdAtUtc;
        Institution = CiServerText.Required(institution, nameof(institution));
        PackagePath = CiServerText.Required(packagePath, nameof(packagePath));
        OutputDirectory = CiServerText.Required(outputDirectory, nameof(outputDirectory));
        Accepted = accepted;
        Promoted = promoted;
        RulePackId = CiServerText.Optional(rulePackId);
        VersionId = CiServerText.Optional(versionId);
        SourceProtocolId = CiServerText.Required(sourceProtocolId, nameof(sourceProtocolId));
        SourceProtocolName = CiServerText.Required(sourceProtocolName, nameof(sourceProtocolName));
        SourceProtocolVersion = CiServerText.Required(sourceProtocolVersion, nameof(sourceProtocolVersion));
        LocalProtocolId = CiServerText.Required(localProtocolId, nameof(localProtocolId));
        PackageFingerprint = CiServerText.Required(packageFingerprint, nameof(packageFingerprint));
        InstitutionProfileFingerprint = CiServerText.Required(institutionProfileFingerprint, nameof(institutionProfileFingerprint));
        EsapiSnapshotFingerprint = CiServerText.Optional(esapiSnapshotFingerprint);
        HasEsapiEvidence = hasEsapiEvidence;
        ErrorCount = errorCount;
        WarningCount = warningCount;
        ReportJson = CiServerText.Required(reportJson, nameof(reportJson));
        SafetyEvidenceJson = CiServerText.Optional(safetyEvidenceJson);
    }

    /// <summary>
    /// Acceptance record id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// UTC timestamp when the server created the acceptance record.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Institution that accepted or reviewed the protocol package.
    /// </summary>
    public string Institution { get; init; }

    /// <summary>
    /// Server-local package path used for acceptance.
    /// </summary>
    public string PackagePath { get; init; }

    /// <summary>
    /// Server-local output directory containing acceptance artifacts.
    /// </summary>
    public string OutputDirectory { get; init; }

    /// <summary>
    /// Indicates whether acceptance completed without blocking errors.
    /// </summary>
    public bool Accepted { get; init; }

    /// <summary>
    /// Indicates whether the generated managed rule-pack version was promoted active.
    /// </summary>
    public bool Promoted { get; init; }

    /// <summary>
    /// Managed CI-server rule-pack id created from the accepted package.
    /// </summary>
    public string? RulePackId { get; init; }

    /// <summary>
    /// Managed CI-server rule-pack version id created from the accepted package.
    /// </summary>
    public string? VersionId { get; init; }

    /// <summary>
    /// Source RT-PX protocol id.
    /// </summary>
    public string SourceProtocolId { get; init; }

    /// <summary>
    /// Source RT-PX protocol name.
    /// </summary>
    public string SourceProtocolName { get; init; }

    /// <summary>
    /// Source RT-PX protocol version.
    /// </summary>
    public string SourceProtocolVersion { get; init; }

    /// <summary>
    /// Local accepted RT-PX protocol id.
    /// </summary>
    public string LocalProtocolId { get; init; }

    /// <summary>
    /// SHA-256 fingerprint of the incoming package bytes.
    /// </summary>
    public string PackageFingerprint { get; init; }

    /// <summary>
    /// SHA-256 fingerprint of the institution profile JSON.
    /// </summary>
    public string InstitutionProfileFingerprint { get; init; }

    /// <summary>
    /// SHA-256 fingerprint of the optional ESAPI snapshot JSON.
    /// </summary>
    public string? EsapiSnapshotFingerprint { get; init; }

    /// <summary>
    /// Indicates whether ESAPI snapshot evidence was evaluated.
    /// </summary>
    public bool HasEsapiEvidence { get; init; }

    /// <summary>
    /// Number of blocking acceptance issues.
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Number of non-blocking acceptance warnings.
    /// </summary>
    public int WarningCount { get; init; }

    /// <summary>
    /// Serialized RT-PX acceptance report.
    /// </summary>
    public string ReportJson { get; init; }

    /// <summary>
    /// Serialized safety evidence package generated for the managed rule-pack version.
    /// </summary>
    public string? SafetyEvidenceJson { get; init; }
}
