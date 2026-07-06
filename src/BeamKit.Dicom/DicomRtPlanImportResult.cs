using BeamKit.Core.Domain;

namespace BeamKit.Dicom;

/// <summary>
/// BeamKit representation of imported RTPLAN prescription and beam metadata.
/// </summary>
public sealed record DicomRtPlanImportResult
{
    /// <summary>
    /// Creates an RTPLAN import result.
    /// </summary>
    public DicomRtPlanImportResult(
        string id,
        string? planLabel,
        string? planName,
        Prescription prescription,
        IEnumerable<Beam>? beams = null)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Plan id is required.", nameof(id)) : id.Trim();
        PlanLabel = string.IsNullOrWhiteSpace(planLabel) ? null : planLabel.Trim();
        PlanName = string.IsNullOrWhiteSpace(planName) ? null : planName.Trim();
        Prescription = prescription ?? throw new ArgumentNullException(nameof(prescription));
        Beams = beams?.ToArray() ?? Array.Empty<Beam>();
    }

    /// <summary>
    /// Imported RTPLAN identifier.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// DICOM RT Plan Label, when present.
    /// </summary>
    public string? PlanLabel { get; init; }

    /// <summary>
    /// DICOM RT Plan Name, when present.
    /// </summary>
    public string? PlanName { get; init; }

    /// <summary>
    /// Imported prescription summary.
    /// </summary>
    public Prescription Prescription { get; init; }

    /// <summary>
    /// Imported treatment beams.
    /// </summary>
    public IReadOnlyList<Beam> Beams { get; init; }
}
