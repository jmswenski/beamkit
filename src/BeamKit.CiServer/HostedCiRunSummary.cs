using BeamKit.Check;

namespace BeamKit.CiServer;

/// <summary>
/// Metadata summary for a persisted hosted BeamKit CI run.
/// </summary>
public sealed record HostedCiRunSummary
{
    /// <summary>
    /// Creates a hosted CI run summary.
    /// </summary>
    public HostedCiRunSummary(
        string id,
        DateTimeOffset createdAtUtc,
        string syntheticCaseId,
        BeamKitCheckStatus status,
        int exitCode,
        string? inputSource,
        string? branch,
        string? commit,
        string? buildId,
        string planId,
        string rulePackName,
        string rulePackVersion,
        string planFingerprint,
        string prescriptionFingerprint,
        string rulePackFingerprint)
    {
        Id = CiServerText.Required(id, nameof(id));
        CreatedAtUtc = createdAtUtc;
        SyntheticCaseId = CiServerText.Required(syntheticCaseId, nameof(syntheticCaseId));
        Status = status;
        ExitCode = exitCode;
        InputSource = CiServerText.Optional(inputSource);
        Branch = CiServerText.Optional(branch);
        Commit = CiServerText.Optional(commit);
        BuildId = CiServerText.Optional(buildId);
        PlanId = CiServerText.Required(planId, nameof(planId));
        RulePackName = CiServerText.Required(rulePackName, nameof(rulePackName));
        RulePackVersion = CiServerText.Required(rulePackVersion, nameof(rulePackVersion));
        PlanFingerprint = CiServerText.Required(planFingerprint, nameof(planFingerprint));
        PrescriptionFingerprint = CiServerText.Required(prescriptionFingerprint, nameof(prescriptionFingerprint));
        RulePackFingerprint = CiServerText.Required(rulePackFingerprint, nameof(rulePackFingerprint));
    }

    /// <summary>
    /// Server run id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// UTC timestamp when the server created the run.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Synthetic case used for the run.
    /// </summary>
    public string SyntheticCaseId { get; init; }

    /// <summary>
    /// Top-level run status.
    /// </summary>
    public BeamKitCheckStatus Status { get; init; }

    /// <summary>
    /// Suggested process exit code.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Optional input source.
    /// </summary>
    public string? InputSource { get; init; }

    /// <summary>
    /// Optional branch.
    /// </summary>
    public string? Branch { get; init; }

    /// <summary>
    /// Optional commit.
    /// </summary>
    public string? Commit { get; init; }

    /// <summary>
    /// Optional build id.
    /// </summary>
    public string? BuildId { get; init; }

    /// <summary>
    /// Plan id.
    /// </summary>
    public string PlanId { get; init; }

    /// <summary>
    /// Rule-pack name.
    /// </summary>
    public string RulePackName { get; init; }

    /// <summary>
    /// Rule-pack version.
    /// </summary>
    public string RulePackVersion { get; init; }

    /// <summary>
    /// Plan fingerprint.
    /// </summary>
    public string PlanFingerprint { get; init; }

    /// <summary>
    /// Prescription fingerprint.
    /// </summary>
    public string PrescriptionFingerprint { get; init; }

    /// <summary>
    /// Rule-pack fingerprint.
    /// </summary>
    public string RulePackFingerprint { get; init; }

    /// <summary>
    /// Creates a summary from a full hosted run record.
    /// </summary>
    public static HostedCiRunSummary FromRecord(HostedCiRunRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var provenance = record.Artifact.Provenance;
        return new HostedCiRunSummary(
            record.Id,
            record.CreatedAtUtc,
            record.SyntheticCaseId,
            record.Status,
            record.ExitCode,
            provenance.InputSource,
            provenance.Branch,
            provenance.Commit,
            provenance.BuildId,
            provenance.PlanId,
            provenance.RulePackName,
            provenance.RulePackVersion,
            provenance.PlanFingerprint,
            provenance.PrescriptionFingerprint,
            provenance.RulePackFingerprint);
    }
}
