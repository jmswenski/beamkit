namespace BeamKit.Safety;

/// <summary>
/// Reviews validation evidence against BeamKit safety gate requirements.
/// </summary>
public sealed class SafetyEvidenceReviewer
{
    /// <summary>
    /// Reviews evidence for managed rule-pack promotion.
    /// </summary>
    public SafetyEvidenceReviewResult ReviewRulePackPromotion(
        ValidationEvidencePackage package,
        string rulePackId,
        string versionId,
        string fingerprint)
    {
        ArgumentNullException.ThrowIfNull(package);
        var findings = new List<SafetyEvidenceFinding>();

        AddMatchFinding(findings, "subject.type", "RulePack", package.SubjectType, "Evidence subject type must be RulePack.");
        AddMatchFinding(findings, "subject.id", rulePackId, package.SubjectId, "Evidence subject id must match the rule-pack id.");
        AddMatchFinding(findings, "subject.version", versionId, package.SubjectVersion, "Evidence subject version must match the managed version id.");
        AddMatchFinding(findings, "subject.fingerprint", fingerprint, package.SubjectFingerprint, "Evidence fingerprint must match the imported rule-pack fingerprint.");

        if (package.IntendedUse == ClinicalUseClassification.DeviceFunctionCandidate)
        {
            findings.Add(new SafetyEvidenceFinding(
                "intended-use.regulatory-review",
                ValidationEvidenceStatus.Fail,
                "Device-function candidate evidence requires an explicit regulatory strategy outside the CI promotion gate."));
        }

        AddRequiredEvidenceFinding(findings, package, ValidationEvidenceKind.RegressionTest, "A passing regression-test evidence item is required.");
        AddClinicalReviewFinding(findings, package);

        if (package.Checklist is null)
        {
            findings.Add(new SafetyEvidenceFinding(
                "checklist.required",
                ValidationEvidenceStatus.Fail,
                "A safety-control checklist is required."));
        }
        else if (!package.Checklist.IsComplete)
        {
            findings.Add(new SafetyEvidenceFinding(
                "checklist.incomplete",
                ValidationEvidenceStatus.Fail,
                $"Safety-control checklist has {package.Checklist.MissingRequiredControls.Count} unsatisfied required control(s)."));
        }
        else
        {
            findings.Add(new SafetyEvidenceFinding(
                "checklist.complete",
                ValidationEvidenceStatus.Pass,
                "Safety-control checklist is complete."));
        }

        if (package.HasFailedEvidence)
        {
            findings.Add(new SafetyEvidenceFinding(
                "evidence.failed",
                ValidationEvidenceStatus.Fail,
                "Evidence package contains failed evidence items."));
        }

        if (findings.Count == 0 || findings.All(finding => finding.Status != ValidationEvidenceStatus.Fail))
        {
            findings.Add(new SafetyEvidenceFinding(
                "evidence.acceptable",
                ValidationEvidenceStatus.Pass,
                "Evidence package satisfies the rule-pack promotion safety gate."));
        }

        return new SafetyEvidenceReviewResult(package, findings);
    }

    private static void AddMatchFinding(
        ICollection<SafetyEvidenceFinding> findings,
        string code,
        string expected,
        string actual,
        string failureMessage)
    {
        findings.Add(string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase)
            ? new SafetyEvidenceFinding(code, ValidationEvidenceStatus.Pass, $"{code} matches.")
            : new SafetyEvidenceFinding(code, ValidationEvidenceStatus.Fail, $"{failureMessage} Expected '{expected}', received '{actual}'."));
    }

    private static void AddRequiredEvidenceFinding(
        ICollection<SafetyEvidenceFinding> findings,
        ValidationEvidencePackage package,
        ValidationEvidenceKind kind,
        string failureMessage)
    {
        findings.Add(package.HasPassingEvidence(kind)
            ? new SafetyEvidenceFinding($"evidence.{kind}", ValidationEvidenceStatus.Pass, $"Passing {kind} evidence is present.")
            : new SafetyEvidenceFinding($"evidence.{kind}", ValidationEvidenceStatus.Fail, failureMessage));
    }

    private static void AddClinicalReviewFinding(
        ICollection<SafetyEvidenceFinding> findings,
        ValidationEvidencePackage package)
    {
        if (package.HasPassingEvidence(ValidationEvidenceKind.ClinicalReview)
            || package.HasPassingEvidence(ValidationEvidenceKind.Commissioning))
        {
            findings.Add(new SafetyEvidenceFinding(
                "evidence.clinical-review",
                ValidationEvidenceStatus.Pass,
                "Passing clinical review or commissioning evidence is present."));
            return;
        }

        findings.Add(new SafetyEvidenceFinding(
            "evidence.clinical-review",
            ValidationEvidenceStatus.Fail,
            "A passing clinical review or commissioning evidence item is required."));
    }
}
