using System.Text.Json;
using BeamKit.Check;
using BeamKit.PlanCheck;
using BeamKit.RulePacks;
using Xunit;

namespace BeamKit.RulePacks.Tests;

public sealed class RulePackAuthoringTests
{
    [Fact]
    public void ManifestStoreRoundTripsApprovalMetadata()
    {
        var manifest = new RulePackManifest(
            "Institution HN",
            "2026.1",
            "clinical-rules.json",
            "plan-checks.json",
            owner: "Physics",
            diseaseSite: "Head and Neck",
            tags: new[] { "head-neck", "baseline" },
            approval: new RulePackApprovalMetadata(
                status: "Approved",
                institution: "Example",
                reviewedBy: "Physics",
                approvedBy: "Director",
                effectiveDate: new DateOnly(2026, 7, 1),
                reviewDueDate: new DateOnly(2027, 7, 1),
                reference: "Policy",
                rationale: "Baseline review"));

        var json = RulePackManifestStore.ToJson(manifest);
        var loaded = RulePackManifestStore.FromJson(json);

        Assert.Equal("Institution HN", loaded.Name);
        Assert.Equal("Approved", loaded.Approval?.Status);
        Assert.Equal(new DateOnly(2027, 7, 1), loaded.Approval?.ReviewDueDate);
        Assert.Equal(new[] { "baseline", "head-neck" }, loaded.Tags);
    }

