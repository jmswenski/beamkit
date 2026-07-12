using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeamKit.Core.Domain;
using BeamKit.Esapi;
using BeamKit.Protocols;
using BeamKit.Protocols.Acceptance;
using BeamKit.Protocols.Word;
using Xunit;

namespace BeamKit.Protocols.Acceptance.Tests;

public sealed class RtpxPackageAcceptanceEngineTests
{
    [Fact]
    public void AcceptPackageWritesLocalArtifactsAndRulePack()
    {
        var packagePath = CreateRtpxPackage();
        var outputDirectory = TempDirectory();
        var profile = CreateProfile();

        var report = new RtpxPackageAcceptanceEngine().Accept(new RtpxAcceptanceRequest(
            packagePath,
            profile,
            outputDirectory,
            AcceptedAtUtc: new DateTimeOffset(2026, 7, 12, 12, 0, 0, TimeSpan.Zero)));

        Assert.True(report.IsAccepted, Describe(report));
        Assert.Equal("Synthetic Hospital", report.Institution);
        Assert.Contains(report.StructureMappings, mapping => mapping.ProtocolName == "PTV_5000" && mapping.LocalName == "PTV_Hospital");
        Assert.True(File.Exists(Path.Combine(outputDirectory, "local-rtpx.json")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "acceptance-report.json")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "acceptance-report.md")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "rule-pack", "beamkit-rule-pack.json")));
        Assert.Equal("PTV_Hospital", report.LocalPackage.Prescriptions.Single().Target);
    }

    [Fact]
    public void MissingRequiredMappingBlocksRulePackOutput()
    {
        var packagePath = CreateRtpxPackage();
        var outputDirectory = TempDirectory();
        var profile = new RtpxInstitutionProfile(
            "Synthetic Hospital",
            new[] { new RtpxStructureMapping("PTV_5000", "PTV_Hospital") },
            acceptedBy: "Physics",
            effectiveDate: new DateOnly(2026, 7, 12));

        var report = new RtpxPackageAcceptanceEngine().Accept(new RtpxAcceptanceRequest(packagePath, profile, outputDirectory));

        Assert.False(report.IsAccepted);
        Assert.Contains(report.Issues, issue => issue.Code == "rtpx.acceptance.structure.mapping-missing" && issue.Severity == RtpxAcceptanceIssueSeverity.Error);
        Assert.True(File.Exists(Path.Combine(outputDirectory, "acceptance-report.md")));
        Assert.False(File.Exists(Path.Combine(outputDirectory, "rule-pack", "beamkit-rule-pack.json")));
    }

    [Fact]
    public void EsapiSnapshotEvidencePassesWhenMappedPlanMatchesProtocol()
    {
        var packagePath = CreateRtpxPackage();
        var outputDirectory = TempDirectory();
        var profile = CreateProfile();
        var snapshot = CreateMatchingSnapshot();

        var report = new RtpxPackageAcceptanceEngine().Accept(new RtpxAcceptanceRequest(
            packagePath,
            profile,
            outputDirectory,
            snapshot,
            "snapshot.json"));

        Assert.True(report.IsAccepted, Describe(report));
        Assert.NotNull(report.EsapiEvidence);
        Assert.All(report.EsapiEvidence.StructureChecks, check => Assert.Equal("Pass", check.Status));
        Assert.Equal("Pass", report.EsapiEvidence.PrescriptionChecks.Single().Status);
    }

    [Fact]
    public void EsapiSnapshotEvidenceAcceptsInstitutionLocalStructureAlias()
    {
        var packagePath = CreateRtpxPackage();
        var outputDirectory = TempDirectory();
        var profile = new RtpxInstitutionProfile(
            "Synthetic Hospital",
            new[]
            {
                new RtpxStructureMapping("PTV_5000", "PTV_Hospital", aliases: new[] { "PTV_Alias" }),
                new RtpxStructureMapping("Cord", "SpinalCord")
            },
            acceptedBy: "Physics Director",
            effectiveDate: new DateOnly(2026, 7, 12));
        var snapshot = CreateMatchingSnapshot(targetStructureId: "PTV_Alias");

        var report = new RtpxPackageAcceptanceEngine().Accept(new RtpxAcceptanceRequest(
            packagePath,
            profile,
            outputDirectory,
            snapshot,
            "snapshot.json"));

        Assert.True(report.IsAccepted, Describe(report));
        Assert.Equal("Pass", report.EsapiEvidence?.PrescriptionChecks.Single().Status);
        Assert.All(report.EsapiEvidence!.StructureChecks, check => Assert.Equal("Pass", check.Status));
    }

    [Fact]
    public void EsapiSnapshotMismatchBlocksAcceptance()
    {
        var packagePath = CreateRtpxPackage();
        var outputDirectory = TempDirectory();
        var profile = CreateProfile();
        var snapshot = CreateMatchingSnapshot(totalDoseGy: 45m);

        var report = new RtpxPackageAcceptanceEngine().Accept(new RtpxAcceptanceRequest(
            packagePath,
            profile,
            outputDirectory,
            snapshot,
            "snapshot.json"));

        Assert.False(report.IsAccepted);
        Assert.Contains(report.Issues, issue => issue.Code == "rtpx.acceptance.esapi.prescription-mismatch");
        Assert.Equal(ProtocolPackageStatus.InReview, report.LocalPackage.Status);
        Assert.Contains("\"status\": \"InReview\"", File.ReadAllText(Path.Combine(outputDirectory, "local-rtpx.json")), StringComparison.Ordinal);
    }

    [Fact]
    public void EsapiSnapshotEvidenceEvaluatesMatchingTargetPrescriptionOnly()
    {
        var packagePath = CreateRtpxPackage(CreateMultiPrescriptionPackage());
        var outputDirectory = TempDirectory();
        var profile = new RtpxInstitutionProfile(
            "Synthetic Hospital",
            new[]
            {
                new RtpxStructureMapping("PTV_5000", "PTV_Hospital"),
                new RtpxStructureMapping("Boost", "Boost_Hospital"),
                new RtpxStructureMapping("Cord", "SpinalCord")
            },
            acceptedBy: "Physics Director",
            effectiveDate: new DateOnly(2026, 7, 12));
        var snapshot = CreateMatchingSnapshot(extraStructures: new[]
        {
            new EsapiStructureSnapshot("Boost_Hospital", "Boost_Hospital", StructureType.Target, 25m, HasContours: true)
        });

        var report = new RtpxPackageAcceptanceEngine().Accept(new RtpxAcceptanceRequest(
            packagePath,
            profile,
            outputDirectory,
            snapshot,
            "snapshot.json"));

        Assert.True(report.IsAccepted, Describe(report));
        var prescriptionCheck = Assert.Single(report.EsapiEvidence!.PrescriptionChecks);
        Assert.Equal("rx.primary", prescriptionCheck.ProtocolPrescriptionId);
        Assert.Contains(report.Issues, issue =>
            issue.Code == "rtpx.acceptance.esapi.prescription-not-evaluated"
            && issue.Subject == "rx.boost"
            && issue.Severity == RtpxAcceptanceIssueSeverity.Info);
        Assert.DoesNotContain(report.Issues, issue => issue.Code == "rtpx.acceptance.esapi.prescription-mismatch");
    }

    [Fact]
    public void DuplicateInstitutionMappingsProduceAcceptanceIssue()
    {
        var packagePath = CreateRtpxPackage();
        var outputDirectory = TempDirectory();
        var profile = new RtpxInstitutionProfile(
            "Synthetic Hospital",
            new[]
            {
                new RtpxStructureMapping("PTV_5000", "PTV_A"),
                new RtpxStructureMapping("PTV_5000", "PTV_B"),
                new RtpxStructureMapping("Cord", "SpinalCord")
            },
            acceptedBy: "Physics Director",
            effectiveDate: new DateOnly(2026, 7, 12));

        var report = new RtpxPackageAcceptanceEngine().Accept(new RtpxAcceptanceRequest(packagePath, profile, outputDirectory));

        Assert.False(report.IsAccepted);
        Assert.Contains(report.Issues, issue => issue.Code == "rtpx.acceptance.structure.mapping-duplicate");
    }

    [Fact]
    public void AcceptanceMapsOnlyStructurePlanCheckParameters()
    {
        var packagePath = CreateRtpxPackage(CreatePackageWithPlanCheckParameters());
        var outputDirectory = TempDirectory();

        var report = new RtpxPackageAcceptanceEngine().Accept(new RtpxAcceptanceRequest(
            packagePath,
            CreateProfile(),
            outputDirectory));

        Assert.True(report.IsAccepted, Describe(report));
        var parameters = report.LocalPackage.PlanChecks.Single().Parameters;
        Assert.Equal("SpinalCord", parameters["structure"]);
        Assert.Equal("PTV_5000", parameters["technique"]);
    }

    [Fact]
    public void ExistingAcceptanceOutputBlocksBeforePartialWrites()
    {
        var packagePath = CreateRtpxPackage();
        var outputDirectory = TempDirectory();
        File.WriteAllText(Path.Combine(outputDirectory, "acceptance-report.json"), "existing");

        var exception = Assert.Throws<IOException>(() => new RtpxPackageAcceptanceEngine().Accept(new RtpxAcceptanceRequest(
            packagePath,
            CreateProfile(),
            outputDirectory)));

        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(Path.Combine(outputDirectory, "local-rtpx.json")));
        Assert.False(File.Exists(Path.Combine(outputDirectory, "structure-mapping.json")));
        Assert.Equal("existing", File.ReadAllText(Path.Combine(outputDirectory, "acceptance-report.json")));
    }

    [Fact]
    public void InstitutionProfileRoundTripsThroughJson()
    {
        var profile = CreateProfile();

        var roundTripped = RtpxInstitutionProfileStore.FromJson(RtpxInstitutionProfileStore.ToJson(profile));

        Assert.Equal(profile.Institution, roundTripped.Institution);
        Assert.Equal(profile.AcceptedBy, roundTripped.AcceptedBy);
        Assert.Equal(2, roundTripped.StructureMappings.Count);
    }

    [Fact]
    public void InstitutionProfileJsonRequiresInstitutionName()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => RtpxInstitutionProfileStore.FromJson("{}"));

        Assert.Contains("institution", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateRtpxPackage()
    {
        var directory = TempDirectory();
        var docxPath = Path.Combine(directory, "protocol.docx");
        var packagePath = Path.Combine(directory, "protocol.rtpx.zip");
        new RtpxWordTemplateGenerator().Create(docxPath);
        var result = new RtpxWordPackageStore().Create(docxPath, packagePath);
        Assert.True(result.WrotePackage);
        return packagePath;
    }

    private static string CreateRtpxPackage(RadiotherapyProtocolPackage package)
    {
        var directory = TempDirectory();
        var packagePath = Path.Combine(directory, "protocol.rtpx.zip");
        var manifest = new RtpxWordPackageManifest(
            "beamkit.rtpx.word-package/0.1",
            new DateTimeOffset(2026, 7, 12, 12, 0, 0, TimeSpan.Zero).ToString("O"),
            package.Id,
            package.Name,
            package.Version,
            package.SchemaVersion,
            "synthetic.docx",
            "synthetic",
            IncludesSourceDocument: false,
            new[]
            {
                RtpxWordPackageStore.RtpxEntryName,
                RtpxWordPackageStore.ManifestEntryName,
                RtpxWordPackageStore.ValidationEntryName
            });
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);
        WriteEntry(archive, RtpxWordPackageStore.RtpxEntryName, RadiotherapyProtocolPackageStore.ToJson(package));
        WriteEntry(archive, RtpxWordPackageStore.ManifestEntryName, JsonSerializer.Serialize(manifest, jsonOptions));
        WriteEntry(archive, RtpxWordPackageStore.ValidationEntryName, "{}");
        return packagePath;
    }

    private static RadiotherapyProtocolPackage CreateMultiPrescriptionPackage()
    {
        return new RadiotherapyProtocolPackage(
            "rtpx.synthetic.multi-rx",
            "Synthetic Multi Rx Protocol",
            "1.0",
            "Lung",
            "Definitive",
            structures: new[]
            {
                new ProtocolStructureRequirement("ptv", "PTV_5000", ProtocolStructureRole.Target),
                new ProtocolStructureRequirement("boost", "Boost", ProtocolStructureRole.Target),
                new ProtocolStructureRequirement("cord", "Cord", ProtocolStructureRole.OrganAtRisk)
            },
            prescriptions: new[]
            {
                new ProtocolPrescription("rx.primary", "PTV_5000", 50m, 5, technique: "VMAT", energy: "6X"),
                new ProtocolPrescription("rx.boost", "Boost", 20m, 5, technique: "VMAT", energy: "6X")
            });
    }

    private static RadiotherapyProtocolPackage CreatePackageWithPlanCheckParameters()
    {
        return new RadiotherapyProtocolPackage(
            "rtpx.synthetic.parameters",
            "Synthetic Parameter Protocol",
            "1.0",
            "Lung",
            "Definitive",
            structures: new[]
            {
                new ProtocolStructureRequirement("ptv", "PTV_5000", ProtocolStructureRole.Target),
                new ProtocolStructureRequirement("cord", "Cord", ProtocolStructureRole.OrganAtRisk)
            },
            prescriptions: new[]
            {
                new ProtocolPrescription("rx.primary", "PTV_5000", 50m, 5)
            },
            planChecks: new[]
            {
                new ProtocolPlanCheckRequirement(
                    "custom",
                    "Custom plan check",
                    "custom",
                    parameters: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["structure"] = "Cord",
                        ["technique"] = "PTV_5000"
                    })
            });
    }

    private static RtpxInstitutionProfile CreateProfile()
    {
        return new RtpxInstitutionProfile(
            "Synthetic Hospital",
            new[]
            {
                new RtpxStructureMapping("PTV_5000", "PTV_Hospital", notes: "Local target convention."),
                new RtpxStructureMapping("Cord", "SpinalCord")
            },
            acceptedBy: "Physics Director",
            effectiveDate: new DateOnly(2026, 7, 12),
            reviewedBy: "Protocol Physicist",
            localPolicyReference: "Synthetic policy",
            tags: new[] { "accepted", "synthetic" });
    }

    private static EsapiPlanSnapshot CreateMatchingSnapshot(
        decimal totalDoseGy = 50m,
        string targetStructureId = "PTV_Hospital",
        IEnumerable<EsapiStructureSnapshot>? extraStructures = null)
    {
        var structures = new List<EsapiStructureSnapshot>
        {
            new(targetStructureId, targetStructureId, StructureType.Target, 100m, HasContours: true),
            new("SpinalCord", "SpinalCord", StructureType.OrganAtRisk, 12m, HasContours: true)
        };
        structures.AddRange(extraStructures ?? Array.Empty<EsapiStructureSnapshot>());

        return new EsapiPlanSnapshot(
            "SYNTHETIC",
            null,
            "C1",
            "PlanA",
            new EsapiPrescriptionSnapshot(totalDoseGy, 5, targetStructureId, IsSigned: true, RequestedEnergy: "6X", RequestedTechniqueId: "VMAT"),
            structures,
            new EsapiDoseGridSnapshot(2.5m, 2.5m, 2.5m, "AAA", "16.1"),
            new[]
            {
                new EsapiDoseStatisticsSnapshot("PTV_Hospital", new Dictionary<string, decimal> { ["Max"] = 52m })
            },
            new[]
            {
                new EsapiBeamSnapshot(
                    "B1",
                    "Arc 1",
                    "Photon",
                    "6X",
                    MonitorUnits: 400m,
                    TechniqueId: "VMAT",
                    ControlPoints: new[] { new EsapiBeamControlPointSnapshot(0, 0m, 0m) },
                    BeamModelId: "SyntheticBeamModel")
            });
    }

    private static string TempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string Describe(RtpxAcceptanceReport report)
    {
        return string.Join(Environment.NewLine, report.Issues.Select(issue => $"{issue.Severity} {issue.Code}: {issue.Message}"));
    }

    private static void WriteEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }
}
