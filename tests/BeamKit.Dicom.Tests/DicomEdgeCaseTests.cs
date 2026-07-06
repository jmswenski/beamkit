using BeamKit.Core.Domain;
using BeamKit.Dicom;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using Xunit;

namespace BeamKit.Dicom.Tests;

public sealed class DicomEdgeCaseTests
{
    [Theory]
    [InlineData("RTSTRUCT")]
    [InlineData("RTDOSE")]
    [InlineData("RTPLAN")]
    public void ImportersRejectUnexpectedModality(string importer)
    {
        var dataset = new DicomDataset { { DicomTag.Modality, "CT" } };

        Assert.Throws<DicomImportException>(() =>
        {
            switch (importer)
            {
                case "RTSTRUCT":
                    new DicomRtStructureImporter().Import(dataset);
                    break;
                case "RTDOSE":
                    new DicomRtDoseImporter().Import(dataset);
                    break;
                case "RTPLAN":
                    new DicomRtPlanImporter().Import(dataset);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        });
    }

    [Fact]
    public void RtStructRequiresStructureSetRoiSequence()
    {
        var dataset = new DicomDataset { { DicomTag.Modality, "RTSTRUCT" } };

        var exception = Assert.Throws<DicomImportException>(() => new DicomRtStructureImporter().Import(dataset));

        Assert.Contains("StructureSetROISequence", exception.Message);
    }

    [Fact]
    public void RtStructMarksMissingContourSequenceAsNoContours()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.Modality, "RTSTRUCT" },
            {
                DicomTag.StructureSetROISequence,
                new DicomDataset
                {
                    { DicomTag.ROINumber, 1 },
                    { DicomTag.ROIName, "PTV" }
                }
            }
        };

        var structureSet = new DicomRtStructureImporter().Import(dataset);

        Assert.False(structureSet.Structures.Single().HasContours);
    }

    [Fact]
    public void RtDoseRejectsUnsupportedPixelDepth()
    {
        var dataset = CreatePixelDoseDataset(bitsAllocated: 8, pixelRepresentation: 0);
        var pixelData = DicomPixelData.Create(dataset, true);
        pixelData.AddFrame(new MemoryByteBuffer(new byte[] { 1, 2, 3, 4 }));

        var exception = Assert.Throws<DicomImportException>(() => new DicomRtDoseImporter().Import(dataset));

        Assert.Contains("Unsupported RTDOSE pixel depth", exception.Message);
    }

    [Fact]
    public void RtDoseReadsThirtyTwoBitPixelGrid()
    {
        var dataset = CreatePixelDoseDataset(bitsAllocated: 32, pixelRepresentation: 0);
        var pixelData = DicomPixelData.Create(dataset, true);
        pixelData.AddFrame(new MemoryByteBuffer(new byte[] { 232, 3, 0, 0, 208, 7, 0, 0, 184, 11, 0, 0, 160, 15, 0, 0 }));

        var result = new DicomRtDoseImporter().Import(dataset);

        Assert.NotNull(result.PixelGrid);
        Assert.Equal(10m, result.PixelGrid.GetDoseGy(0, 0, 0));
        Assert.Equal(40m, result.PixelGrid.GetDoseGy(0, 1, 1));
    }

    [Fact]
    public void RtDoseWithoutPixelDataLeavesPixelGridNull()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.Modality, "RTDOSE" },
            { DicomTag.SOPInstanceUID, "1.2.840.100" },
            { DicomTag.PixelSpacing, new[] { 2d, 2d } },
            { DicomTag.SliceThickness, 3d }
        };

        var result = new DicomRtDoseImporter().Import(dataset);

        Assert.Null(result.PixelGrid);
        Assert.Equal(3m, result.Dose.Grid.SpacingZMm);
    }

    [Fact]
    public void RtDoseWithoutDvhSequenceHasNoDvhCurvesOrStatistics()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.Modality, "RTDOSE" },
            { DicomTag.SOPInstanceUID, "1.2.840.101" }
        };

        var result = new DicomRtDoseImporter().Import(dataset);

        Assert.Empty(result.DvhCurves);
        Assert.Empty(result.Dose.Statistics);
    }

    [Fact]
    public void RtPlanRequiresPrescriptionDose()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.Modality, "RTPLAN" },
            { DicomTag.SOPInstanceUID, "1.2.840.200" },
            {
                DicomTag.DoseReferenceSequence,
                new DicomDataset
                {
                    { DicomTag.ReferencedROINumber, 1 }
                }
            }
        };

        var exception = Assert.Throws<DicomImportException>(() => new DicomRtPlanImporter().Import(dataset));

        Assert.Contains("target prescription dose", exception.Message);
    }

    [Fact]
    public void RtPlanUsesFallbackDeliveryDoseWhenTargetPrescriptionDoseIsAbsent()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.Modality, "RTPLAN" },
            { DicomTag.SOPInstanceUID, "1.2.840.201" },
            {
                DicomTag.DoseReferenceSequence,
                new DicomDataset
                {
                    { DicomTag.ReferencedROINumber, 1 },
                    { DicomTag.DeliveryMaximumDose, 45d }
                }
            }
        };

        var result = new DicomRtPlanImporter().Import(dataset);

        Assert.Equal(45m, result.Prescription.TotalDoseGy);
        Assert.Equal("1", result.Prescription.TargetStructureId);
    }

    [Fact]
    public void DicomDoseGridValidatesIndexBounds()
    {
        var grid = new DicomDoseGrid(1, 2, 1, 0.01m, 2m, 2m, null, new[] { 1m, 2m });

        Assert.Equal(2m, grid.GetDoseGy(0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetDoseGy(1, 0, 0));
    }

    private static DicomDataset CreatePixelDoseDataset(ushort bitsAllocated, ushort pixelRepresentation)
    {
        return new DicomDataset
        {
            { DicomTag.Modality, "RTDOSE" },
            { DicomTag.SOPInstanceUID, "1.2.840.102" },
            { DicomTag.Rows, (ushort)2 },
            { DicomTag.Columns, (ushort)2 },
            { DicomTag.NumberOfFrames, "1" },
            { DicomTag.DoseGridScaling, 0.01d },
            { DicomTag.SamplesPerPixel, (ushort)1 },
            { DicomTag.PhotometricInterpretation, "MONOCHROME2" },
            { DicomTag.BitsAllocated, bitsAllocated },
            { DicomTag.BitsStored, bitsAllocated },
            { DicomTag.HighBit, (ushort)(bitsAllocated - 1) },
            { DicomTag.PixelRepresentation, pixelRepresentation }
        };
    }
}
