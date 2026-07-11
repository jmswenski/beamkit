using BeamKit.ChangeDetection;

namespace BeamKit.Release;

/// <summary>
/// Result of verifying a current plan snapshot against a captured write-up manifest.
/// </summary>
public sealed record WriteUpVerificationReport
{
    /// <summary>
    /// Creates a write-up verification report.
    /// </summary>
    public WriteUpVerificationReport(
        string manifestPlanId,
        string currentPlanId,
        string manifestPlanFingerprint,
        string capturedSnapshotFingerprint,
        string currentPlanFingerprint,
        DateTimeOffset capturedAtUtc,
        DateTimeOffset verifiedAtUtc,
        PlanChangeReport changeReport)
    {
        ManifestPlanId = ReleaseText.Required(manifestPlanId, nameof(manifestPlanId));
        CurrentPlanId = ReleaseText.Required(currentPlanId, nameof(currentPlanId));
        ManifestPlanFingerprint = ReleaseText.Required(manifestPlanFingerprint, nameof(manifestPlanFingerprint));
        CapturedSnapshotFingerprint = ReleaseText.Required(capturedSnapshotFingerprint, nameof(capturedSnapshotFingerprint));
        CurrentPlanFingerprint = ReleaseText.Required(currentPlanFingerprint, nameof(currentPlanFingerprint));
        CapturedAtUtc = capturedAtUtc;
        VerifiedAtUtc = verifiedAtUtc;
        ChangeReport = changeReport ?? throw new ArgumentNullException(nameof(changeReport));
    }

    /// <summary>
    /// Plan identifier captured in the manifest.
    /// </summary>
    public string ManifestPlanId { get; init; }

    /// <summary>
    /// Plan identifier for the current plan snapshot.
    /// </summary>
    public string CurrentPlanId { get; init; }

    /// <summary>
    /// Fingerprint captured in the manifest.
    /// </summary>
    public string ManifestPlanFingerprint { get; init; }

    /// <summary>
    /// Fingerprint recomputed from the captured plan snapshot embedded in the manifest.
    /// </summary>
    public string CapturedSnapshotFingerprint { get; init; }

    /// <summary>
    /// Fingerprint computed for the current plan snapshot.
    /// </summary>
    public string CurrentPlanFingerprint { get; init; }

    /// <summary>
    /// UTC timestamp when the manifest was captured.
    /// </summary>
    public DateTimeOffset CapturedAtUtc { get; init; }

    /// <summary>
    /// UTC timestamp when the verification was performed.
    /// </summary>
    public DateTimeOffset VerifiedAtUtc { get; init; }

    /// <summary>
    /// Tolerant change report used to explain detected drift.
    /// </summary>
    public PlanChangeReport ChangeReport { get; init; }

    /// <summary>
    /// Current/stale status based on exact fingerprint equality.
    /// </summary>
    public WriteUpVerificationStatus Status =>
        string.Equals(ManifestPlanFingerprint, CurrentPlanFingerprint, StringComparison.Ordinal)
        && string.Equals(ManifestPlanFingerprint, CapturedSnapshotFingerprint, StringComparison.Ordinal)
            ? WriteUpVerificationStatus.Current
            : WriteUpVerificationStatus.Stale;

    /// <summary>
    /// Indicates whether the write-up evidence is stale relative to the current plan.
    /// </summary>
    public bool IsStale => Status == WriteUpVerificationStatus.Stale;

    /// <summary>
    /// Indicates whether the verification should block an automated downstream workflow.
    /// </summary>
    public bool HasBlockingIssues => IsStale || ChangeReport.HasBlockingChanges;
}
