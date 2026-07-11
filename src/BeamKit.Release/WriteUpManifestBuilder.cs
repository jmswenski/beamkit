using BeamKit.ChangeDetection;
using BeamKit.Core.Domain;
using BeamKit.Workflow;

namespace BeamKit.Release;

/// <summary>
/// Builds write-up manifests from vendor-neutral plan and workflow evidence.
/// </summary>
public sealed class WriteUpManifestBuilder
{
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Creates a manifest builder.
    /// </summary>
    public WriteUpManifestBuilder(TimeProvider? timeProvider = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Captures a manifest from explicit readiness state and optional attested records.
    /// </summary>
    public WriteUpManifest Capture(
        Plan plan,
        PlanReadinessState? readinessState = null,
        IEnumerable<ExportRecord>? exports = null,
        IEnumerable<WriteUpDocument>? documents = null,
        IEnumerable<Attestation>? attestations = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var capturedExports = exports?.ToArray() ?? Array.Empty<ExportRecord>();
        var capturedDocuments = documents?.ToArray() ?? Array.Empty<WriteUpDocument>();
        var checklist = BuildChecklist(plan, readinessState, capturedExports, capturedDocuments);

        return new WriteUpManifest(
            plan.Id,
            plan.Patient.Id,
            plan.CourseId,
            PlanFingerprint.Compute(plan),
            PlanFingerprint.Compute(plan.Prescription),
            timeProvider.GetUtcNow(),
            plan,
            checklist,
            capturedExports,
            capturedDocuments,
            attestations,
            plan.DiseaseSite);
    }

    /// <summary>
    /// Captures a manifest by evaluating readiness input before capture.
    /// </summary>
    public WriteUpManifest Capture(
        PlanReadinessInput readinessInput,
        IEnumerable<ExportRecord>? exports = null,
        IEnumerable<WriteUpDocument>? documents = null,
        IEnumerable<Attestation>? attestations = null)
    {
        ArgumentNullException.ThrowIfNull(readinessInput);

        var readinessState = new PlanReadinessEvaluator().Evaluate(readinessInput);
        return Capture(readinessInput.Plan, readinessState, exports, documents, attestations);
    }

    private static IReadOnlyList<ReadinessItem> BuildChecklist(
        Plan plan,
        PlanReadinessState? readinessState,
        IReadOnlyCollection<ExportRecord> exports,
        IReadOnlyCollection<WriteUpDocument> documents)
    {
        var workflowState = readinessState ?? new PlanReadinessEvaluator().Evaluate(new PlanReadinessInput(plan));
        if (!string.Equals(workflowState.PlanId, plan.Id, StringComparison.Ordinal))
        {
            throw new ArgumentException("Readiness state plan id must match the captured plan id.", nameof(readinessState));
        }

        var items = workflowState.Items.ToList();
        items.Add(new ReadinessItem(
            "exports-recorded",
            "Exports Recorded",
            exports.Count > 0 ? ReadinessItemStatus.Complete : ReadinessItemStatus.Pending,
            exports.Count > 0 ? null : "No export records were supplied for this write-up manifest."));
        items.Add(new ReadinessItem(
            "documents-recorded",
            "Documents Recorded",
            documents.Count > 0 ? ReadinessItemStatus.Complete : ReadinessItemStatus.Pending,
            documents.Count > 0 ? null : "No document records were supplied for this write-up manifest."));
        items.Add(new ReadinessItem(
            "writeup-captured",
            "Write-Up Evidence Captured",
            ReadinessItemStatus.Complete));

        return items;
    }
}
