using BeamKit.Check;
using BeamKit.Core.Domain;
using BeamKit.Core.Serialization;
using BeamKit.Esapi;
using BeamKit.PlanCheck;
using BeamKit.RulePacks;
using BeamKit.Samples;
using BeamKit.Sdk;
using BeamKit.Workflow;
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
    public void ImportRulePackStoresImmutableBundleAndIgnoresSourceDrift()
    {
        var root = CreateTemporarySampleRulePackCopy();
        try
        {
            var manifestPath = Path.Combine(root, "samples", "rule-packs", "head-neck-v1", "beamkit-rule-pack.json");
            var catalogPath = Path.Combine(root, "samples", "plan-check-baseline.json");
            var store = new CiRunStore();
            var service = CreateService(store);
            var imported = service.ImportRulePack(new RulePackImportServerRequest
            {
                RulePackId = "institution-head-neck",
                ManifestPath = manifestPath
            });
            var promoted = service.PromoteManagedRulePackVersion(
                imported.Version.RulePackId,
                imported.Version.VersionId,
                new RulePackPromotionServerRequest { PromotedBy = "physics" });
            var storedVersion = store.FindRulePackVersion(promoted.RulePackId, promoted.VersionId)
                ?? throw new InvalidOperationException("Managed version was not found.");
            MutateCordMaxThreshold(catalogPath, "1");

            var run = service.CreateRun(new HostedCiRunRequest
            {
                SyntheticCaseId = "head-neck-pass",
                RulePackId = "institution-head-neck"
            });

            Assert.False(string.IsNullOrWhiteSpace(storedVersion.VersionId));
            Assert.NotNull(storedVersion.BundleJson);
            Assert.Equal(promoted.Fingerprint, run.Artifact.Provenance.RulePackFingerprint);
            Assert.Equal(BeamKitCheckStatus.Pass, run.Status);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ImportRulePackAcceptsVerifiedBundle()
    {
        var root = CreateTemporarySampleRulePackCopy();
        try
        {
            var manifestPath = Path.Combine(root, "samples", "rule-packs", "head-neck-v1", "beamkit-rule-pack.json");
            var bundle = new RulePackBundleBuilder(new FixedTimeProvider()).FromFile(manifestPath);
            var service = CreateService();

            var imported = service.ImportRulePack(new RulePackImportServerRequest
            {
                RulePackId = "institution-head-neck",
                BundleJson = RulePackBundleStore.ToJson(bundle)
            });

            Assert.True(imported.Validation.IsValid);
            Assert.Equal(bundle.RulePackFingerprint, imported.Version.Fingerprint);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PromoteManagedRulePackVersionCanRollbackToPreviousBundle()
    {
        var root = CreateTemporarySampleRulePackCopy();
        try
        {
            var manifestPath = Path.Combine(root, "samples", "rule-packs", "head-neck-v1", "beamkit-rule-pack.json");
            var catalogPath = Path.Combine(root, "samples", "plan-check-baseline.json");
            var service = CreateService();
            var first = service.ImportRulePack(new RulePackImportServerRequest
            {
                RulePackId = "institution-head-neck",
                ManifestPath = manifestPath
            });
            MutateCordMaxThreshold(catalogPath, "44");
            var second = service.ImportRulePack(new RulePackImportServerRequest
            {
                RulePackId = "institution-head-neck",
                ManifestPath = manifestPath,
                SyntheticCaseId = "head-neck-pass"
            });

            service.PromoteManagedRulePackVersion(second.Version.RulePackId, second.Version.VersionId, new RulePackPromotionServerRequest { PromotedBy = "physics" });
            var rolledBack = service.PromoteManagedRulePackVersion(first.Version.RulePackId, first.Version.VersionId, new RulePackPromotionServerRequest { PromotedBy = "physics", Note = "Rollback." });

            Assert.True(rolledBack.IsActive);
            Assert.Equal(first.Version.VersionId, service.ListManagedRulePackVersions("institution-head-neck").Single(version => version.IsActive).VersionId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
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
    public void ReviewRulePackDraftComparesAgainstActiveVersion()
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
            new RulePackPromotionServerRequest { PromotedBy = "physics" });

        var review = service.ReviewRulePackDraft(
            "institution-head-neck",
            new RulePackImportServerRequest
            {
                ManifestPath = SampleRulePackPath(),
                RunRegressionTests = true
            });

        Assert.True(review.IsPromotable);
        Assert.Equal(promoted.VersionId, review.ComparedToVersionId);
        Assert.True(review.Validation.IsValid);
        Assert.True(review.TestReport?.Passed);
        Assert.Empty(review.Diff.Changes);
    }

    [Fact]
    public void CompareManagedRulePackVersionsReturnsDiffReport()
    {
        var service = CreateService();
        var first = service.ImportRulePack(new RulePackImportServerRequest
        {
            RulePackId = "institution-head-neck",
            ManifestPath = SampleRulePackPath()
        });
        var second = service.ImportRulePack(new RulePackImportServerRequest
        {
            RulePackId = "institution-head-neck",
            ManifestPath = SampleRulePackPath()
        });

        var diff = service.CompareManagedRulePackVersions(
            "institution-head-neck",
            first.Version.VersionId,
            second.Version.VersionId);

        Assert.Equal(first.Validation.Fingerprint, diff.OldFingerprint);
        Assert.Equal(second.Validation.Fingerprint, diff.NewFingerprint);
        Assert.False(diff.HasPolicyRelevantChanges);
        Assert.Empty(diff.Changes);
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

    [Fact]
    public void RecommendStaffingReturnsDosimetristAndPhysicist()
    {
        var service = CreateService();

        var recommendation = service.RecommendStaffing(new AssignmentServerRequest
        {
            DiseaseSite = "Lung",
            RequiredSkills = new[] { "VMAT", "SBRT" },
            DueDate = "2026-07-12",
            ComplexityScore = 4,
            Priority = 4,
            Physician = "Dr Smith"
        });

        Assert.True(recommendation.IsFullyStaffed);
        Assert.Equal("planner-jane", recommendation.RoleRecommendations.Single(role => role.Role == PlanningStaffRole.Dosimetrist).RecommendedCandidate?.Planner.Id);
        Assert.Equal("physicist-morgan", recommendation.RoleRecommendations.Single(role => role.Role == PlanningStaffRole.Physicist).RecommendedCandidate?.Planner.Id);
    }

    [Fact]
    public void RecommendStaffingUsesEmbeddedRoster()
    {
        var service = CreateService();
        var roster = new StaffRoster(
            "Embedded roster",
            new[]
            {
                new StaffRosterMember(
                    "embedded-dosimetrist",
                    "Embedded Dosimetrist",
                    PlanningStaffRole.Dosimetrist,
                    new[] { "VMAT", "SBRT", "Lung" },
                    new[] { "Lung" },
                    activeCaseCount: 1,
                    maxActiveCaseCount: 8,
                    preferredPhysicians: new[] { "Dr Smith" },
                    schedule: new[] { new PlannerScheduleDay(new DateOnly(2026, 7, 9), assignedCaseCount: 0, capacity: 2) }),
                new StaffRosterMember(
                    "embedded-physicist",
                    "Embedded Physicist",
                    PlanningStaffRole.Physicist,
                    new[] { "VMAT", "SBRT", "Machine QA" },
                    new[] { "Lung" },
                    activeCaseCount: 2,
                    maxActiveCaseCount: 10,
                    schedule: new[] { new PlannerScheduleDay(new DateOnly(2026, 7, 9), assignedCaseCount: 0, capacity: 2) })
            });

        var recommendation = service.RecommendStaffing(new AssignmentServerRequest
        {
            DiseaseSite = "Lung",
            RequiredSkills = new[] { "VMAT", "SBRT" },
            RequiredRoles = new[] { "Dosimetrist", "Physicist" },
            DueDate = "2026-07-11",
            ComplexityScore = 4,
            Priority = 4,
            Physician = "Dr Smith",
            Roster = roster
        });

        Assert.True(recommendation.IsFullyStaffed);
        Assert.Equal("embedded-dosimetrist", recommendation.RoleRecommendations.Single(role => role.Role == PlanningStaffRole.Dosimetrist).RecommendedCandidate?.Planner.Id);
        Assert.Equal("embedded-physicist", recommendation.RoleRecommendations.Single(role => role.Role == PlanningStaffRole.Physicist).RecommendedCandidate?.Planner.Id);
    }

    [Fact]
    public void RecommendStaffingInfersIntelligenceFromSyntheticCase()
    {
        var service = CreateService();
        var roster = new StaffRoster(
            "Synthetic intelligent assignment roster",
            new[]
            {
                new StaffRosterMember(
                    "lung-dosimetrist",
                    "Lung SBRT Dosimetrist",
                    PlanningStaffRole.Dosimetrist,
                    new[] { "VMAT", "SBRT", "Lung" },
                    new[] { "Lung" },
                    activeCaseCount: 1,
                    maxActiveCaseCount: 8,
                    maxComplexityScore: 5),
                new StaffRosterMember(
                    "sbrt-physicist",
                    "SBRT Physicist",
                    PlanningStaffRole.Physicist,
                    new[] { "VMAT", "SBRT", "Lung", "Machine QA" },
                    new[] { "Lung" },
                    activeCaseCount: 2,
                    maxActiveCaseCount: 10,
                    maxComplexityScore: 5)
            });

        var recommendation = service.RecommendStaffing(new AssignmentServerRequest
        {
            SyntheticCaseId = "lung-sbrt-pass",
            DueDate = "2026-07-12",
            Priority = 4,
            Roster = roster
        });

        Assert.True(recommendation.IsFullyStaffed);
        Assert.NotNull(recommendation.Intelligence);
        Assert.Equal("LUNG-SBRT-SYN-001", recommendation.Intelligence.PlanId);
        Assert.Contains("SBRT", recommendation.Intelligence.SuggestedSkills);
        Assert.True(recommendation.Intelligence.AppliedAssignmentComplexityScore >= 3);
        Assert.Equal("lung-dosimetrist", recommendation.RoleRecommendations.Single(role => role.Role == PlanningStaffRole.Dosimetrist).RecommendedCandidate?.Planner.Id);
        Assert.Equal("sbrt-physicist", recommendation.RoleRecommendations.Single(role => role.Role == PlanningStaffRole.Physicist).RecommendedCandidate?.Planner.Id);
    }

    [Fact]
    public void WorkItemAssignmentRecommendationsAccountForLiveQueueWorkload()
    {
        var store = new CiRunStore();
        var service = CreateService(store);
        var roster = CreateLiveWorkloadRoster();
        var first = service.CreateWorkItem(new CreateCaseWorkItemRequest
        {
            SyntheticCaseId = "lung-sbrt-pass",
            DueDate = "2026-07-12",
            Priority = 4,
            Physician = "Dr Smith"
        });
        service.AssignWorkItem(first.Id, new AssignCaseWorkItemRequest
        {
            DosimetristId = "lung-dosimetrist",
            PhysicistId = "sbrt-physicist",
            Note = "Accepted first recommendation."
        });
        var second = service.CreateWorkItem(new CreateCaseWorkItemRequest
        {
            SyntheticCaseId = "lung-sbrt-pass",
            DueDate = "2026-07-12",
            Priority = 4,
            Physician = "Dr Smith"
        });

        var recommendation = service.RecommendWorkItemAssignment(second.Id, new AssignmentServerRequest
        {
            Roster = roster
        });

        var dosimetristRecommendation = recommendation.RoleRecommendations.Single(role => role.Role == PlanningStaffRole.Dosimetrist);
        Assert.Equal("backup-dosimetrist", dosimetristRecommendation.RecommendedCandidate?.Planner.Id);
        var saturatedCandidate = dosimetristRecommendation.Recommendation.Candidates.Single(candidate => candidate.Planner.Id == "lung-dosimetrist");
        Assert.Contains(saturatedCandidate.Reasons, reason => reason.Contains("At or above configured workload capacity", StringComparison.Ordinal));
        Assert.Equal(2, store.FindWorkItem(second.Id)?.AssignmentHistory.Count);
        Assert.Single(store.ListAuditEvents(new CiServerAuditQuery { Action = "work-item.assignment-recommended" }));
    }

    private static BeamKitCiServerService CreateService(ICiRunStore? store = null)
    {
        return new BeamKitCiServerService(new BeamKitClient(), store ?? new CiRunStore(), new FixedTimeProvider());
    }

    private static StaffRoster CreateLiveWorkloadRoster()
    {
        return new StaffRoster(
            "Live workload roster",
            new[]
            {
                new StaffRosterMember(
                    "lung-dosimetrist",
                    "Lung SBRT Dosimetrist",
                    PlanningStaffRole.Dosimetrist,
                    new[] { "VMAT", "SBRT", "Lung" },
                    new[] { "Lung" },
                    activeCaseCount: 0,
                    maxActiveCaseCount: 1,
                    maxComplexityScore: 5),
                new StaffRosterMember(
                    "backup-dosimetrist",
                    "Backup Lung Dosimetrist",
                    PlanningStaffRole.Dosimetrist,
                    new[] { "VMAT", "SBRT", "Lung" },
                    new[] { "Lung" },
                    activeCaseCount: 0,
                    maxActiveCaseCount: 8,
                    maxComplexityScore: 5),
                new StaffRosterMember(
                    "sbrt-physicist",
                    "SBRT Physicist",
                    PlanningStaffRole.Physicist,
                    new[] { "VMAT", "SBRT", "Lung", "Machine QA" },
                    new[] { "Lung" },
                    activeCaseCount: 0,
                    maxActiveCaseCount: 10,
                    maxComplexityScore: 5)
            });
    }

    private static string SampleRulePackPath()
    {
        return Path.Combine(FindRepositoryRoot(), "samples", "rule-packs", "head-neck-v1", "beamkit-rule-pack.json");
    }

    private static string CreateTemporarySampleRulePackCopy()
    {
        var repositoryRoot = FindRepositoryRoot();
        var root = Path.Combine(Path.GetTempPath(), "beamkit-rule-pack-" + Guid.NewGuid().ToString("N"));
        var sampleRoot = Path.Combine(root, "samples");
        var rulePackRoot = Path.Combine(sampleRoot, "rule-packs", "head-neck-v1");
        Directory.CreateDirectory(rulePackRoot);

        CopySample(repositoryRoot, root, "samples", "rule-catalog-head-neck.json");
        CopySample(repositoryRoot, root, "samples", "plan-check-baseline.json");
        CopySample(repositoryRoot, root, "samples", "naming-dictionary-head-neck.json");
        CopySample(repositoryRoot, root, "samples", "machine-profile-synthetic.json");
        CopySample(repositoryRoot, root, "samples", "rule-packs", "head-neck-v1", "beamkit-rule-pack.json");

        return root;
    }

    private static void CopySample(string repositoryRoot, string targetRoot, params string[] relativeSegments)
    {
        var relativePath = Path.Combine(relativeSegments);
        var source = Path.Combine(repositoryRoot, relativePath);
        var target = Path.Combine(targetRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target);
    }

    private static void MutateCordMaxThreshold(string catalogPath, string threshold)
    {
        var catalog = PlanCheckCatalogLoader.FromFile(catalogPath);
        var changedChecks = catalog.Checks.Select(check =>
            string.Equals(check.Id, "cord.max", StringComparison.OrdinalIgnoreCase)
                ? check with { Parameters = new Dictionary<string, string>(check.Parameters) { ["threshold"] = threshold } }
                : check);

        PlanCheckCatalogStore.Save(catalogPath, catalog with { Checks = changedChecks.ToArray() });
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
