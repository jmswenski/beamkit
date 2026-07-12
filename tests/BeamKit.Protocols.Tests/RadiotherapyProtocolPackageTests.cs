using BeamKit.Check;
using BeamKit.Core.Domain;
using BeamKit.Protocols;
using BeamKit.RulePacks;
using Xunit;

namespace BeamKit.Protocols.Tests;

public sealed class RadiotherapyProtocolPackageTests
{
    [Fact]
    public void SampleProtocolLoadsAndValidates()
    {
        var package = RadiotherapyProtocolPackageStore.FromPath(SampleProtocolPath());
        var report = new RadiotherapyProtocolValidator().Validate(package);

        Assert.Equal("synthetic-lung-sbrt-protocol", package.Id);
        Assert.Equal(ProtocolPackageStatus.Approved, package.Status);
        Assert.True(report.IsValid);
        Assert.Empty(report.Issues);
    }

    [Fact]
    public void JsonRoundTripPreservesEnumsAndClinicalIntent()
    {
        var package = RadiotherapyProtocolPackageStore.FromPath(SampleProtocolPath());
        var roundTripped = RadiotherapyProtocolPackageStore.FromJson(RadiotherapyProtocolPackageStore.ToJson(package));

        Assert.Equal(package.Status, roundTripped.Status);
        Assert.Equal(package.Structures[1].Role, roundTripped.Structures[1].Role);
        Assert.Equal(package.Constraints[0].Comparison, roundTripped.Constraints[0].Comparison);
        Assert.Equal(package.Prescriptions[0].ComputedDosePerFractionGy, roundTripped.Prescriptions[0].ComputedDosePerFractionGy);
    }

    [Fact]
    public void ValidatorRejectsMissingPrescriptionTarget()
    {
        var package = new RadiotherapyProtocolPackage(
            "bad-protocol",
            "Bad Protocol",
            "1",
            "Lung",
            "Definitive",
            ProtocolPackageStatus.Draft,
            new ProtocolSourceDocument("Synthetic", hash: "sha256:abc"),
            structures: new[]
            {
                new ProtocolStructureRequirement("body", "BODY", ProtocolStructureRole.External)
            },
            prescriptions: new[]
            {
                new ProtocolPrescription("primary", "PTV_5000", 50m, 5)
            });

        var report = new RadiotherapyProtocolValidator().Validate(package);

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, issue => issue.Code == "rtpx.prescription.target-missing");
    }

    [Fact]
    public void ValidatorRejectsUnsupportedSchemaVersion()
    {
        var package = RadiotherapyProtocolPackageStore.FromPath(SampleProtocolPath()) with
        {
            SchemaVersion = "9.9"
        };

        var report = new RadiotherapyProtocolValidator().Validate(package);

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, issue => issue.Code == "rtpx.schema-version.unsupported");
    }

    [Fact]
    public void CompilerCreatesLoadableRulePackScaffold()
    {
        using var directory = TemporaryDirectory.Create();
        var compilation = new RadiotherapyProtocolCompiler().CompilePath(SampleProtocolPath());

        compilation.Scaffold.WriteToDirectory(directory.Path);

        var manifestPath = Path.Combine(directory.Path, compilation.ManifestPath);
        var manifest = RulePackManifestStore.FromFile(manifestPath);
        var rulePack = BeamKitRulePackLoader.FromFile(manifestPath);
        var validation = new RulePackPolicyValidator().Validate(rulePack);

        Assert.Equal("clinical-rules.json", manifest.ClinicalRuleCatalog);
        Assert.Equal("plan-checks.json", manifest.PlanCheckCatalog);
        Assert.Equal("Lung", rulePack.DiseaseSite);
        Assert.Contains(rulePack.PlanCheckCatalog.Checks, check => check.Id == "dose.grid.spacing");
        Assert.Contains(rulePack.ClinicalRuleSet.Rules, rule => rule.Id == "cord.max");
        Assert.True(validation.IsValid);
    }

    [Fact]
    public void CompilerRejectsInvalidProtocol()
    {
        var package = new RadiotherapyProtocolPackage(
            "invalid-protocol",
            "Invalid Protocol",
            "1",
            "Lung",
            "Definitive",
            structures: new[]
            {
                new ProtocolStructureRequirement("body", "BODY", ProtocolStructureRole.External)
            },
            prescriptions: new[]
            {
                new ProtocolPrescription("primary", "PTV_5000", 50m, 5)
            },
            constraints: new[]
            {
                new ProtocolDoseConstraint("cord.max", "Cord", "Max", GoalComparison.LessThanOrEqual, 18m, "Gy")
            });

        var exception = Assert.Throws<InvalidOperationException>(() => new RadiotherapyProtocolCompiler().Compile(package));

        Assert.Contains("validation failed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string SampleProtocolPath()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "samples",
            "rtpx",
            "lung-sbrt-v1"));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryDirectory Create()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beamkit-protocols-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TemporaryDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
