using BeamKit.Core.Domain;
using FellowOakDicom;
using System.Globalization;

namespace BeamKit.Dicom;

/// <summary>
/// Imports DICOM RTPLAN objects into BeamKit prescription and beam metadata.
/// </summary>
public sealed class DicomRtPlanImporter
{
    /// <summary>
    /// Imports an RTPLAN file.
    /// </summary>
    public DicomRtPlanImportResult Import(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        return Import(DicomFile.Open(path).Dataset);
    }

    /// <summary>
    /// Imports an RTPLAN dataset.
    /// </summary>
    public DicomRtPlanImportResult Import(DicomDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        RequireModality(dataset, "RTPLAN");

        var id = dataset.GetSingleValueOrDefault(
            DicomTag.SOPInstanceUID,
            dataset.GetSingleValueOrDefault(DicomTag.RTPlanLabel, "RTPLAN"));
        var planLabel = dataset.GetSingleValueOrDefault<string?>(DicomTag.RTPlanLabel, null);
        var planName = dataset.GetSingleValueOrDefault<string?>(DicomTag.RTPlanName, null);
        var beamMetersets = ReadBeamMetersets(dataset);
        var prescription = ReadPrescription(dataset);
        var beams = ReadBeams(dataset, beamMetersets);

        return new DicomRtPlanImportResult(id, planLabel, planName, prescription, beams);
    }

    private static Prescription ReadPrescription(DicomDataset dataset)
    {
        var fractionCount = 1;
        if (dataset.TryGetSequence(DicomTag.FractionGroupSequence, out var fractionGroups) && fractionGroups.Items.Count > 0)
        {
            fractionCount = Math.Max(1, fractionGroups.Items[0].GetSingleValueOrDefault(DicomTag.NumberOfFractionsPlanned, 1));
        }

        var doseReferences = ReadDoseReferences(dataset);
        var selectedDoseReference = doseReferences
            .Where(doseReference => doseReference.TargetPrescriptionDoseGy.HasValue)
            .OrderByDescending(doseReference => doseReference.TargetPrescriptionDoseGy!.Value)
            .FirstOrDefault()
            ?? doseReferences
                .Where(doseReference => doseReference.FallbackDoseGy.HasValue)
                .OrderByDescending(doseReference => doseReference.FallbackDoseGy!.Value)
                .FirstOrDefault();
        var totalDoseGy = selectedDoseReference?.SelectedDoseGy;

        if (!totalDoseGy.HasValue)
        {
            throw new DicomImportException("RTPLAN is missing a target prescription dose.");
        }

        var targetStructureId = selectedDoseReference?.TargetStructureId
            ?? doseReferences.Select(doseReference => doseReference.TargetStructureId).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? "PTV";

        return new Prescription(totalDoseGy.Value, fractionCount, targetStructureId);
    }

    private static IReadOnlyList<DoseReferenceCandidate> ReadDoseReferences(DicomDataset dataset)
    {
        if (!dataset.TryGetSequence(DicomTag.DoseReferenceSequence, out var doseReferences))
        {
            return Array.Empty<DoseReferenceCandidate>();
        }

        return doseReferences.Items.Select(item => new DoseReferenceCandidate(
            ReadTargetPrescriptionDoseGy(item),
            ReadFallbackDoseGy(item),
            ReadDoseReferenceTarget(item))).ToArray();
    }

    private static decimal? ReadTargetPrescriptionDoseGy(DicomDataset item)
    {
        if (item.TryGetSingleValue<double>(DicomTag.TargetPrescriptionDose, out var targetDose))
        {
            return (decimal)targetDose;
        }

        return null;
    }

    private static decimal? ReadFallbackDoseGy(DicomDataset item)
    {
        if (item.TryGetSingleValue<double>(DicomTag.DeliveryMaximumDose, out var maximumDose))
        {
            return (decimal)maximumDose;
        }

        if (item.TryGetSingleValue<double>(DicomTag.DeliveryWarningDose, out var warningDose))
        {
            return (decimal)warningDose;
        }

        return null;
    }

    private sealed record DoseReferenceCandidate(decimal? TargetPrescriptionDoseGy, decimal? FallbackDoseGy, string? TargetStructureId)
    {
        public decimal? SelectedDoseGy => TargetPrescriptionDoseGy ?? FallbackDoseGy;
    }

    private static string? ReadDoseReferenceTarget(DicomDataset item)
    {
        if (item.TryGetSingleValue<int>(DicomTag.ReferencedROINumber, out var roiNumber) && roiNumber > 0)
        {
            return roiNumber.ToString(CultureInfo.InvariantCulture);
        }

        var description = item.GetSingleValueOrDefault<string?>(DicomTag.DoseReferenceDescription, null);
        return string.IsNullOrWhiteSpace(description) ? null : description;
    }

    private static IReadOnlyDictionary<int, decimal> ReadBeamMetersets(DicomDataset dataset)
    {
        var metersets = new Dictionary<int, decimal>();
        if (!dataset.TryGetSequence(DicomTag.FractionGroupSequence, out var fractionGroups))
        {
            return metersets;
        }

        foreach (var fractionGroup in fractionGroups.Items)
        {
            if (!fractionGroup.TryGetSequence(DicomTag.ReferencedBeamSequence, out var referencedBeams))
            {
                continue;
            }

            foreach (var referencedBeam in referencedBeams.Items)
            {
                var beamNumber = referencedBeam.GetSingleValueOrDefault(DicomTag.ReferencedBeamNumber, 0);
                if (beamNumber <= 0)
                {
                    continue;
                }

                if (referencedBeam.TryGetSingleValue<double>(DicomTag.BeamMeterset, out var meterset))
                {
                    metersets[beamNumber] = (decimal)meterset;
                }
            }
        }

        return metersets;
    }

