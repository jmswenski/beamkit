using BeamKit.Release;
using BeamKit.Samples;
using BeamKit.Workflow;

namespace BeamKit.Release.Tests;

public sealed class WriteUpManifestBuilderTests
{
    [Fact]
    public void CaptureStoresPlanFingerprintsAndSnapshot()
    {
        var timestamp = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var manifest = new WriteUpManifestBuilder(new FixedTimeProvider(timestamp)).Capture(
            CreateCompleteReadiness(plan),
            new[] { new ExportRecord("ARIA", DestinationKind.RecordAndVerify, timestamp, "EXT-1") },
            new[] { new WriteUpDocument("Plan write-up", "html", timestamp) },
            new[] { new Attestation("physics-review", "complete", "physicist", timestamp) });

        Assert.Equal(plan.Id, manifest.PlanId);
        Assert.Equal(plan.Patient.Id, manifest.PatientId);
        Assert.Equal(timestamp, manifest.CapturedAtUtc);
        Assert.Equal(plan, manifest.CapturedPlanSnapshot);
        Assert.StartsWith("sha256:", manifest.PlanFingerprint, StringComparison.Ordinal);
        Assert.StartsWith("sha256:", manifest.PrescriptionFingerprint, StringComparison.Ordinal);
        Assert.False(manifest.HasOutstandingChecklistItems);
        Assert.Contains(manifest.Checklist, item => item.Key == "exports-recorded" && item.Status == ReadinessItemStatus.Complete);
        Assert.Contains(manifest.Checklist, item => item.Key == "documents-recorded" && item.Status == ReadinessItemStatus.Complete);
    }

    [Fact]
    public void CaptureMarksExportAndDocumentEvidencePendingWhenMissing()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var manifest = new WriteUpManifestBuilder(new FixedTimeProvider(DateTimeOffset.UnixEpoch)).Capture(CreateCompleteReadiness(plan));

        Assert.True(manifest.HasOutstandingChecklistItems);
        Assert.Contains(manifest.OutstandingChecklistItems, item => item.Key == "exports-recorded");
        Assert.Contains(manifest.OutstandingChecklistItems, item => item.Key == "documents-recorded");
    }

    [Fact]
    public void CaptureRejectsReadinessStateForAnotherPlan()
    {
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var readiness = new PlanReadinessState("OtherPlan", Array.Empty<ReadinessItem>());

        var exception = Assert.Throws<ArgumentException>(() =>
            new WriteUpManifestBuilder(new FixedTimeProvider(DateTimeOffset.UnixEpoch)).Capture(plan, readiness));

        Assert.Contains("Readiness state plan id", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ManifestRoundTripsThroughJson()
    {
        var timestamp = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var manifest = new WriteUpManifestBuilder(new FixedTimeProvider(timestamp)).Capture(
            CreateCompleteReadiness(plan),
            new[] { new ExportRecord("PACS", DestinationKind.Pacs, timestamp, externalVersionId: "V1") },
            new[] { new WriteUpDocument("DVH summary", "markdown", timestamp, "sha256:abc") },
            new[] { new Attestation("documents-printed", "true", attestedAtUtc: timestamp) });

        var roundTripped = WriteUpManifestStore.FromJson(WriteUpManifestStore.ToJson(manifest));

        Assert.Equal(manifest.PlanFingerprint, roundTripped.PlanFingerprint);
        Assert.Equal(manifest.CapturedPlanSnapshot.Id, roundTripped.CapturedPlanSnapshot.Id);
        Assert.Equal(manifest.Exports.Single().DestinationSystem, roundTripped.Exports.Single().DestinationSystem);
        Assert.Equal(manifest.Documents.Single().Name, roundTripped.Documents.Single().Name);
        Assert.Equal(manifest.Attestations.Single().Key, roundTripped.Attestations.Single().Key);
    }

    private static PlanReadinessInput CreateCompleteReadiness(BeamKit.Core.Domain.Plan plan)
    {
        return new PlanReadinessInput(plan)
        {
            CtImported = true,
            OptimizationFinished = true,
            PhysicsQaComplete = true,
            PhysicianApprovalComplete = true,
            TreatmentReady = true
        };
    }
}
