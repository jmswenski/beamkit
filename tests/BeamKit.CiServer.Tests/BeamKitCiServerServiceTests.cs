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
        var summary = store.Find(record.Id);
        Assert.NotNull(summary);
        Assert.Equal(record.Id, summary.Id);
        Assert.Equal("main", summary.Branch);
        var artifactJson = store.FindArtifactJson(record.Id) ?? throw new InvalidOperationException("Artifact JSON was not stored.");
        Assert.Contains("planFingerprint", artifactJson, StringComparison.Ordinal);
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
    public void ValidateRulePackReturnsCleanPolicyReport()
    {
        var service = CreateService();

        var report = service.ValidateRulePack(new RulePackValidationServerRequest());

        Assert.True(report.IsValid);
        Assert.Equal(0, report.ErrorCount);
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
