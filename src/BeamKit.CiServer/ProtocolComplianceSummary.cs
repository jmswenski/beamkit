namespace BeamKit.CiServer;

/// <summary>
/// Counts and effective status for a protocol compliance run.
/// </summary>
public sealed record ProtocolComplianceSummary
{
    /// <summary>
    /// Creates a compliance summary from findings and variances.
    /// </summary>
    public ProtocolComplianceSummary(
        ProtocolComplianceStatus status,
        int passCount,
        int warningCount,
        int failCount,
        int notEvaluableCount,
        int acceptedVarianceCount,
        int unresolvedBlockingCount)
    {
        Status = status;
        PassCount = passCount;
        WarningCount = warningCount;
        FailCount = failCount;
        NotEvaluableCount = notEvaluableCount;
        AcceptedVarianceCount = acceptedVarianceCount;
        UnresolvedBlockingCount = unresolvedBlockingCount;
    }

    /// <summary>
    /// Effective compliance status after accepted variances are applied.
    /// </summary>
    public ProtocolComplianceStatus Status { get; init; }

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
    /// Number of unresolved blocking findings after accepted variances are applied.
    /// </summary>
    public int UnresolvedBlockingCount { get; init; }
}
