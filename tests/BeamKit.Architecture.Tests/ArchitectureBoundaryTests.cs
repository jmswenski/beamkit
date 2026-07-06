using System.Reflection;
using BeamKit.Calculations;
using BeamKit.ChangeDetection;
using BeamKit.Core.Domain;
using BeamKit.Deliverability;
using BeamKit.Dicom;
using BeamKit.Esapi;
using BeamKit.Metrics;
using BeamKit.Naming;
using BeamKit.PlanCheck;
using BeamKit.Qa;
using BeamKit.Reporting;
using BeamKit.Rules;
using BeamKit.Structures;
using BeamKit.Templates;
using BeamKit.Workflow;
using Xunit;

namespace BeamKit.Architecture.Tests;

public sealed class ArchitectureBoundaryTests
{
    [Fact]
    public void CoreDoesNotReferenceOtherBeamKitPackages()
    {
        var references = ProjectReferenceReader.ProjectReferenceNames(typeof(Plan).Assembly);

        Assert.DoesNotContain(references, name => name.StartsWith("BeamKit.", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(AdapterAssemblies))]
    public void AdaptersDoNotReferenceBusinessLogicPackages(Assembly assembly)
    {
        var forbidden = new[]
        {
            "BeamKit.ChangeDetection",
            "BeamKit.Deliverability",
            "BeamKit.Metrics",
            "BeamKit.PlanCheck",
            "BeamKit.Qa",
            "BeamKit.Reporting",
            "BeamKit.Rules",
            "BeamKit.Structures",
            "BeamKit.Templates",
            "BeamKit.Workflow"
        };
        var references = ProjectReferenceReader.ProjectReferenceNames(assembly);

        Assert.DoesNotContain(references, name => forbidden.Contains(name, StringComparer.Ordinal));
    }

    [Fact]
    public void EsapiAdapterDoesNotReferenceProprietarySdkAssemblies()
    {
        var references = ProjectReferenceReader.AssemblyReferenceNames(typeof(EsapiPlanConverter).Assembly)
            .Concat(ProjectReferenceReader.DeclaredExternalReferenceNames(typeof(EsapiPlanConverter).Assembly))
            .ToArray();

        Assert.DoesNotContain(references, name =>
            name.Contains("VMS", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Varian", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Eclipse", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [MemberData(nameof(CoreExtensionAssemblies))]
    public void CoreExtensionPackagesDoNotReferenceAdapters(Assembly assembly)
    {
        var forbidden = new[] { "BeamKit.Dicom", "BeamKit.Esapi" };
        var references = ProjectReferenceReader.ProjectReferenceNames(assembly);

        Assert.DoesNotContain(references, name => forbidden.Contains(name, StringComparer.Ordinal));
    }

    public static TheoryData<Assembly> AdapterAssemblies()
    {
        return new TheoryData<Assembly>
        {
            typeof(DicomRtStructureImporter).Assembly,
            typeof(EsapiPlanConverter).Assembly
        };
    }

    public static TheoryData<Assembly> CoreExtensionAssemblies()
    {
        return new TheoryData<Assembly>
        {
            typeof(PlanChangeDetector).Assembly,
            typeof(DoseCalculationService).Assembly,
            typeof(DeliverabilityCheckService).Assembly,
            typeof(PlanQualityMetricService).Assembly,
            typeof(PlanCheckEngine).Assembly,
            typeof(StructureNameNormalizer).Assembly,
            typeof(PlanQaPipeline).Assembly,
            typeof(ReportBuilder).Assembly,
            typeof(RuleEngine).Assembly,
            typeof(RingStructurePlanner).Assembly,
            typeof(ClinicalGoalTemplateLoader).Assembly,
            typeof(PlanReadinessEvaluator).Assembly
        };
    }
}
