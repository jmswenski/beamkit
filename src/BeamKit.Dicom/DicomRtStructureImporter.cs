using BeamKit.Core.Domain;
using FellowOakDicom;

namespace BeamKit.Dicom;

/// <summary>
/// Imports DICOM RTSTRUCT objects into BeamKit structures.
/// </summary>
public sealed class DicomRtStructureImporter
{
    /// <summary>
    /// Imports an RTSTRUCT file.
    /// </summary>
    public DicomRtStructureSet Import(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        return Import(DicomFile.Open(path).Dataset);
    }

    /// <summary>
    /// Imports an RTSTRUCT dataset.
    /// </summary>
    public DicomRtStructureSet Import(DicomDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        RequireModality(dataset, "RTSTRUCT");

        var id = dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, dataset.GetSingleValueOrDefault(DicomTag.StructureSetLabel, "RTSTRUCT"));
        var observations = ReadStructureTypes(dataset);
        var contourPresence = ReadContourPresence(dataset);
        var structures = ReadStructures(dataset, observations, contourPresence);
        return new DicomRtStructureSet(id, structures);
    }

    private static IReadOnlyList<Structure> ReadStructures(
        DicomDataset dataset,
        IReadOnlyDictionary<int, StructureType> observations,
        IReadOnlySet<int> contourPresence)
    {
        if (!dataset.TryGetSequence(DicomTag.StructureSetROISequence, out var roiSequence))
        {
            throw new DicomImportException("RTSTRUCT is missing StructureSetROISequence.");
        }

        return roiSequence.Items
            .Select(item =>
            {
                var roiNumber = item.GetSingleValueOrDefault(DicomTag.ROINumber, 0);
                var name = item.GetSingleValueOrDefault(DicomTag.ROIName, $"ROI_{roiNumber}");
                var type = observations.TryGetValue(roiNumber, out var observedType) ? observedType : StructureType.Unknown;
                return new Structure(
                    roiNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    name,
                    type,
                    0m,
                    contourPresence.Contains(roiNumber));
            })
            .ToArray();
    }

    private static IReadOnlyDictionary<int, StructureType> ReadStructureTypes(DicomDataset dataset)
    {
        var types = new Dictionary<int, StructureType>();
        if (!dataset.TryGetSequence(DicomTag.RTROIObservationsSequence, out var observations))
        {
            return types;
        }

        foreach (var item in observations.Items)
        {
            var roiNumber = item.GetSingleValueOrDefault(DicomTag.ReferencedROINumber, 0);
            var interpretedType = item.GetSingleValueOrDefault(DicomTag.RTROIInterpretedType, string.Empty);
            types[roiNumber] = MapStructureType(interpretedType);
        }

        return types;
    }

    private static IReadOnlySet<int> ReadContourPresence(DicomDataset dataset)
    {
        var roiNumbers = new HashSet<int>();
        if (!dataset.TryGetSequence(DicomTag.ROIContourSequence, out var roiContours))
        {
            return roiNumbers;
        }

        foreach (var item in roiContours.Items)
        {
            var roiNumber = item.GetSingleValueOrDefault(DicomTag.ReferencedROINumber, 0);
            if (roiNumber != 0 && item.Contains(DicomTag.ContourSequence))
            {
                roiNumbers.Add(roiNumber);
            }
        }

        return roiNumbers;
    }

    private static StructureType MapStructureType(string interpretedType)
    {
        return interpretedType.ToUpperInvariant() switch
        {
            "EXTERNAL" => StructureType.External,
            "PTV" => StructureType.Target,
            "CTV" => StructureType.Target,
            "GTV" => StructureType.Target,
            "ORGAN" => StructureType.OrganAtRisk,
            "AVOIDANCE" => StructureType.Avoidance,
            "SUPPORT" => StructureType.Support,
            _ => StructureType.Unknown
        };
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
