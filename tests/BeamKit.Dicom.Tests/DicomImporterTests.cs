using BeamKit.Dicom;
using BeamKit.Core.Domain;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using Xunit;

namespace BeamKit.Dicom.Tests;

public sealed class DicomImporterTests
{
    [Fact]
    public void ImportsRtStructStructures()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.Modality, "RTSTRUCT" },
            { DicomTag.SOPInstanceUID, "1.2.3" },
            {
                DicomTag.StructureSetROISequence,
                new DicomDataset
                {
                    { DicomTag.ROINumber, 1 },
                    { DicomTag.ROIName, "PTV_7000" }
                },
                new DicomDataset
                {
                    { DicomTag.ROINumber, 2 },
                    { DicomTag.ROIName, "SpinalCord" }
                }
            },
            {
                DicomTag.RTROIObservationsSequence,
                new DicomDataset
                {
                    { DicomTag.ReferencedROINumber, 1 },
                    { DicomTag.RTROIInterpretedType, "PTV" }
                },
                new DicomDataset
                {
                    { DicomTag.ReferencedROINumber, 2 },
                    { DicomTag.RTROIInterpretedType, "ORGAN" }
                }
            }
        };

        var structureSet = new DicomRtStructureImporter().Import(dataset);

        Assert.Equal("1.2.3", structureSet.Id);
        Assert.Equal(2, structureSet.Structures.Count);
        Assert.Equal(StructureType.Target, structureSet.Structures[0].Type);
        Assert.Equal(StructureType.OrganAtRisk, structureSet.Structures[1].Type);
    }

    [Fact]
    public void ImportsRtDoseGridAndDvhMetrics()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.Modality, "RTDOSE" },
            { DicomTag.SOPInstanceUID, "4.5.6" },
            { DicomTag.PixelSpacing, new[] { 2.5d, 2.5d } },
            { DicomTag.GridFrameOffsetVector, new[] { 0d, 2.5d } },
            {
                DicomTag.DVHSequence,
                new DicomDataset
                {
                    {
                        DicomTag.DVHReferencedROISequence,
                        new DicomDataset
                        {
                            { DicomTag.ReferencedROINumber, 1 }
                        }
                    },
                    { DicomTag.DVHData, new[] { 0d, 100d, 50d, 50d, 100d, 0d } }
                }
            }
        };

        var result = new DicomRtDoseImporter().Import(dataset);

        Assert.Equal("4.5.6", result.Dose.Id);
        Assert.Equal(2.5m, result.Dose.Grid.MaxSpacingMm);
        Assert.Single(result.DvhCurves);
        Assert.Equal(5m, result.Dose.Statistics.Single().GetMetric(DoseMetricKeys.DoseAtVolumePercent(95m)));
    }

    [Fact]
    public void ImportsRtPlanPrescriptionAndBeams()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.Modality, "RTPLAN" },
            { DicomTag.SOPInstanceUID, "1.2.840.1" },
            { DicomTag.RTPlanLabel, "HN_70Gy" },
            {
                DicomTag.DoseReferenceSequence,
                new DicomDataset
                {
                    { DicomTag.ReferencedROINumber, 7 },
                    { DicomTag.TargetPrescriptionDose, 70d }
                }
            },
            {
                DicomTag.FractionGroupSequence,
                new DicomDataset
                {
                    { DicomTag.NumberOfFractionsPlanned, 35 },
                    {
                        DicomTag.ReferencedBeamSequence,
                        new DicomDataset
                        {
                            { DicomTag.ReferencedBeamNumber, 1 },
                            { DicomTag.BeamMeterset, 412.5d }
                        }
                    }
                }
            },
            {
                DicomTag.BeamSequence,
                new DicomDataset
                {
                    { DicomTag.BeamNumber, 1 },
                    { DicomTag.BeamName, "Arc 1" },
                    { DicomTag.RadiationType, "PHOTON" },
                    { DicomTag.TreatmentMachineName, "LINAC-A" },
                    { DicomTag.NominalBeamEnergy, 6d },
                    {
                        DicomTag.ControlPointSequence,
                        new DicomDataset
                        {
                            { DicomTag.GantryAngle, 181d }
                        }
                    }
                }
            }
        };

        var result = new DicomRtPlanImporter().Import(dataset);

        Assert.Equal("1.2.840.1", result.Id);
        Assert.Equal("HN_70Gy", result.PlanLabel);
        Assert.Equal(70m, result.Prescription.TotalDoseGy);
        Assert.Equal(35, result.Prescription.FractionCount);
        Assert.Equal("7", result.Prescription.TargetStructureId);
        var beam = Assert.Single(result.Beams);
        Assert.Equal("Arc 1", beam.Name);
        Assert.Equal(412.5m, beam.MonitorUnits);
        Assert.Equal(181m, beam.GantryAngleDegrees);
    }

    [Fact]
    public void RtPlanScansMultipleDoseReferencesForPrescriptionDose()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.Modality, "RTPLAN" },
            { DicomTag.SOPInstanceUID, "1.2.840.2" },
            {
                DicomTag.DoseReferenceSequence,
                new DicomDataset
                {
                    { DicomTag.ReferencedROINumber, 7 }
                },
                new DicomDataset
                {
                    { DicomTag.TargetPrescriptionDose, 70d }
                }
            },
            {
                DicomTag.FractionGroupSequence,
                new DicomDataset
                {
                    { DicomTag.NumberOfFractionsPlanned, 35 }
                }
            }
        };

        var result = new DicomRtPlanImporter().Import(dataset);

        Assert.Equal(70m, result.Prescription.TotalDoseGy);
        Assert.Equal("7", result.Prescription.TargetStructureId);
    }

    [Fact]
    public void RtPlanKeepsPrescriptionDoseAndTargetCoupled()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.Modality, "RTPLAN" },
            { DicomTag.SOPInstanceUID, "1.2.840.3" },
            {
                DicomTag.DoseReferenceSequence,
                new DicomDataset
                {
                    { DicomTag.ReferencedROINumber, 7 },
                    { DicomTag.TargetPrescriptionDose, 70d }
                },
                new DicomDataset
                {
                    { DicomTag.ReferencedROINumber, 3 },
                    { DicomTag.TargetPrescriptionDose, 56d }
                },
                new DicomDataset
                {
                    { DicomTag.ReferencedROINumber, 12 },
                    { DicomTag.DeliveryMaximumDose, 75d }
                }
            }
        };

        var result = new DicomRtPlanImporter().Import(dataset);

        Assert.Equal(70m, result.Prescription.TotalDoseGy);
        Assert.Equal("7", result.Prescription.TargetStructureId);
    }

    [Fact]
    public void ImportsRtDosePixelGrid()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.Modality, "RTDOSE" },
            { DicomTag.SOPInstanceUID, "4.5.7" },
            { DicomTag.Rows, (ushort)2 },
            { DicomTag.Columns, (ushort)2 },
            { DicomTag.NumberOfFrames, "1" },
            { DicomTag.PixelSpacing, new[] { 2d, 3d } },
            { DicomTag.DoseGridScaling, 0.01d },
            { DicomTag.SamplesPerPixel, (ushort)1 },
            { DicomTag.PhotometricInterpretation, "MONOCHROME2" },
            { DicomTag.BitsAllocated, (ushort)16 },
            { DicomTag.BitsStored, (ushort)16 },
            { DicomTag.HighBit, (ushort)15 },
            { DicomTag.PixelRepresentation, (ushort)0 }
        };
        var pixelData = DicomPixelData.Create(dataset, true);
        pixelData.AddFrame(new MemoryByteBuffer(new byte[] { 100, 0, 200, 0, 44, 1, 144, 1 }));

        var result = new DicomRtDoseImporter().Import(dataset);

        Assert.NotNull(result.PixelGrid);
        Assert.Equal(2, result.PixelGrid.Rows);
        Assert.Equal(3m, result.PixelGrid.ColumnSpacingMm);
        Assert.Equal(1m, result.PixelGrid.GetDoseGy(0, 0, 0));
        Assert.Equal(4m, result.PixelGrid.GetDoseGy(0, 1, 1));
    }

    [Fact]
    public void RtDoseSignedPixelGridThrowsImportException()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.Modality, "RTDOSE" },
            { DicomTag.SOPInstanceUID, "4.5.8" },
            { DicomTag.Rows, (ushort)2 },
            { DicomTag.Columns, (ushort)2 },
            { DicomTag.NumberOfFrames, "1" },
            { DicomTag.DoseGridScaling, 0.01d },
            { DicomTag.SamplesPerPixel, (ushort)1 },
            { DicomTag.PhotometricInterpretation, "MONOCHROME2" },
            { DicomTag.BitsAllocated, (ushort)16 },
            { DicomTag.BitsStored, (ushort)16 },
            { DicomTag.HighBit, (ushort)15 },
            { DicomTag.PixelRepresentation, (ushort)1 }
        };
        var pixelData = DicomPixelData.Create(dataset, true);
        pixelData.AddFrame(new MemoryByteBuffer(new byte[] { 100, 0, 200, 0, 44, 1, 144, 1 }));

        var exception = Assert.Throws<DicomImportException>(() => new DicomRtDoseImporter().Import(dataset));

        Assert.Contains("signed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
