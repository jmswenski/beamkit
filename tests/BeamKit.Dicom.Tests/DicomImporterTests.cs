using BeamKit.Dicom;
using BeamKit.Core.Domain;
using FellowOakDicom;
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
}
