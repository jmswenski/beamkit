using BeamKit.Core.Domain;
using BeamKit.Deliverability;
using BeamKit.Naming;
using BeamKit.PlanCheck;
using BeamKit.Rules;
using BeamKit.Samples;
using BeamKit.Templates;
using Xunit;

namespace BeamKit.Check.Tests;

public sealed class RulePackAutomationTests
{
    [Fact]
    public void PolicyValidatorMarksDefaultRulePackValid()
    {
        var report = new RulePackPolicyValidator().Validate(CreateRulePack());

        Assert.True(report.IsValid);
        Assert.StartsWith("sha256:", report.Fingerprint, StringComparison.Ordinal);
        Assert.Equal(0, report.ErrorCount);
    }

    [Fact]
    public void PolicyValidatorRejectsDuplicatePlanCheckIds()
    {
        var duplicateCheck = new PlanCheckDefinition("duplicate", "Duplicate", "dose-exists");
        var rulePack = CreateRulePack() with
        {
            PlanCheckCatalog = new PlanCheckCatalog(
                "Duplicate checks",
                "1",
                new[] { duplicateCheck, duplicateCheck },
                owner: "BeamKit")
        };

        var report = new RulePackPolicyValidator().Validate(rulePack);

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, issue => issue.Code == "plan-checks.duplicate-id" && issue.Severity == PolicyIssueSeverity.Error);
    }

    [Fact]
    public void PolicyValidatorRejectsNamingDictionaryAliasCollision()
    {
        var rulePack = CreateRulePack() with
        {
            NamingDictionary = new StructureNameDictionary(
                "Bad naming dictionary",
                new[] { "Lung_R", "Lung_L" },
                new[]
                {
                    new StructureNameAlias("Lung", "Lung_R"),
                    new StructureNameAlias("Lung", "Lung_L")
                },
                id: "bad.naming",
                version: "1")
        };

        var report = new RulePackPolicyValidator().Validate(rulePack);

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, issue => issue.Code == "naming.dictionary.alias-collision" && issue.Severity == PolicyIssueSeverity.Error);
    }

    [Fact]
    public void ClinicalPromotionPolicyRejectsMissingTraceability()
    {
        var report = new RulePackPolicyValidator(RulePackPolicyValidationOptions.ClinicalPromotion).Validate(CreateRulePack());

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, issue => issue.Code == "clinical-rules.requirement-id-missing");
        Assert.Contains(report.Issues, issue => issue.Code == "clinical-rules.hazard-links-missing");
        Assert.Contains(report.Issues, issue => issue.Code == "plan-checks.requirement-id-missing");
        Assert.Contains(report.Issues, issue => issue.Code == "plan-checks.control-links-missing");
    }

    [Fact]
    public void ClinicalPromotionPolicyAcceptsTraceableRulePack()
    {
        var report = new RulePackPolicyValidator(RulePackPolicyValidationOptions.ClinicalPromotion)
            .Validate(CreateTraceableRulePack());

        Assert.True(report.IsValid);
        Assert.Equal(0, report.ErrorCount);
    }

    [Fact]
    public void RulePackTestRunnerValidatesExpectedPassingAndFailingCases()
    {
        var report = new RulePackTestRunner(new FixedTimeProvider()).Run(
            CreateRulePack(),
            new[]
            {
                CreateTestCase(SyntheticClinicalCaseLibrary.HeadAndNeckBaseline()),
                CreateTestCase(SyntheticClinicalCaseLibrary.HeadAndNeckCordFailure())
            });

        Assert.True(report.Passed);
        Assert.Equal(2, report.PassedCount);
        Assert.Contains(report.Results, result => result.TestId == "head-neck-cord-fail" && result.ObservedFindingIds.Contains("cord.max"));
    }

    [Fact]
    public void RulePackTestRunnerReportsMissingExpectedFindings()
    {
        var report = new RulePackTestRunner(new FixedTimeProvider()).Run(
            CreateRulePack(),
            new[]
            {
                new RulePackTestCase(
                    "incorrect-expectation",
                    "A deliberately incorrect expectation.",
                    SyntheticClinicalCaseLibrary.HeadAndNeckBaseline().Plan,
                    BeamKitCheckStatus.Pass,
                    new[] { "cord.max" })
            });

        var result = Assert.Single(report.Results);
        Assert.False(report.Passed);
        Assert.False(result.Passed);
        Assert.Equal("cord.max", Assert.Single(result.MissingExpectedFindingIds));
    }

    [Fact]
    public void ProvenanceBuilderRecordsPlanAndRulePackFingerprints()
    {
        var plan = SyntheticClinicalCaseLibrary.HeadAndNeckBaseline().Plan;
        var rulePack = CreateRulePack();
        var checkReport = new BeamKitCheckEngine(new FixedTimeProvider()).Evaluate(new BeamKitCheckRequest(plan, rulePack));

        var provenance = new CheckRunProvenanceBuilder(new FixedTimeProvider()).Build(
            plan,
            rulePack,
            checkReport,
            inputSource: "case:head-neck-pass",
            branch: "main",
            commit: "abc123",
            buildId: "build-1");

        Assert.StartsWith("sha256:", provenance.PlanFingerprint, StringComparison.Ordinal);
        Assert.StartsWith("sha256:", provenance.PrescriptionFingerprint, StringComparison.Ordinal);
        Assert.StartsWith("sha256:", provenance.RulePackFingerprint, StringComparison.Ordinal);
        Assert.Equal("main", provenance.Branch);
        Assert.Equal("abc123", provenance.Commit);
        Assert.Equal("build-1", provenance.BuildId);
    }

    [Fact]
    public void CiRunnerReturnsZeroForPassingSyntheticCase()
    {
        var record = new BeamKitCiRunner(
            checkEngine: new BeamKitCheckEngine(new FixedTimeProvider()),
            provenanceBuilder: new CheckRunProvenanceBuilder(new FixedTimeProvider()))
            .Run(new BeamKitCiRunRequest(
                SyntheticClinicalCaseLibrary.HeadAndNeckBaseline().Plan,
                CreateRulePack(),
                inputSource: "case:head-neck-pass"));

        Assert.NotEqual(BeamKitCheckStatus.Fail, record.Status);
        Assert.Equal(0, record.ExitCode);
        Assert.True(record.PolicyValidation.IsValid);
    }

    [Fact]
    public void CiRunnerReturnsTwoForFailingSyntheticCase()
    {
        var record = new BeamKitCiRunner(
            checkEngine: new BeamKitCheckEngine(new FixedTimeProvider()),
            provenanceBuilder: new CheckRunProvenanceBuilder(new FixedTimeProvider()))
            .Run(new BeamKitCiRunRequest(
                SyntheticClinicalCaseLibrary.HeadAndNeckCordFailure().Plan,
                CreateRulePack(),
                inputSource: "case:head-neck-cord-fail"));

        Assert.Equal(BeamKitCheckStatus.Fail, record.Status);
        Assert.Equal(2, record.ExitCode);
        Assert.True(record.CheckReport.HasBlockingIssues);
    }

    private static RulePackTestCase CreateTestCase(SyntheticClinicalCase clinicalCase)
    {
        return new RulePackTestCase(
            clinicalCase.Id,
            clinicalCase.Description,
            clinicalCase.Plan,
            clinicalCase.ExpectedToPass ? BeamKitCheckStatus.Pass : BeamKitCheckStatus.Fail,
            clinicalCase.Id == "head-neck-cord-fail" ? new[] { "cord.max" } : Array.Empty<string>());
    }

    private static BeamKitRulePack CreateRulePack()
    {
        var query = new ClinicalRuleCatalogQuery
        {
            DiseaseSite = "Head and Neck",
            Institution = "Synthetic",
            Tags = new[] { "baseline" }
        };

        return new BeamKitRulePack(
            "Synthetic head-and-neck check pack",
            "2026.1",
            SyntheticClinicalRuleCatalogFactory.CreateHeadAndNeckCatalog().ToRuleSet(query),
            PlanCheckCatalog.CreateSyntheticBaseline(),
            SyntheticStructureNameDictionaryFactory.CreateTg263Subset(),
            MachineConstraintProfile.CreateSynthetic(),
            new RulePackReadinessDefaults
            {
                CtImported = true,
                OptimizationFinished = true,
                PhysicsQaComplete = true,
                PhysicianApprovalComplete = true,
                TreatmentReady = true
            },
            query,
            owner: "BeamKit",
            description: "Synthetic default rule pack for automation tests.",
            diseaseSite: "Head and Neck",
            tags: new[] { "synthetic", "head-neck", "test" });
    }

    private static BeamKitRulePack CreateTraceableRulePack()
    {
        var rule = new DoseMetricThresholdRule(
            "cord.max",
            "Spinal cord maximum dose must be no more than 45 Gy.",
            "SpinalCord",
            DoseMetricKeys.MaximumDoseGy,
            GoalComparison.LessThanOrEqual,
            45m,
            "Gy",
            EvaluationStatus.Fail,
            reference: "HN policy v1 section 4.2",
            rationale: "Serial-organ maximum dose constraint is reviewed before plan release.",
            requirementId: "REQ-HN-CORD-MAX",
            hazardIds: new[] { "HZ-FALSE-PASS", "HZ-WRONG-PROTOCOL" },
            controlIds: new[] { "CTRL-REQUIREMENT-TRACE", "CTRL-CLINICAL-REVIEW" });
        var planCheck = new PlanCheckDefinition(
            "dose.exists",
            "Dose exists",
            "dose-exists",
            description: "A plan cannot be promoted without calculated dose evidence.",
            reference: "HN policy v1 section 2.1",
            requirementId: "REQ-HN-DOSE-EXISTS",
            hazardIds: new[] { "HZ-STALE-SNAPSHOT" },
            controlIds: new[] { "CTRL-PROVENANCE" });

        return new BeamKitRulePack(
            "Traceable head-and-neck check pack",
            "2026.1",
            new PlanRuleSet("Traceable clinical rules", new[] { rule }),
            new PlanCheckCatalog("Traceable plan checks", "2026.1", new[] { planCheck }, owner: "Physics"),
            SyntheticStructureNameDictionaryFactory.CreateTg263Subset(),
            MachineConstraintProfile.CreateSynthetic(),
            new RulePackReadinessDefaults
            {
                CtImported = true,
                OptimizationFinished = true,
                PhysicsQaComplete = true,
                PhysicianApprovalComplete = true,
                TreatmentReady = true
            },
            owner: "Physics",
            description: "Synthetic traceable rule pack for clinical-promotion policy validation.",
            diseaseSite: "Head and Neck",
            tags: new[] { "synthetic", "traceable", "clinical-promotion" });
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);
        }
    }
}
