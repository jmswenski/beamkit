namespace BeamKit.Esapi;

/// <summary>
/// Read-only plan snapshot extracted from ESAPI by caller-owned code.
/// </summary>
public sealed record EsapiPlanSnapshot
{
    /// <summary>
    /// Creates an ESAPI plan snapshot.
    /// </summary>
    public EsapiPlanSnapshot(
        string patientId,
        string? patientDisplayName,
        string courseId,
        string planId,
        EsapiPrescriptionSnapshot prescription,
        IEnumerable<EsapiStructureSnapshot> structures,
        EsapiDoseGridSnapshot? doseGrid = null,
        IEnumerable<EsapiDoseStatisticsSnapshot>? doseStatistics = null,
        IEnumerable<EsapiBeamSnapshot>? beams = null,
        string? diseaseSite = null)
    {
        PatientId = EsapiText.Required(patientId, nameof(patientId));
        PatientDisplayName = string.IsNullOrWhiteSpace(patientDisplayName) ? null : patientDisplayName.Trim();
        CourseId = EsapiText.Required(courseId, nameof(courseId));
        PlanId = EsapiText.Required(planId, nameof(planId));
        Prescription = prescription ?? throw new ArgumentNullException(nameof(prescription));
        Structures = structures?.ToArray() ?? throw new ArgumentNullException(nameof(structures));
        DoseGrid = doseGrid;
        DoseStatistics = doseStatistics?.ToArray() ?? Array.Empty<EsapiDoseStatisticsSnapshot>();
        Beams = beams?.ToArray() ?? Array.Empty<EsapiBeamSnapshot>();
        DiseaseSite = string.IsNullOrWhiteSpace(diseaseSite) ? null : diseaseSite.Trim();
    }

    /// <summary>
    /// Patient identifier.
    /// </summary>
    public string PatientId { get; init; }

    /// <summary>
    /// Optional patient display name.
    /// </summary>
    public string? PatientDisplayName { get; init; }

    /// <summary>
    /// Course identifier.
    /// </summary>
    public string CourseId { get; init; }

    /// <summary>
    /// Plan identifier.
    /// </summary>
    public string PlanId { get; init; }

    /// <summary>
    /// Prescription snapshot.
    /// </summary>
    public EsapiPrescriptionSnapshot Prescription { get; init; }

    /// <summary>
    /// Structure snapshots.
    /// </summary>
    public IReadOnlyList<EsapiStructureSnapshot> Structures { get; init; }

    /// <summary>
    /// Optional dose-grid snapshot.
    /// </summary>
    public EsapiDoseGridSnapshot? DoseGrid { get; init; }

    /// <summary>
    /// Optional dose-statistics snapshots.
    /// </summary>
    public IReadOnlyList<EsapiDoseStatisticsSnapshot> DoseStatistics { get; init; }

    /// <summary>
    /// Optional beam snapshots.
    /// </summary>
    public IReadOnlyList<EsapiBeamSnapshot> Beams { get; init; }

    /// <summary>
    /// Optional disease-site label.
    /// </summary>
    public string? DiseaseSite { get; init; }
}