    private static IReadOnlyList<Beam> ReadBeams(DicomDataset dataset, IReadOnlyDictionary<int, decimal> beamMetersets)
    {
        if (!dataset.TryGetSequence(DicomTag.BeamSequence, out var beamSequence))
        {
            return Array.Empty<Beam>();
        }

        return beamSequence.Items.Select(item =>
        {
            var beamNumber = item.GetSingleValueOrDefault(DicomTag.BeamNumber, 0);
            var beamId = beamNumber > 0
                ? beamNumber.ToString(CultureInfo.InvariantCulture)
                : item.GetSingleValueOrDefault(DicomTag.BeamName, "BEAM");
            var beamName = item.GetSingleValueOrDefault(DicomTag.BeamName, $"Beam {beamId}");
            var radiationType = item.GetSingleValueOrDefault(DicomTag.RadiationType, "UNKNOWN");
            var treatmentMachine = item.GetSingleValueOrDefault(DicomTag.TreatmentMachineName, string.Empty);
            var modality = string.IsNullOrWhiteSpace(treatmentMachine)
                ? radiationType
                : $"{radiationType} {treatmentMachine}";
            var energy = ReadEnergy(item);
            var gantryAngle = ReadGantryAngle(item);
            var controlPoints = ReadControlPoints(item);
            var techniqueId = ReadTechniqueId(item, controlPoints);
            decimal? monitorUnits = beamNumber > 0 && beamMetersets.TryGetValue(beamNumber, out var meterset)
                ? meterset
                : null;

            return new Beam(
                beamId,
                beamName,
                modality,
                energy,
                gantryAngle,
                monitorUnits,
                treatmentMachine,
                techniqueId,
                isSetupField: false,
                controlPoints);
        }).ToArray();
    }

    private static string ReadEnergy(DicomDataset item)
    {
        if (item.TryGetSingleValue<double>(DicomTag.NominalBeamEnergy, out var energy))
        {
            return string.Create(CultureInfo.InvariantCulture, $"{energy:g} MV");
        }

        return item.GetSingleValueOrDefault(DicomTag.RadiationType, "UNKNOWN");
    }

    private static decimal? ReadGantryAngle(DicomDataset beamItem)
    {
        if (!beamItem.TryGetSequence(DicomTag.ControlPointSequence, out var controlPoints) || controlPoints.Items.Count == 0)
        {
            return null;
        }

        return controlPoints.Items[0].TryGetSingleValue<double>(DicomTag.GantryAngle, out var gantryAngle)
            ? (decimal)gantryAngle
            : null;
    }

    private static IReadOnlyList<BeamControlPoint> ReadControlPoints(DicomDataset beamItem)
    {
        if (!beamItem.TryGetSequence(DicomTag.ControlPointSequence, out var controlPoints) || controlPoints.Items.Count == 0)
        {
            return Array.Empty<BeamControlPoint>();
        }

        return controlPoints.Items.Select((item, index) => new BeamControlPoint(
            item.GetSingleValueOrDefault(DicomTag.ControlPointIndex, index),
            ReadDecimal(item, DicomTag.GantryAngle),
            ReadDecimal(item, DicomTag.CumulativeMetersetWeight),
            ReadJawPositions(item))).ToArray();
    }

    private static BeamJawPositions? ReadJawPositions(DicomDataset controlPoint)
    {
        if (!controlPoint.TryGetSequence(DicomTag.BeamLimitingDevicePositionSequence, out var positions))
        {
            return null;
        }

        decimal? x1 = null;
        decimal? x2 = null;
        decimal? y1 = null;
        decimal? y2 = null;

        foreach (var item in positions.Items)
        {
            var deviceType = item.GetSingleValueOrDefault(DicomTag.RTBeamLimitingDeviceType, string.Empty);
            if (!item.TryGetValues<double>(DicomTag.LeafJawPositions, out var jawPositions) || jawPositions.Length < 2)
            {
                continue;
            }

            if (string.Equals(deviceType, "ASYMX", StringComparison.OrdinalIgnoreCase)
                || string.Equals(deviceType, "X", StringComparison.OrdinalIgnoreCase))
            {
                x1 = (decimal)jawPositions[0] / 10m;
                x2 = (decimal)jawPositions[1] / 10m;
            }
            else if (string.Equals(deviceType, "ASYMY", StringComparison.OrdinalIgnoreCase)
                || string.Equals(deviceType, "Y", StringComparison.OrdinalIgnoreCase))
            {
                y1 = (decimal)jawPositions[0] / 10m;
                y2 = (decimal)jawPositions[1] / 10m;
            }
        }

        return x1.HasValue && x2.HasValue && y1.HasValue && y2.HasValue
            ? new BeamJawPositions(x1.Value, x2.Value, y1.Value, y2.Value)
            : null;
    }

    private static decimal? ReadDecimal(DicomDataset item, DicomTag tag)
    {
        return item.TryGetSingleValue<double>(tag, out var value) ? (decimal)value : null;
    }

    private static string ReadTechniqueId(DicomDataset beamItem, IReadOnlyList<BeamControlPoint> controlPoints)
    {
        var deliveryType = beamItem.GetSingleValueOrDefault(DicomTag.TreatmentDeliveryType, string.Empty);
        if (!string.IsNullOrWhiteSpace(deliveryType))
        {
            return deliveryType;
        }

        var gantryAngles = controlPoints
            .Select(controlPoint => controlPoint.GantryAngleDegrees)
            .Where(angle => angle.HasValue)
            .Select(angle => angle!.Value)
            .Distinct()
            .Take(2)
            .ToArray();
        return gantryAngles.Length > 1 ? "ARC" : "STATIC";
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