    [Fact]
    public void StarterScaffoldCreatesLoadableRulePack()
    {
        using var directory = TemporaryDirectory.Create();
        var scaffold = new RulePackStarterScaffoldFactory().Create("lung-sbrt", owner: "Physics", institution: "Synthetic");

        scaffold.WriteToDirectory(directory.Path);

        var manifestPath = Path.Combine(directory.Path, scaffold.ManifestPath);
        var rulePack = BeamKitRulePackLoader.FromFile(manifestPath);
        var doctor = new RulePackDoctor(new FixedTimeProvider()).InspectFile(manifestPath);

        Assert.Equal("Lung SBRT", rulePack.DiseaseSite);
        Assert.True(doctor.Validation.IsValid);
        Assert.Equal(0, doctor.ErrorCount);
        Assert.Contains(scaffold.Files, file => file.RelativePath == "regression-suite.json");

        var regressionSuite = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory.Path, "regression-suite.json")));
        Assert.Equal("lung-sbrt-pass", regressionSuite.RootElement.GetProperty("availableSyntheticCaseIds")[0].GetString());
        Assert.Contains(
            regressionSuite.RootElement.GetProperty("recommendedFutureCaseIds").EnumerateArray(),
            id => id.GetString() == "lung-sbrt-fail");
    }

    [Fact]
    public void DifferReportsPlanCheckThresholdChanges()
    {
        using var oldDirectory = TemporaryDirectory.Create();
        using var newDirectory = TemporaryDirectory.Create();
        var factory = new RulePackStarterScaffoldFactory();
        factory.Create("prostate", institution: "Synthetic").WriteToDirectory(oldDirectory.Path);
        factory.Create("prostate", institution: "Synthetic").WriteToDirectory(newDirectory.Path);
        var newCatalogPath = Path.Combine(newDirectory.Path, "plan-checks.json");
        var catalog = PlanCheckCatalogLoader.FromFile(newCatalogPath);
        var changedChecks = catalog.Checks.Select(check =>
            check.Id == "target.d95"
                ? check with { Parameters = new Dictionary<string, string>(check.Parameters) { ["minPercentPrescription"] = "97" } }
                : check);
        PlanCheckCatalogStore.Save(newCatalogPath, catalog with { Checks = changedChecks.ToArray() });

        var report = new RulePackDiffer().CompareFiles(
            Path.Combine(oldDirectory.Path, "beamkit-rule-pack.json"),
            Path.Combine(newDirectory.Path, "beamkit-rule-pack.json"));

        Assert.True(report.HasPolicyRelevantChanges);
        Assert.Contains(report.Changes, change =>
            change.Area == "PlanCheck"
            && change.Id == "target.d95"
            && change.Property == "parameters.minPercentPrescription"
            && change.NewValue == "97");
    }

    [Fact]
    public void ReminderParserCreatesPlanChecks()
    {
        var checks = new RulePackReminderParser().Parse("""
            # Monthly reminders

            ## dose.grid.review
            title: Dose grid review
            type: dose-grid-max-spacing
            severity: Failure
            reference: July reminder email
            parameter.maxSpacingMm: 2.0
            """);

        var check = Assert.Single(checks);
        Assert.Equal("dose.grid.review", check.Id);
        Assert.Equal(PlanCheckSeverity.Failure, check.Severity);
        Assert.Equal("2.0", check.Parameters["maxSpacingMm"]);
    }

    [Fact]
    public void PlanCheckCatalogStoreRejectsDuplicateAddedCheck()
    {
        var catalog = new PlanCheckCatalog(
            "Catalog",
            "1",
            new[] { new PlanCheckDefinition("one", "One", "dose-exists") });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PlanCheckCatalogStore.AddCheck(catalog, new PlanCheckDefinition("ONE", "Duplicate", "dose-exists")));

        Assert.Contains("already contains", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DoctorFlagsOverdueApprovalReview()
    {
        using var directory = TemporaryDirectory.Create();
        new RulePackStarterScaffoldFactory().Create("brain-srs", institution: "Synthetic").WriteToDirectory(directory.Path);
        var manifestPath = Path.Combine(directory.Path, "beamkit-rule-pack.json");
        var manifest = RulePackManifestStore.FromFile(manifestPath) with
        {
            Approval = new RulePackApprovalMetadata(
                status: "Approved",
                institution: "Synthetic",
                reviewedBy: "Physics",
                approvedBy: "Director",
                effectiveDate: new DateOnly(2026, 1, 1),
                reviewDueDate: new DateOnly(2026, 6, 1),
                reference: "Policy",
                rationale: "Baseline")
        };
        RulePackManifestStore.Save(manifestPath, manifest);

        var report = new RulePackDoctor(new FixedTimeProvider()).InspectFile(manifestPath);

        Assert.Contains(report.Issues, issue => issue.Code == "approval.review-overdue");
    }

    [Fact]
    public void DoctorReturnsStructuredReportForMissingCatalogFile()
    {
        using var directory = TemporaryDirectory.Create();
        new RulePackStarterScaffoldFactory().Create("prostate", institution: "Synthetic").WriteToDirectory(directory.Path);
        File.Delete(Path.Combine(directory.Path, "plan-checks.json"));

        var report = new RulePackDoctor(new FixedTimeProvider()).InspectFile(Path.Combine(directory.Path, "beamkit-rule-pack.json"));

        Assert.False(report.IsHealthy);
        Assert.Contains(report.Issues, issue => issue.Code == "manifest.planCheckCatalog.missing-file");
        Assert.Contains(report.Validation.Issues, issue => issue.Code == "rule-pack.load-failed");
    }

    [Fact]
    public void BundleBuilderCreatesVerifiedLoadableBundle()
    {
        using var directory = TemporaryDirectory.Create();
        new RulePackStarterScaffoldFactory().Create("lung-sbrt", institution: "Synthetic").WriteToDirectory(directory.Path);
        var manifestPath = Path.Combine(directory.Path, "beamkit-rule-pack.json");

        var bundle = new RulePackBundleBuilder(new FixedTimeProvider()).FromFile(manifestPath, createdBy: "physics");
        var roundTripped = RulePackBundleStore.FromJson(RulePackBundleStore.ToJson(bundle));
        var report = new RulePackBundleVerifier().Verify(roundTripped);
        var loaded = RulePackBundleLoader.ToRulePack(roundTripped);

        Assert.True(report.IsValid);
        Assert.Equal("physics", roundTripped.CreatedBy);
        Assert.Equal(bundle.RulePackFingerprint, BeamKit.Check.RulePackFingerprint.Compute(loaded));
        Assert.Contains(roundTripped.Files, file => file.ManifestProperty == "clinicalRuleCatalog");
    }

    [Fact]
    public void BundleVerifierFlagsTamperedEmbeddedFile()
    {
        using var directory = TemporaryDirectory.Create();
        new RulePackStarterScaffoldFactory().Create("prostate", institution: "Synthetic").WriteToDirectory(directory.Path);
        var bundle = new RulePackBundleBuilder(new FixedTimeProvider()).FromFile(Path.Combine(directory.Path, "beamkit-rule-pack.json"));
        var tamperedFiles = bundle.Files.Select(file => file.ManifestProperty == "planCheckCatalog"
            ? file with { Content = file.Content + Environment.NewLine }
            : file);

        var tampered = bundle with { Files = tamperedFiles.ToArray() };
        var report = new RulePackBundleVerifier().Verify(tampered);

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, issue => issue.Code == "bundle.file-hash-mismatch");
    }

    [Fact]
    public void BundleLoadIsIndependentOfSourceFileDrift()
    {
        using var directory = TemporaryDirectory.Create();
        new RulePackStarterScaffoldFactory().Create("brain-srs", institution: "Synthetic").WriteToDirectory(directory.Path);
        var manifestPath = Path.Combine(directory.Path, "beamkit-rule-pack.json");
        var bundle = new RulePackBundleBuilder(new FixedTimeProvider()).FromFile(manifestPath);
        var bundledFingerprint = BeamKit.Check.RulePackFingerprint.Compute(RulePackBundleLoader.ToRulePack(bundle));
        var catalogPath = Path.Combine(directory.Path, "plan-checks.json");
        var catalog = PlanCheckCatalogLoader.FromFile(catalogPath);
        var changedChecks = catalog.Checks.Select(check => check.Id == "target.d95"
            ? check with { Parameters = new Dictionary<string, string>(check.Parameters) { ["minPercentPrescription"] = "99" } }
            : check);
        PlanCheckCatalogStore.Save(catalogPath, catalog with { Checks = changedChecks.ToArray() });

        var sourceFingerprintAfterDrift = BeamKit.Check.RulePackFingerprint.Compute(BeamKitRulePackLoader.FromFile(manifestPath));
        var bundledFingerprintAfterDrift = BeamKit.Check.RulePackFingerprint.Compute(RulePackBundleLoader.ToRulePack(bundle));

        Assert.NotEqual(bundledFingerprint, sourceFingerprintAfterDrift);
        Assert.Equal(bundledFingerprint, bundledFingerprintAfterDrift);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(2026, 7, 11, 0, 0, 0, TimeSpan.Zero);
        }
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
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beamkit-rulepacks-tests", Guid.NewGuid().ToString("N"));
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
