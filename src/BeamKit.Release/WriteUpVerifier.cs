using BeamKit.ChangeDetection;
using BeamKit.Core.Domain;

namespace BeamKit.Release;

/// <summary>
/// Verifies whether a current plan still matches captured write-up evidence.
/// </summary>
public sealed class WriteUpVerifier
{
    private readonly TimeProvider timeProvider;
    private readonly PlanChangeDetector detector;

    /// <summary>
    /// Creates a write-up verifier.
    /// </summary>
    public WriteUpVerifier(TimeProvider? timeProvider = null, PlanChangeDetector? detector = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.detector = detector ?? new PlanChangeDetector();
    }

    /// <summary>
    /// Verifies a current plan snapshot against a captured manifest.
    /// </summary>
    public WriteUpVerificationReport Verify(
        WriteUpManifest manifest,
        Plan currentPlan,
        PlanChangeDetectionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(currentPlan);

        var capturedSnapshotFingerprint = PlanFingerprint.Compute(manifest.CapturedPlanSnapshot);
        var currentFingerprint = PlanFingerprint.Compute(currentPlan);
        var changeReport = detector.Compare(manifest.CapturedPlanSnapshot, currentPlan, options);
        return new WriteUpVerificationReport(
            manifest.PlanId,
            currentPlan.Id,
            manifest.PlanFingerprint,
            capturedSnapshotFingerprint,
            currentFingerprint,
            manifest.CapturedAtUtc,
            timeProvider.GetUtcNow(),
            changeReport);
    }
}
