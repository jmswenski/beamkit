using BeamKit.Core.Domain;
using BeamKit.Dvh;

namespace BeamKit.Dicom;

/// <summary>
/// BeamKit representation of imported RTDOSE metadata and DVH curves.
/// </summary>
public sealed record DicomRtDoseImportResult
{
    /// <summary>
    /// Creates an RTDOSE import result.
    /// </summary>
    public DicomRtDoseImportResult(Dose dose, IEnumerable<DvhCurve>? dvhCurves = null, DicomDoseGrid? pixelGrid = null)
    {
        Dose = dose ?? throw new ArgumentNullException(nameof(dose));
        DvhCurves = dvhCurves?.ToArray() ?? Array.Empty<DvhCurve>();
        PixelGrid = pixelGrid;
    }

    /// <summary>
    /// Imported dose grid and statistics.
    /// </summary>
    public Dose Dose { get; init; }

    /// <summary>
    /// Imported DVH curves, when present in the RTDOSE object.
    /// </summary>
    public IReadOnlyList<DvhCurve> DvhCurves { get; init; }

    /// <summary>
    /// Imported dose pixel grid, when pixel data is present and uncompressed.
    /// </summary>
    public DicomDoseGrid? PixelGrid { get; init; }
}
