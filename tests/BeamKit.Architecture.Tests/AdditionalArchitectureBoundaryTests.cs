using BeamKit.Calculations;
using BeamKit.ChangeDetection;
using BeamKit.Check;
using BeamKit.Deliverability;
using BeamKit.Dicom;
using BeamKit.Metrics;
using BeamKit.Naming;
using BeamKit.PlanCheck;
using BeamKit.Protocols;
using BeamKit.Qa;
using BeamKit.Release;
using BeamKit.Reporting;
using BeamKit.Rules;
using BeamKit.Safety;
using BeamKit.Sdk;
using BeamKit.Structures;
using BeamKit.Templates;
using BeamKit.Workflow;
using Xunit;

namespace BeamKit.Architecture.Tests;

public sealed class AdditionalArchitectureBoundaryTests
{
    [Fact]
    public void NonSamplePackagesDoNotReferenceSamples()
    {
        var assemblies = new[]
        {
            typeof(PlanChangeDetector).Assembly,
            typeof(BeamKitCheckEngine).Assembly,
            typeof(DoseCalculationService).Assembly,
            typeof(DeliverabilityCheckService).Assembly,
            typeof(DicomRtDoseImporter).Assembly,
            typeof(PlanQualityMetricService).Assembly,
            typeof(PlanCheckEngine).Assembly,
            typeof(RadiotherapyProtocolValidator).Assembly,
            typeof(StructureNameNormalizer).Assembly,
            typeof(PlanQaPipeline).Assembly,
            typeof(WriteUpManifestBuilder).Assembly,
            typeof(ReportBuilder).Assembly,
            typeof(RuleEngine).Assembly,
            typeof(SafetyEvidenceReviewer).Assembly,
            typeof(BeamKitClient).Assembly,
            typeof(RingStructurePlanner).Assembly,
            typeof(ClinicalGoalTemplateLoader).Assembly,
            typeof(PlanReadinessEvaluator).Assembly
        };

        Assert.All(assemblies, assembly => Assert.DoesNotContain("BeamKit.Samples", ProjectReferenceReader.ProjectReferenceNames(assembly)));
    }

    [Fact]
    public void ChangeDetectionOnlyReferencesCoreWithinBeamKit()
    {
        var beamKitReferences = ProjectReferenceReader.ProjectReferenceNames(typeof(PlanChangeDetector).Assembly)
            .Where(name => name.StartsWith("BeamKit.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "BeamKit.Core" }, beamKitReferences);
    }

    [Fact]
    public void ReportingDoesNotReferenceWorkflowOrAdapters()
    {
        var references = ProjectReferenceReader.ProjectReferenceNames(typeof(ReportBuilder).Assembly);

        Assert.DoesNotContain("BeamKit.Workflow", references);
        Assert.DoesNotContain("BeamKit.Dicom", references);
        Assert.DoesNotContain("BeamKit.Esapi", references);
    }

    [Fact]
    public void ReleaseDoesNotReferenceAdapters()
    {
        var references = ProjectReferenceReader.ProjectReferenceNames(typeof(WriteUpManifestBuilder).Assembly);

        Assert.DoesNotContain("BeamKit.Dicom", references);
        Assert.DoesNotContain("BeamKit.Esapi", references);
    }
}
