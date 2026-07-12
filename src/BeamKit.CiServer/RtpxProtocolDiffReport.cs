namespace BeamKit.CiServer;

/// <summary>
/// Protocol-level difference report between a draft RT-PX package and the active accepted package.
/// </summary>
public sealed record RtpxProtocolDiffReport(
    string DraftProtocolId,
    string? ComparedToAcceptanceId,
    string? ComparedToVersionId,
    string? ComparedToFingerprint,
    IReadOnlyList<RtpxProtocolDiffChange> Changes)
{
    /// <summary>
    /// Indicates whether this draft has no active accepted RT-PX package to compare against.
    /// </summary>
    public bool IsInitial => string.IsNullOrWhiteSpace(ComparedToAcceptanceId);

    /// <summary>
    /// Number of changes in the report.
    /// </summary>
    public int ChangeCount => Changes.Count;
}

/// <summary>
/// One protocol-level change.
/// </summary>
public sealed record RtpxProtocolDiffChange(
    string Category,
    string Key,
    string ChangeType,
    string Severity,
    string Message,
    string? Before,
    string? After)
{
    /// <summary>
    /// Stable id for acknowledgement and review tracking.
    /// </summary>
    public string Id => $"{Category}:{Key}:{ChangeType}".ToLowerInvariant();
}
