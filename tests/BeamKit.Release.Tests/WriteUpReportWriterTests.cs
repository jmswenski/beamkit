using BeamKit.Release;
using BeamKit.Samples;
using BeamKit.Workflow;

namespace BeamKit.Release.Tests;

public sealed class WriteUpReportWriterTests
{
    [Fact]
    public void ManifestMarkdownIncludesSafetyNoteAndEvidenceSections()
    {
        var manifest = CreateManifest();

        var markdown = WriteUpReportWriter.WriteManifest(manifest, WriteUpReportFormat.Markdown);

        Assert.Contains("Advisory consistency evidence only", markdown, StringComparison.Ordinal);
        Assert.Contains("## Checklist", markdown, StringComparison.Ordinal);
        Assert.Contains("## Export Evidence", markdown, StringComparison.Ordinal);
        Assert.Contains("## Document Evidence", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void VerificationMarkdownIncludesStaleFingerprintMessage()
    {
        var manifest = CreateManifest();
        var changed = manifest.CapturedPlanSnapshot with { DiseaseSite = "Different label" };
        var report = new WriteUpVerifier(new FixedTimeProvider(manifest.CapturedAtUtc.AddHours(1))).Verify(manifest, changed);

        var markdown = WriteUpReportWriter.WriteVerification(report, WriteUpReportFormat.Markdown);

        Assert.Contains("Status: `Stale`", markdown, StringComparison.Ordinal);
        Assert.Contains("exact fingerprint changed", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void ManifestHtmlEncodesAttestedValues()
    {
        var timestamp = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
        var manifest = new WriteUpManifestBuilder(new FixedTimeProvider(timestamp)).Capture(
            new PlanReadinessInput(plan)
            {
                CtImported = true,
                OptimizationFinished = true,
                PhysicsQaComplete = true,
                PhysicianApprovalComplete = true,
                TreatmentReady = true
            },
            new[] { new ExportRecord("<ARIA>", DestinationKind.RecordAndVerify, timestamp) },
            new[] { new WriteUpDocument("<Packet>", "html", timestamp) },
            new[] { new Attestation("note", "<checked>", "dosimetry", timestamp) });

        var html = WriteUpReportWriter.WriteManifest(manifest, WriteUpReportFormat.Html);

        Assert.Contains("&lt;ARIA&gt;", html, StringComparison.Ordinal);
        Assert.Contains("&lt;Packet&gt;", html, StringComparison.Ordinal);
        Assert.Contains("&lt;checked&gt;", html, StringComparison.Ordinal);
    }

    private static WriteUpManifest CreateManifest()
    {
        var timestamp = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);
        var plan = SyntheticPlanFactory.CreateHeadAndNeckPlan();
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
