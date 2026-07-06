using BeamKit.Core.Domain;
using BeamKit.Dvh;
using FellowOakDicom;

namespace BeamKit.Dicom;

/// <summary>
/// Imports DICOM RTDOSE objects into BeamKit dose metadata and DVH curves.
/// </summary>
public sealed class DicomRtDoseImporter
{
    private readonly DvhMetricCalculator metricCalculator = new();

    /// <summary>
    /// Imports an RTDOSE file.
    /// </summary>
    public DicomRtDoseImportResult Import(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        return Import(DicomFile.Open(path).Dataset);
    }

    /// <summary>
    /// Imports an RTDOSE dataset.
    /// </summary>
    public DicomRtDoseImportResult Import(DicomDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        RequireModality(dataset, "RTDOSE");

        var doseId = dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, "RTDOSE");
        var grid = ReadDoseGrid(dataset);
        var curves = ReadDvhCurves(dataset);
        var statistics = curves.Select(curve => metricCalculator.ToDoseStatistics(curve, new[] { 95m }, new[] { 20m })).ToArray();
        return new DicomRtDoseImportResult(new Dose(doseId, grid, statistics), curves);
    }

    private static DoseGrid ReadDoseGrid(DicomDataset dataset)
    {
        var pixelSpacing = dataset.TryGetValues<double>(DicomTag.PixelSpacing, out var spacingValues) && spacingValues.Length >= 2
            ? spacingValues
            : new[] { 1d, 1d };
        var zSpacing = dataset.TryGetValues<double>(DicomTag.GridFrameOffsetVector, out var offsets) && offsets.Length >= 2
            ? Math.Abs(offsets[1] - offsets[0])
            : dataset.GetSingleValueOrDefault(DicomTag.SliceThickness, 1d);

        return new DoseGrid((decimal)pixelSpacing[1], (decimal)pixelSpacing[0], (decimal)zSpacing);
    }

    private static IReadOnlyList<DvhCurve> ReadDvhCurves(DicomDataset dataset)
    {
        if (!dataset.TryGetSequence(DicomTag.DVHSequence, out var dvhSequence))
        {
            return Array.Empty<DvhCurve>();
        }

        var curves = new List<DvhCurve>();
        foreach (var item in dvhSequence.Items)
        {
            if (!item.TryGetSequence(DicomTag.DVHReferencedROISequence, out var referencedRois)
                || referencedRois.Items.Count == 0
                || !item.TryGetValues<double>(DicomTag.DVHData, out var dvhData)
                || dvhData.Length < 2)
            {
                continue;
            }

            var roiNumber = referencedRois.Items[0].GetSingleValueOrDefault(DicomTag.ReferencedROINumber, 0);
            var points = new List<DvhPoint>();
            for (var index = 0; index + 1 < dvhData.Length; index += 2)
            {
                points.Add(new DvhPoint((decimal)dvhData[index], ClampVolumePercent((decimal)dvhData[index + 1])));
            }

            if (points.Count > 0)
            {
                curves.Add(new DvhCurve(roiNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), points));
            }
        }

        return curves;
    }

    private static decimal ClampVolumePercent(decimal value)
    {
        return Math.Min(100m, Math.Max(0m, value));
    }

    private static void RequireModality(DicomDataset dataset, string expectedModality)
    {
        var modality = dataset.GetSingleValueOrDefault(DicomTag.Modality, string.Empty);
        if (!string.Equals(modality, expectedModality, StringComparison.OrdinalIgnoreCase))
        {
            throw new DicomImportException($"Expected DICOM modality {expectedModality}, found '{modality}'.");
        }
    }
}
