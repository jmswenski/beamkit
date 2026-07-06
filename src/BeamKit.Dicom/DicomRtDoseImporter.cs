using System.Buffers.Binary;
using BeamKit.Core.Domain;
using BeamKit.Dvh;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO;

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
        var pixelGrid = ReadPixelGrid(dataset);
        var curves = ReadDvhCurves(dataset);
        var statistics = curves.Select(curve => metricCalculator.ToDoseStatistics(curve, new[] { 95m }, new[] { 20m })).ToArray();
        return new DicomRtDoseImportResult(new Dose(doseId, grid, statistics), curves, pixelGrid);
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

    private static DicomDoseGrid? ReadPixelGrid(DicomDataset dataset)
    {
        if (!dataset.Contains(DicomTag.PixelData)
            || !dataset.TryGetSingleValue<ushort>(DicomTag.Rows, out var rows)
            || !dataset.TryGetSingleValue<ushort>(DicomTag.Columns, out var columns))
        {
            return null;
        }

        if (dataset.InternalTransferSyntax.IsEncapsulated)
        {
            throw new DicomImportException($"Unsupported compressed RTDOSE transfer syntax '{dataset.InternalTransferSyntax.UID.Name}'.");
        }

        if (dataset.InternalTransferSyntax.Endian != Endian.Little)
        {
            throw new DicomImportException($"Unsupported RTDOSE transfer syntax endian '{dataset.InternalTransferSyntax.UID.Name}'.");
        }

        var frames = dataset.TryGetSingleValue<int>(DicomTag.NumberOfFrames, out var numberOfFrames)
            ? Math.Max(1, numberOfFrames)
            : 1;
        var scaling = dataset.TryGetSingleValue<double>(DicomTag.DoseGridScaling, out var doseGridScaling)
            ? (decimal)doseGridScaling
            : 1m;
        var pixelSpacing = dataset.TryGetValues<double>(DicomTag.PixelSpacing, out var spacingValues) && spacingValues.Length >= 2
            ? spacingValues
            : new[] { 1d, 1d };
        var frameOffsets = dataset.TryGetValues<double>(DicomTag.GridFrameOffsetVector, out var offsets)
            ? offsets.Select(offset => (decimal)offset).ToArray()
            : Array.Empty<decimal>();
        var bitsAllocated = dataset.GetSingleValueOrDefault<ushort>(DicomTag.BitsAllocated, 16);
        var pixelRepresentation = dataset.GetSingleValueOrDefault<ushort>(DicomTag.PixelRepresentation, 0);

        if (pixelRepresentation != 0)
        {
            throw new DicomImportException("Signed RTDOSE pixel data is not supported.");
        }

        if (bitsAllocated is not (16 or 32))
        {
            throw new DicomImportException($"Unsupported RTDOSE pixel depth '{bitsAllocated}'.");
        }

        var pixelData = DicomPixelData.Create(dataset);
        var values = new List<decimal>(rows * columns * frames);
        for (var frame = 0; frame < frames; frame++)
        {
            var frameBytes = pixelData.GetFrame(frame).Data;
            ValidateFrameLength(frameBytes.Length, rows, columns, bitsAllocated);
            ReadFrameValues(frameBytes, bitsAllocated, scaling, values);
        }

        return new DicomDoseGrid(
            rows,
            columns,
            frames,
            scaling,
            (decimal)pixelSpacing[0],
            (decimal)pixelSpacing[1],
            frameOffsets,
            values);
    }

    private static void ValidateFrameLength(int frameByteLength, int rows, int columns, ushort bitsAllocated)
    {
        var expectedLength = rows * columns * (bitsAllocated / 8);
        if (frameByteLength != expectedLength)
        {
            throw new DicomImportException($"RTDOSE pixel frame length {frameByteLength} does not match expected length {expectedLength}.");
        }
    }

    private static void ReadFrameValues(byte[] frameBytes, ushort bitsAllocated, decimal scaling, ICollection<decimal> values)
    {
        if (bitsAllocated == 16)
        {
            for (var index = 0; index + sizeof(ushort) <= frameBytes.Length; index += sizeof(ushort))
            {
                values.Add(BinaryPrimitives.ReadUInt16LittleEndian(frameBytes.AsSpan(index, sizeof(ushort))) * scaling);
            }

            return;
        }

        for (var index = 0; index + sizeof(uint) <= frameBytes.Length; index += sizeof(uint))
        {
            values.Add(BinaryPrimitives.ReadUInt32LittleEndian(frameBytes.AsSpan(index, sizeof(uint))) * scaling);
        }
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
