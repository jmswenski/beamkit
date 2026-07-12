using BeamKit.Check;

namespace BeamKit.CiServer;

/// <summary>
/// Full protocol compliance report for a plan checked against an active RT-PX-derived rule pack.
/// </summary>
public sealed record ProtocolComplianceReport
{
    /// <summary>
    /// Creates a protocol compliance report.
    /// </summary>
    public ProtocolComplianceReport(
        string runId,
        DateTimeOffset createdAtUtc,
        string planId,
        string patientId,
        string courseId,
        string? diseaseSite,
        string inputKind,
        string inputSource,
        string rulePackId,
        string versionId,
        string rtpxAcceptanceId,
        string protocolId,
        string protocolName,
        string protocolVersion,
        string packageFingerprint,
        IReadOnlyList<ProtocolComplianceFinding> findings,
        IReadOnlyList<ProtocolComplianceVariance> acceptedVariances,
        BeamKitCheckReport checkReport)
    {
        RunId = CiServerText.Required(runId, nameof(runId));
        CreatedAtUtc = createdAtUtc;
        PlanId = CiServerText.Required(planId, nameof(planId));
        PatientId = CiServerText.Required(patientId, nameof(patientId));
        CourseId = CiServerText.Required(courseId, nameof(courseId));
        DiseaseSite = CiServerText.Optional(diseaseSite);
        InputKind = CiServerText.Required(inputKind, nameof(inputKind));
        InputSource = CiServerText.Required(inputSource, nameof(inputSource));
        RulePackId = CiServerText.Required(rulePackId, nameof(rulePackId));
        VersionId = CiServerText.Required(versionId, nameof(versionId));
        RtpxAcceptanceId = CiServerText.Required(rtpxAcceptanceId, nameof(rtpxAcceptanceId));
        ProtocolId = CiServerText.Required(protocolId, nameof(protocolId));
        ProtocolName = CiServerText.Required(protocolName, nameof(protocolName));
        ProtocolVersion = CiServerText.Required(protocolVersion, nameof(protocolVersion));
        PackageFingerprint = CiServerText.Required(packageFingerprint, nameof(packageFingerprint));
        Findings = findings?.ToArray() ?? throw new ArgumentNullException(nameof(findings));
        AcceptedVariances = acceptedVariances?.ToArray() ?? throw new ArgumentNullException(nameof(acceptedVariances));
        CheckReport = checkReport ?? throw new ArgumentNullException(nameof(checkReport));
        Summary = CreateSummary(Findings, AcceptedVariances);
    }

    /// <summary>
    /// Compliance run id.
    /// </summary>
    public string RunId { get; init; }

    /// <summary>
    /// UTC timestamp when the report was generated.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

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
    public string InputKind { get; init; }

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
    /// RT-PX acceptance record that produced the active rule-pack version.
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
    /// Fingerprint of the accepted RT-PX package.
    /// </summary>
    public string PackageFingerprint { get; init; }

    /// <summary>
    /// Effective summary after variances are applied.
    /// </summary>
    public ProtocolComplianceSummary Summary { get; init; }

    /// <summary>
    /// Findings generated from plan checks, clinical goals, readiness, naming, and write-up evidence.
    /// </summary>
    public IReadOnlyList<ProtocolComplianceFinding> Findings { get; init; }

    /// <summary>
    /// Accepted variances applied to blocking findings.
    /// </summary>
    public IReadOnlyList<ProtocolComplianceVariance> AcceptedVariances { get; init; }

    /// <summary>
    /// Underlying BeamKit check report.
    /// </summary>
    public BeamKitCheckReport CheckReport { get; init; }

    /// <summary>
    /// Recalculates the summary for findings and accepted variances.
    /// </summary>
    public static ProtocolComplianceSummary CreateSummary(
        IReadOnlyList<ProtocolComplianceFinding> findings,
        IReadOnlyList<ProtocolComplianceVariance> variances)
    {
        var accepted = variances.Select(variance => variance.FindingId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unresolved = findings
            .Where(finding => finding.IsBlocking && !accepted.Contains(finding.Id))
            .ToArray();
        var status =
            unresolved.Any(finding => finding.Status == ProtocolComplianceStatus.Fail) ? ProtocolComplianceStatus.Fail :
            unresolved.Any(finding => finding.Status == ProtocolComplianceStatus.NotEvaluable) ? ProtocolComplianceStatus.NotEvaluable :
            findings.Any(finding => finding.Status == ProtocolComplianceStatus.Warning) ? ProtocolComplianceStatus.Warning :
            ProtocolComplianceStatus.Pass;

        return new ProtocolComplianceSummary(
            status,
            findings.Count(finding => finding.Status == ProtocolComplianceStatus.Pass),
            findings.Count(finding => finding.Status == ProtocolComplianceStatus.Warning),
            findings.Count(finding => finding.Status == ProtocolComplianceStatus.Fail),
            findings.Count(finding => finding.Status == ProtocolComplianceStatus.NotEvaluable),
            variances.Count,
            unresolved.Length);
    }
}
