using BeamKit.Check;
using BeamKit.Core.Domain;
using BeamKit.Core.Serialization;
using BeamKit.Esapi;
using BeamKit.Samples;
using BeamKit.Sdk;
using Xunit;

namespace BeamKit.CiServer.Tests;

public sealed class BeamKitCiServerServiceTests
{
    [Fact]
    public void ListCasesReturnsSyntheticCaseLibrary()
    {
        var service = CreateService();

        var cases = service.ListCases();

        Assert.True(cases.Count >= 6);
        Assert.Contains(cases, clinicalCase => clinicalCase.Id == "head-neck-pass");
        Assert.Contains(cases, clinicalCase => clinicalCase.Id == "head-neck-cord-fail" && !clinicalCase.ExpectedToPass);
    }

    [Fact]
    public void CreateRunStoresPassingCiArtifact()
    {
        var store = new CiRunStore();
        var service = CreateService(store);

        var record = service.CreateRun(new HostedCiRunRequest
        {
            SyntheticCaseId = "head-neck-pass",
            Branch = "main",
            Commit = "abc123",
            BuildId = "build-1"
        });

        Assert.Equal("head-neck-pass", record.SyntheticCaseId);
        Assert.Equal(BeamKitCheckStatus.Pass, record.Status);
        Assert.Equal(0, record.ExitCode);
        Assert.Equal("main", record.Artifact.Provenance.Branch);
        Assert.True(record.HasPlanSnapshot);
        var summary = store.Find(record.Id);
        Assert.NotNull(summary);
        Assert.Equal(record.Id, summary.Id);
        Assert.Equal("main", summary.Branch);
        Assert.True(summary.HasPlanSnapshot);
        var artifactJson = store.FindArtifactJson(record.Id) ?? throw new InvalidOperationException("Artifact JSON was not stored.");
        Assert.Contains("planFingerprint", artifactJson, StringComparison.Ordinal);
        var planSnapshotJson = store.FindPlanSnapshotJson(record.Id) ?? throw new InvalidOperationException("Plan snapshot JSON was not stored.");
        Assert.Contains("\"plan\"", planSnapshotJson, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateRunWritesAuditEvent()
    {
        var store = new CiRunStore();
        var service = CreateService(store);

        var record = service.CreateRun(
            new HostedCiRunRequest { SyntheticCaseId = "head-neck-pass" },
            new CiServerAuditContext("physics-key", "/api/runs", "POST", "127.0.0.1"));

        var auditEvent = Assert.Single(store.ListAuditEvents(new CiServerAuditQuery { Action = "run.created" }));
        Assert.Equal("physics-key", auditEvent.Actor);
        Assert.Equal(record.Id, auditEvent.RunId);
        Assert.Equal(record.CaseId, auditEvent.CaseId);
        Assert.Equal("Pass", auditEvent.Status);
    }

    [Fact]
    public void CreateRunStoresFailingCiArtifact()
    {
        var service = CreateService();

        var record = service.CreateRun(new HostedCiRunRequest { SyntheticCaseId = "head-neck-cord-fail" });

        Assert.Equal(BeamKitCheckStatus.Fail, record.Status);
        Assert.Equal(2, record.ExitCode);
        Assert.True(record.Artifact.CheckReport.HasBlockingIssues);
    }

    [Fact]
    public void CreateRunFromBeamKitPlanJsonStoresUploadedPlanArtifact()
    {
        var store = new CiRunStore();
        var service = CreateService(store);
        var planJson = BeamKitPlanJson.ToJson(SyntheticClinicalCaseLibrary.HeadAndNeckBaseline().Plan);

        var record = service.CreateRunFromPlanSnapshot(new HostedCiRunUploadRequest
        {
            PlanJson = planJson,
            Branch = "main",
            Commit = "uploaded-plan",
            BuildId = "upload-1"
        });

        Assert.Equal(CiRunInputKind.BeamKitPlanJson, record.InputKind);
        Assert.Equal("HN-SYN-001", record.CaseId);
        Assert.Equal(BeamKitCheckStatus.Pass, record.Status);
        Assert.Equal("beamkit-plan-json:HN-SYN-001", record.Artifact.Provenance.InputSource);
        var summary = store.Find(record.Id) ?? throw new InvalidOperationException("Run summary was not stored.");
        Assert.Equal(CiRunInputKind.BeamKitPlanJson, summary.InputKind);
        Assert.Equal("HN-SYN-001", summary.CaseId);
        Assert.True(summary.HasPlanSnapshot);
        Assert.Contains("HN-SYN-001", store.FindPlanSnapshotJson(record.Id), StringComparison.Ordinal);
    }

    [Fact]
    public void CreateRunFromEsapiSnapshotJsonValidatesConvertsAndStoresArtifact()
    {
        var service = CreateService();
        var snapshotJson = EsapiPlanSnapshotJson.ToJson(CreateEsapiSnapshot(SyntheticClinicalCaseLibrary.HeadAndNeckBaseline().Plan));

        var record = service.CreateRunFromPlanSnapshot(new HostedCiRunUploadRequest
        {
            EsapiSnapshotJson = snapshotJson,
            Branch = "main",
            Commit = "uploaded-esapi",
            BuildId = "upload-2"
        });

        Assert.Equal(CiRunInputKind.EsapiSnapshotJson, record.InputKind);
        Assert.Equal("HN-SYN-001", record.CaseId);
        Assert.Equal(BeamKitCheckStatus.Pass, record.Status);
        Assert.Equal("esapi-snapshot:HN-SYN-001", record.Artifact.Provenance.InputSource);
        Assert.Equal("uploaded-esapi", record.Artifact.Provenance.Commit);
    }

    [Fact]
    public void CreateRunFromEsapiSnapshotJsonRejectsValidationErrors()
    {
        var service = CreateService();
        var snapshot = new EsapiPlanSnapshot(
            "SYN-0001",
            "Synthetic Patient",
            "C1",
            "BadPlan",
            new EsapiPrescriptionSnapshot(70m, 35, "PTV_7000", true),
            new[] { new EsapiStructureSnapshot("BODY", "Body", StructureType.External, 1000m, true) });

        var exception = Assert.Throws<InvalidOperationException>(() => service.CreateRunFromPlanSnapshot(new HostedCiRunUploadRequest
        {
            EsapiSnapshotJson = EsapiPlanSnapshotJson.ToJson(snapshot)
        }));

        Assert.Contains("structures.target-missing", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PromoteBaselineStoresRunMetadataForCase()
    {
        var service = CreateService();
        var run = service.CreateRun(new HostedCiRunRequest { SyntheticCaseId = "head-neck-pass", Branch = "main" });

        var baseline = service.PromoteBaseline(run.Id, new PromoteCiRunBaselineRequest
        {
            PromotedBy = "physics",
            Note = "Approved synthetic baseline."
        });

        Assert.Equal("head-neck-pass", baseline.CaseId);
        Assert.Equal(run.Id, baseline.BaselineRunId);
        Assert.Equal("physics", baseline.PromotedBy);
        Assert.Equal(run.Artifact.Provenance.PlanFingerprint, baseline.PlanFingerprint);
        Assert.Equal(baseline, service.FindBaseline("HEAD-NECK-PASS"));
        Assert.Single(service.ListBaselines());
    }

    [Fact]
    public void CompareToBaselineReturnsCleanReportForPromotedRun()
    {
        var service = CreateService();
        var run = service.CreateRun(new HostedCiRunRequest { SyntheticCaseId = "head-neck-pass" });
        service.PromoteBaseline(run.Id, new PromoteCiRunBaselineRequest());

        var report = service.CompareToBaseline(run.Id);

        Assert.True(report.MatchesBaseline);
        Assert.Empty(report.Findings);
        Assert.True(report.UsedPlanSnapshotComparison);
        Assert.NotNull(report.PlanChanges);
        Assert.Empty(report.PlanChanges.Changes);
        Assert.Equal(run.Id, report.BaselineRunId);
        Assert.Equal(run.Id, report.ComparisonRunId);
    }

    [Fact]
    public void CompareToBaselineFlagsChangedUploadedPlan()
    {
        var service = CreateService();
        var baselinePlan = SyntheticClinicalCaseLibrary.HeadAndNeckBaseline().Plan;
        var changedPlan = baselinePlan with
        {
            Prescription = baselinePlan.Prescription with
            {
                TotalDoseGy = 66m
            }
        };
        var baseline = service.CreateRunFromPlanSnapshot(new HostedCiRunUploadRequest
        {
            PlanJson = BeamKitPlanJson.ToJson(baselinePlan)
        });
        service.PromoteBaseline(baseline.Id, new PromoteCiRunBaselineRequest { PromotedBy = "physics" });
        var comparison = service.CreateRunFromPlanSnapshot(new HostedCiRunUploadRequest
        {
            PlanJson = BeamKitPlanJson.ToJson(changedPlan)
        });

        var report = service.CompareToBaseline(comparison.Id);

        Assert.False(report.MatchesBaseline);
        Assert.True(report.UsedPlanSnapshotComparison);
        Assert.NotNull(report.PlanChanges);
        Assert.Contains(report.PlanChanges.Changes, change => change.Subject == "Prescription.TotalDoseGy");
        Assert.Contains(report.Findings, finding => finding.Code == "plan-fingerprint.changed" && finding.Severity == CiRunBaselineFindingSeverity.Informational);
        Assert.Contains(report.Findings, finding => finding.Code == "prescription-fingerprint.changed" && finding.Severity == CiRunBaselineFindingSeverity.Informational);
        Assert.Contains(report.Findings, finding => finding.Code == "plan-change.prescriptionchanged" && finding.Severity == CiRunBaselineFindingSeverity.Blocking);
        Assert.Contains(report.Findings, finding => finding.Code == "status.changed");
    }

    [Fact]
    public void CompareToBaselineFallsBackToMetadataWhenSnapshotsAreMissing()
    {
        var store = new CiRunStore();
        var service = CreateService(store);
        var run = service.CreateRun(new HostedCiRunRequest { SyntheticCaseId = "head-neck-pass" });
        store.Save(run with { PlanSnapshotJson = null });
        service.PromoteBaseline(run.Id, new PromoteCiRunBaselineRequest());

        var report = service.CompareToBaseline(run.Id);

        Assert.True(report.MatchesBaseline);
        Assert.False(report.UsedPlanSnapshotComparison);
        Assert.Null(report.PlanChanges);
        Assert.False(store.Find(run.Id)!.HasPlanSnapshot);
    }

    [Fact]
    public void ValidateRulePackReturnsCleanPolicyReport()
    {
        var service = CreateService();

        var report = service.ValidateRulePack(new RulePackValidationServerRequest());

        Assert.True(report.IsValid);
        Assert.Equal(0, report.ErrorCount);
        Assert.StartsWith("sha256:", report.Fingerprint, StringComparison.Ordinal);
    }

    [Fact]
    public void ListRulePacksIncludesBuiltInSyntheticPack()
    {
        var service = CreateService();

        var rulePacks = service.ListRulePacks();

        var rulePack = Assert.Single(rulePacks);
        Assert.Equal(CiServerRulePackRegistry.BuiltInRulePackId, rulePack.Id);
        Assert.True(rulePack.IsLoadable);
        Assert.True(rulePack.IsValid);
        Assert.StartsWith("sha256:", rulePack.Fingerprint, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateRulePackCanUseRegisteredRulePackId()
    {
        var service = CreateService();

        var report = service.ValidateRulePack(new RulePackValidationServerRequest
        {
            RulePackId = CiServerRulePackRegistry.BuiltInRulePackId
        });

        Assert.True(report.IsValid);
        Assert.StartsWith("sha256:", report.Fingerprint, StringComparison.Ordinal);
    }

    [Fact]
    public void TestRulePackRunsDefaultRegressionSuite()
    {
        var service = CreateService();

        var report = service.TestRulePack(new RulePackTestServerRequest());

        Assert.True(report.Passed);
        Assert.Equal(3, report.PassedCount);
    }

    [Fact]
    public void TestRulePackCanRunSingleSyntheticCase()
    {
        var service = CreateService();

        var report = service.TestRulePack(new RulePackTestServerRequest { SyntheticCaseId = "head-neck-cord-fail" });

        var result = Assert.Single(report.Results);
        Assert.True(result.Passed);
        Assert.Equal(BeamKitCheckStatus.Fail, result.ActualStatus);
    }

    [Fact]
    public void ImportRulePackStoresValidationTestAndAuditHistory()
    {
        var store = new CiRunStore();
        var service = CreateService(store);

        var result = service.ImportRulePack(
            new RulePackImportServerRequest
            {
                RulePackId = "institution-head-neck",
                ManifestPath = SampleRulePackPath(),
                ImportedBy = "physics"
            },
            new CiServerAuditContext("physics-key", "/api/rule-packs/import", "POST"));

        Assert.Equal("institution-head-neck", result.Version.RulePackId);
        Assert.True(result.Validation.IsValid);
        Assert.True(result.TestReport?.Passed);
        Assert.False(result.Activated);
        var stored = Assert.Single(service.ListManagedRulePackVersions("institution-head-neck"));
        Assert.Equal(result.Version.VersionId, stored.VersionId);
        Assert.True(stored.TestPassed);
        var auditEvent = Assert.Single(store.ListAuditEvents(new CiServerAuditQuery { Action = "rule-pack.imported" }));
        Assert.Equal("physics-key", auditEvent.Actor);
        Assert.Equal("institution-head-neck", auditEvent.CaseId);
    }

    [Fact]
    public void PromoteManagedRulePackVersionMakesItAvailableForRuns()
    {
        var service = CreateService();
        var imported = service.ImportRulePack(new RulePackImportServerRequest
        {
            RulePackId = "institution-head-neck",
            ManifestPath = SampleRulePackPath()
        });

        var promoted = service.PromoteManagedRulePackVersion(
            imported.Version.RulePackId,
            imported.Version.VersionId,
            new RulePackPromotionServerRequest { PromotedBy = "physics", Note = "Approved for use." });
        var run = service.CreateRun(new HostedCiRunRequest
        {
            SyntheticCaseId = "head-neck-pass",
            RulePackId = "institution-head-neck"
        });

        Assert.True(promoted.IsActive);
        Assert.Equal(promoted.Fingerprint, run.Artifact.Provenance.RulePackFingerprint);
        Assert.Contains(service.ListRulePacks(), rulePack => rulePack.Id == "institution-head-neck" && rulePack.SourceKind == "Managed");
        Assert.Equal("institution-head-neck", service.FindRulePack("institution-head-neck")?.Summary.Id);
    }

    [Fact]
    public void PromoteManagedRulePackVersionRequiresRegressionTests()
    {
        var service = CreateService();
        var imported = service.ImportRulePack(new RulePackImportServerRequest
        {
            RulePackId = "institution-head-neck",
            ManifestPath = SampleRulePackPath(),
            RunRegressionTests = false
        });

        var exception = Assert.Throws<InvalidOperationException>(() => service.PromoteManagedRulePackVersion(
            imported.Version.RulePackId,
            imported.Version.VersionId,
            new RulePackPromotionServerRequest()));

        Assert.Contains("before regression tests pass", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TestManagedRulePackVersionStoresLatestReport()
    {
        var service = CreateService();
        var imported = service.ImportRulePack(new RulePackImportServerRequest
        {
            RulePackId = "institution-head-neck",
            ManifestPath = SampleRulePackPath(),
            RunRegressionTests = false
        });

        var report = service.TestManagedRulePackVersion(
            imported.Version.RulePackId,
            imported.Version.VersionId,
            new RulePackVersionTestServerRequest { SyntheticCaseId = "head-neck-pass" });
        var detail = service.FindManagedRulePackVersion(imported.Version.RulePackId, imported.Version.VersionId)
            ?? throw new InvalidOperationException("Version detail was not stored.");

        Assert.True(report.Passed);
        Assert.Equal(1, report.PassedCount);
        Assert.True(detail.TestReport?.Passed);
        Assert.Equal(1, detail.TestReport?.PassedCount);
    }

    [Fact]
    public void RecommendAssignmentReturnsRankedPlanner()
    {
        var service = CreateService();

        var recommendation = service.RecommendAssignment(new AssignmentServerRequest
        {
            DiseaseSite = "Head and Neck",
            RequiredSkills = new[] { "VMAT" },
            DueDate = "2026-07-12",
            ComplexityScore = 4,
            Priority = 4
        });

        Assert.Equal("planner-jane", recommendation.RecommendedPlanner?.Planner.Id);
        Assert.Contains(recommendation.RecommendedPlanner!.Reasons, reason => reason.Contains("All required skills", StringComparison.Ordinal));
    }

    private static BeamKitCiServerService CreateService(ICiRunStore? store = null)
    {
        return new BeamKitCiServerService(new BeamKitClient(), store ?? new CiRunStore(), new FixedTimeProvider());
    }

    private static string SampleRulePackPath()
    {
        return Path.Combine(FindRepositoryRoot(), "samples", "rule-packs", "head-neck-v1", "beamkit-rule-pack.json");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BeamKit.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find BeamKit repository root.");
    }

    private static EsapiPlanSnapshot CreateEsapiSnapshot(Plan plan)
    {
        return new EsapiPlanSnapshot(
            plan.Patient.Id,
            plan.Patient.DisplayName,
            plan.CourseId,
            plan.Id,
            new EsapiPrescriptionSnapshot(
                plan.Prescription.TotalDoseGy,
                plan.Prescription.FractionCount,
                plan.Prescription.TargetStructureId,
                plan.Prescription.IsSigned,
                plan.Prescription.Intent,
                plan.Prescription.RequestedEnergy,
                plan.Prescription.RequestedTechniqueId),
            plan.Structures.Select(structure => new EsapiStructureSnapshot(
                structure.Id,
                structure.Name,
                structure.Type,
                structure.VolumeCc,
                structure.HasContours)),
            plan.Dose is null
                ? null
                : new EsapiDoseGridSnapshot(
                    plan.Dose.Grid.SpacingXMm,
                    plan.Dose.Grid.SpacingYMm,
                    plan.Dose.Grid.SpacingZMm,
                    plan.Dose.CalculationModel,
                    plan.Dose.CalculationModelVersion),
            plan.Dose?.Statistics.Select(statistics => new EsapiDoseStatisticsSnapshot(statistics.StructureId, statistics.Metrics)),
            plan.Beams.Select(beam => new EsapiBeamSnapshot(
                beam.Id,
                beam.Name,
                beam.Modality,
                beam.Energy,
                beam.GantryAngleDegrees,
                beam.MonitorUnits,
                beam.TreatmentUnitId,
                beam.TechniqueId,
                beam.IsSetupField,
                beam.ControlPoints.Select(controlPoint => new EsapiBeamControlPointSnapshot(
                    controlPoint.Index,
                    controlPoint.GantryAngleDegrees,
                    controlPoint.CumulativeMetersetWeight,
                    controlPoint.JawPositions is null
                        ? null
                        : new EsapiBeamJawPositionsSnapshot(
                            controlPoint.JawPositions.X1Cm,
                            controlPoint.JawPositions.X2Cm,
                            controlPoint.JawPositions.Y1Cm,
                            controlPoint.JawPositions.Y2Cm))).ToArray(),
                beam.BeamModelId,
                beam.JawTrackingEnabled)),
            plan.DiseaseSite);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
        }
    }
}
