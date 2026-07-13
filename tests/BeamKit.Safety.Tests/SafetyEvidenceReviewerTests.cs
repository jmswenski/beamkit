using BeamKit.Safety;
using Xunit;

namespace BeamKit.Safety.Tests;

public sealed class SafetyEvidenceReviewerTests
{
    [Fact]
    public void ReviewRulePackPromotionAcceptsMatchingEvidencePackage()
    {
        var evidence = CreateEvidencePackage("institution-head-neck", "version-1", "sha256:abc");

        var result = new SafetyEvidenceReviewer().ReviewRulePackPromotion(
            evidence,
            "institution-head-neck",
            "version-1",
            "sha256:abc");

        Assert.True(result.IsAcceptable);
        Assert.Empty(result.BlockingFindings);
        Assert.Contains(result.Findings, finding => finding.Code == "evidence.acceptable");
    }

    [Fact]
    public void ReviewRulePackPromotionRejectsMismatchedFingerprint()
    {
        var evidence = CreateEvidencePackage("institution-head-neck", "version-1", "sha256:abc");

        var result = new SafetyEvidenceReviewer().ReviewRulePackPromotion(
            evidence,
            "institution-head-neck",
            "version-1",
            "sha256:def");

        Assert.False(result.IsAcceptable);
        Assert.Contains(result.BlockingFindings, finding => finding.Code == "subject.fingerprint");
    }

    [Fact]
    public void ReviewRulePackPromotionRequiresClinicalReviewEvidence()
    {
        var evidence = CreateEvidencePackage(
            "institution-head-neck",
            "version-1",
            "sha256:abc",
            includeClinicalReview: false);

        var result = new SafetyEvidenceReviewer().ReviewRulePackPromotion(
            evidence,
            "institution-head-neck",
            "version-1",
            "sha256:abc");

        Assert.False(result.IsAcceptable);
        Assert.Contains(result.BlockingFindings, finding => finding.Code == "evidence.clinical-review");
    }

    [Fact]
    public void ClinicalSafetyCaseRequiresControlledHazardsAndCompleteChecklist()
    {
        var checklist = new SafetyControlChecklist(
            "Deployment controls",
            "1",
            new[]
            {
                new SafetyControl(
                    "CTRL-REGRESSION",
                    "Regression suite",
                    "Known-good and known-bad cases passed.",
                    SafetyControlType.Verification,
                    isSatisfied: true)
            });
        var safetyCase = new ClinicalSafetyCase(
            "case-1",
            "Rule-pack deployment",
            ClinicalUseClassification.ClinicalDecisionSupport,
            new[]
            {
                new ClinicalHazard(
                    "HZ-FALSE-PASS",
                    "False pass",
                    "A non-compliant plan passes.",
                    "Clinical review may be delayed or misdirected.",
                    SafetySeverity.Major,
                    SafetyProbability.Occasional,
                    SafetyRiskLevel.Medium,
                    HazardStatus.Controlled,
                    controlIds: new[] { "CTRL-REGRESSION" })
            },
            checklist,
            new[] { CreateEvidencePackage("institution-head-neck", "version-1", "sha256:abc") });

        Assert.True(safetyCase.IsAcceptable);
        Assert.Empty(safetyCase.BlockingHazards);
    }

    [Fact]
    public void ClinicalSafetyRegistryStoreLoadsSampleRegistry()
    {
        var registry = ClinicalSafetyRegistryStore.FromFile(SampleRegistryPath());

        Assert.Equal("beamkit-foundation-safety", registry.Id);
        Assert.NotNull(registry.FindHazard("HZ-FALSE-PASS"));
        Assert.NotNull(registry.FindControl("CTRL-REQUIREMENT-TRACE"));
        Assert.Contains(registry.BlockingHazards, hazard => hazard.Id == "HZ-PHI-LEAK");
    }

    [Fact]
    public void ClinicalSafetyRegistryStoreRejectsDuplicateHazardIds()
    {
        var registry = new ClinicalSafetyRegistry(
            "registry",
            "Registry",
            "1",
            new[]
            {
                new ClinicalHazard(
                    "HZ-DUPLICATE",
                    "First",
                    "Situation",
                    "Harm",
                    SafetySeverity.Major,
                    SafetyProbability.Occasional,
                    SafetyRiskLevel.High),
                new ClinicalHazard(
                    "hz-duplicate",
                    "Second",
                    "Situation",
                    "Harm",
                    SafetySeverity.Major,
                    SafetyProbability.Occasional,
                    SafetyRiskLevel.High)
            },
            Array.Empty<SafetyControl>());

        var exception = Assert.Throws<InvalidOperationException>(() => ClinicalSafetyRegistryStore.ToJson(registry));

        Assert.Contains("Duplicate clinical hazard id", exception.Message);
    }

    private static ValidationEvidencePackage CreateEvidencePackage(
        string rulePackId,
        string versionId,
        string fingerprint,
        bool includeClinicalReview = true)
    {
        var evidence = new List<ValidationEvidenceItem>
        {
            new(
                "EV-REGRESSION",
                "Synthetic regression suite",
                ValidationEvidenceKind.RegressionTest,
                ValidationEvidenceStatus.Pass,
                new DateTimeOffset(2026, 7, 12, 0, 0, 0, TimeSpan.Zero),
                "dotnet test tests/BeamKit.CiServer.Tests/BeamKit.CiServer.Tests.csproj")
        };
        if (includeClinicalReview)
        {
            evidence.Add(new ValidationEvidenceItem(
                "EV-CLINICAL",
                "Clinical QA review",
                ValidationEvidenceKind.ClinicalReview,
                ValidationEvidenceStatus.Pass,
                new DateTimeOffset(2026, 7, 12, 0, 5, 0, TimeSpan.Zero),
                "Clinical QA signoff",
                reviewedBy: "Physics"));
        }

        return new ValidationEvidencePackage(
            $"evidence-{rulePackId}-{versionId}",
            "RulePack",
            rulePackId,
            versionId,
            fingerprint,
            new DateTimeOffset(2026, 7, 12, 0, 10, 0, TimeSpan.Zero),
            ClinicalUseClassification.ClinicalDecisionSupport,
            evidence,
            new SafetyControlChecklist(
                "Rule-pack promotion controls",
                "1",
                new[]
                {
                    new SafetyControl(
                        "CTRL-REGRESSION",
                        "Regression suite",
                        "Known-good and known-bad cases passed.",
                        SafetyControlType.Verification,
                        isSatisfied: true,
                        evidenceIds: new[] { "EV-REGRESSION" }),
                    new SafetyControl(
                        "CTRL-CLINICAL-REVIEW",
                        "Clinical review",
                        "Clinical or physics reviewer accepted the rule pack.",
                        SafetyControlType.Process,
                        isSatisfied: includeClinicalReview,
                        evidenceIds: includeClinicalReview ? new[] { "EV-CLINICAL" } : Array.Empty<string>())
                }),
            owner: "Physics",
            reviewer: "Clinical QA");
    }

    private static string SampleRegistryPath()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "samples",
            "clinical-safety",
            "hazards.json"));
    }
}
