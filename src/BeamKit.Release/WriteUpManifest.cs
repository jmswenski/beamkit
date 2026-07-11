using BeamKit.Core.Domain;
using BeamKit.Workflow;

namespace BeamKit.Release;

/// <summary>
/// Vendor-neutral write-up evidence captured for a plan snapshot.
/// </summary>
public sealed record WriteUpManifest
{
    /// <summary>
    /// Creates a write-up manifest.
    /// </summary>
    public WriteUpManifest(
        string planId,
        string patientId,
        string courseId,
        string planFingerprint,
        string prescriptionFingerprint,
        DateTimeOffset capturedAtUtc,
        Plan capturedPlanSnapshot,
        IEnumerable<ReadinessItem>? checklist = null,
        IEnumerable<ExportRecord>? exports = null,
        IEnumerable<WriteUpDocument>? documents = null,
        IEnumerable<Attestation>? attestations = null,
        string? diseaseSite = null)
    {
        PlanId = ReleaseText.Required(planId, nameof(planId));
        PatientId = ReleaseText.Required(patientId, nameof(patientId));
        CourseId = ReleaseText.Required(courseId, nameof(courseId));
        DiseaseSite = ReleaseText.Optional(diseaseSite);
        PlanFingerprint = ReleaseText.Required(planFingerprint, nameof(planFingerprint));
        PrescriptionFingerprint = ReleaseText.Required(prescriptionFingerprint, nameof(prescriptionFingerprint));
        CapturedAtUtc = capturedAtUtc;
        CapturedPlanSnapshot = capturedPlanSnapshot ?? throw new ArgumentNullException(nameof(capturedPlanSnapshot));
        if (!string.Equals(CapturedPlanSnapshot.Id, PlanId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Captured plan snapshot id must match the manifest plan id.", nameof(capturedPlanSnapshot));
        }

        if (!string.Equals(CapturedPlanSnapshot.Patient.Id, PatientId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Captured plan snapshot patient id must match the manifest patient id.", nameof(capturedPlanSnapshot));
        }

        if (!string.Equals(CapturedPlanSnapshot.CourseId, CourseId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Captured plan snapshot course id must match the manifest course id.", nameof(capturedPlanSnapshot));
        }

        Checklist = checklist?.ToArray() ?? Array.Empty<ReadinessItem>();
        Exports = exports?.ToArray() ?? Array.Empty<ExportRecord>();
        Documents = documents?.ToArray() ?? Array.Empty<WriteUpDocument>();
        Attestations = attestations?.ToArray() ?? Array.Empty<Attestation>();
    }

    /// <summary>
    /// Identifier of the captured plan.
    /// </summary>
    public string PlanId { get; init; }

    /// <summary>
    /// Identifier of the patient associated with the captured plan.
    /// </summary>
    public string PatientId { get; init; }

    /// <summary>
    /// Identifier of the course containing the captured plan.
    /// </summary>
    public string CourseId { get; init; }

    /// <summary>
    /// Optional disease-site label for the captured plan.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Exact deterministic fingerprint of the captured plan snapshot.
    /// </summary>
    public string PlanFingerprint { get; init; }

    /// <summary>
    /// Exact deterministic fingerprint of the captured prescription.
    /// </summary>
    public string PrescriptionFingerprint { get; init; }

    /// <summary>
    /// UTC timestamp when the manifest was captured.
    /// </summary>
    public DateTimeOffset CapturedAtUtc { get; init; }

    /// <summary>
    /// Captured BeamKit plan snapshot used for later change explanations.
    /// </summary>
    public Plan CapturedPlanSnapshot { get; init; }

    /// <summary>
    /// Readiness and write-up checklist evidence.
    /// </summary>
    public IReadOnlyList<ReadinessItem> Checklist { get; init; }

    /// <summary>
    /// Caller-supplied export evidence records.
    /// </summary>
    public IReadOnlyList<ExportRecord> Exports { get; init; }

    /// <summary>
    /// Caller-supplied write-up document records.
    /// </summary>
    public IReadOnlyList<WriteUpDocument> Documents { get; init; }

    /// <summary>
    /// Caller-supplied attestations associated with the write-up.
    /// </summary>
    public IReadOnlyList<Attestation> Attestations { get; init; }

    /// <summary>
    /// Indicates whether any checklist item is pending or blocked.
    /// </summary>
    public bool HasOutstandingChecklistItems => Checklist.Any(item =>
        item.Status is ReadinessItemStatus.Pending or ReadinessItemStatus.Blocked);

    /// <summary>
    /// Pending or blocked checklist items.
    /// </summary>
    public IReadOnlyList<ReadinessItem> OutstandingChecklistItems => Checklist
        .Where(item => item.Status is ReadinessItemStatus.Pending or ReadinessItemStatus.Blocked)
        .ToArray();
}
