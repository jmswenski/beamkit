using BeamKit.ChangeDetection;
using BeamKit.Release;
using BeamKit.Samples;
using BeamKit.Workflow;

namespace BeamKit.Release.Tests;

public sealed class WriteUpVerifierTests
{
    [Fact]
    public void VerifyReturnsCurrentWhenPlanFingerprintMatches()
    {
        var timestamp = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var manifest = CreateManifest(plan, timestamp);

        var report = new WriteUpVerifier(new FixedTimeProvider(timestamp.AddHours(1))).Verify(manifest, plan);

        Assert.Equal(WriteUpVerificationStatus.Current, report.Status);
        Assert.False(report.HasBlockingIssues);
        Assert.Empty(report.ChangeReport.Changes);
    }

    [Fact]
    public void VerifyReturnsStaleWhenPlanChanges()
    {
        var timestamp = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var changed = plan with
        {
            Prescription = plan.Prescription with { FractionCount = plan.Prescription.FractionCount + 1 }
        };
        var manifest = CreateManifest(plan, timestamp);

        var report = new WriteUpVerifier(new FixedTimeProvider(timestamp.AddHours(1))).Verify(manifest, changed);

        Assert.Equal(WriteUpVerificationStatus.Stale, report.Status);
        Assert.True(report.HasBlockingIssues);
        Assert.Contains(report.ChangeReport.Changes, change => change.Subject == "Prescription.FractionCount");
    }

    [Fact]
    public void VerifyCanBeStaleEvenWhenTolerantDiffHasNoEntries()
    {
        var timestamp = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var changed = plan with
        {
            Prescription = plan.Prescription with { TotalDoseGy = plan.Prescription.TotalDoseGy + 0.001m }
        };
        var manifest = CreateManifest(plan, timestamp);

        var report = new WriteUpVerifier(new FixedTimeProvider(timestamp.AddHours(1))).Verify(manifest, changed);

        Assert.Equal(WriteUpVerificationStatus.Stale, report.Status);
        Assert.Empty(report.ChangeReport.Changes);
    }

    [Fact]
    public void VerifyReturnsStaleWhenManifestFingerprintDoesNotMatchCapturedSnapshot()
    {
        var timestamp = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var changed = plan with
        {
            Prescription = plan.Prescription with { TotalDoseGy = plan.Prescription.TotalDoseGy + 1m }
        };
        var manifest = CreateManifest(plan, timestamp) with
        {
            PlanFingerprint = PlanFingerprint.Compute(changed)
        };

        var report = new WriteUpVerifier(new FixedTimeProvider(timestamp.AddHours(1))).Verify(manifest, changed);

        Assert.Equal(WriteUpVerificationStatus.Stale, report.Status);
        Assert.NotEqual(report.ManifestPlanFingerprint, report.CapturedSnapshotFingerprint);
        Assert.Equal(report.ManifestPlanFingerprint, report.CurrentPlanFingerprint);
    }

    private static WriteUpManifest CreateManifest(BeamKit.Core.Domain.Plan plan, DateTimeOffset timestamp)
    {
        return new WriteUpManifestBuilder(new FixedTimeProvider(timestamp)).Capture(
            new PlanReadinessInput(plan)
            {
                CtImported = true,
                OptimizationFinished = true,
                PhysicsQaComplete = true,
                PhysicianApprovalComplete = true,
                TreatmentReady = true
            },
            new[] { new ExportRecord("ARIA", DestinationKind.RecordAndVerify, timestamp) },
            new[] { new WriteUpDocument("Plan write-up", "html", timestamp) });
    }
}
