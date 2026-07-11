using System.Text.Json;
using BeamKit.Check;

namespace BeamKit.RulePacks;

/// <summary>
/// Performs authoring and governance checks for a rule-pack manifest.
/// </summary>
public sealed class RulePackDoctor
{
    private readonly TimeProvider timeProvider;
    private readonly RulePackPolicyValidator policyValidator;

    /// <summary>
    /// Creates a rule-pack doctor.
    /// </summary>
    public RulePackDoctor(TimeProvider? timeProvider = null, RulePackPolicyValidator? policyValidator = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.policyValidator = policyValidator ?? new RulePackPolicyValidator();
    }

    /// <summary>
    /// Inspects a rule-pack manifest file.
    /// </summary>
    public RulePackDoctorReport InspectFile(string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            throw new ArgumentException("Manifest path is required.", nameof(manifestPath));
        }

        var fullPath = Path.GetFullPath(manifestPath);
        var manifest = RulePackManifestStore.FromFile(fullPath);
        var issues = new List<RulePackDoctorIssue>();
        CheckReferences(fullPath, manifest, issues);
        CheckApproval(manifest, issues);

        BeamKitRulePack rulePack;
        RulePackValidationReport validation;
        try
        {
            rulePack = BeamKitRulePackLoader.FromFile(fullPath);
            validation = policyValidator.Validate(rulePack);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or JsonException)
        {
            var validationIssues = new[]
            {
                new RulePackPolicyIssue(
                    "rule-pack.load-failed",
                    PolicyIssueSeverity.Error,
                    "Rule pack could not be loaded for policy validation.",
                    ex.Message)
            };
            validation = new RulePackValidationReport(
                manifest.Name,
                manifest.Version,
                "unavailable",
                validationIssues);
            return new RulePackDoctorReport(
                fullPath,
                manifest.Name,
                manifest.Version,
                validation.Fingerprint,
                validation,
                issues);
        }

        return new RulePackDoctorReport(
            fullPath,
            rulePack.Name,
            rulePack.Version,
            validation.Fingerprint,
            validation,
            issues);
    }

    private static void CheckReferences(string manifestPath, RulePackManifest manifest, List<RulePackDoctorIssue> issues)
    {
        var baseDirectory = Path.GetDirectoryName(manifestPath) ?? Directory.GetCurrentDirectory();
        CheckRequiredReference(issues, baseDirectory, manifest.ClinicalRuleCatalog, "clinicalRuleCatalog");
        CheckRequiredReference(issues, baseDirectory, manifest.PlanCheckCatalog, "planCheckCatalog");
        CheckOptionalReference(issues, baseDirectory, manifest.NamingDictionary, "namingDictionary");
        CheckOptionalReference(issues, baseDirectory, manifest.MachineProfile, "machineProfile");
    }

    private static void CheckRequiredReference(List<RulePackDoctorIssue> issues, string baseDirectory, string path, string property)
    {
        if (!File.Exists(Path.GetFullPath(Path.Combine(baseDirectory, path))))
        {
            issues.Add(new RulePackDoctorIssue(
                $"manifest.{property}.missing-file",
                RulePackDoctorIssueSeverity.Error,
                $"Manifest reference '{property}' points to a file that does not exist.",
                path));
        }
    }

    private static void CheckOptionalReference(List<RulePackDoctorIssue> issues, string baseDirectory, string? path, string property)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            issues.Add(new RulePackDoctorIssue(
                $"manifest.{property}.missing",
                RulePackDoctorIssueSeverity.Warning,
                $"Manifest should include '{property}' for production rule packs."));
            return;
        }

        if (!File.Exists(Path.GetFullPath(Path.Combine(baseDirectory, path))))
        {
            issues.Add(new RulePackDoctorIssue(
                $"manifest.{property}.missing-file",
                RulePackDoctorIssueSeverity.Error,
                $"Manifest reference '{property}' points to a file that does not exist.",
                path));
        }
    }

    private void CheckApproval(RulePackManifest manifest, List<RulePackDoctorIssue> issues)
    {
        if (manifest.Approval is null)
        {
            issues.Add(new RulePackDoctorIssue(
                "approval.missing",
                RulePackDoctorIssueSeverity.Warning,
                "Production rule packs should include approval metadata."));
            return;
        }

        AddMissing(issues, manifest.Approval.Status, "approval.status", "Approval status should be declared.");
        AddMissing(issues, manifest.Approval.Institution, "approval.institution", "Owning institution should be declared.");
        AddMissing(issues, manifest.Approval.ReviewedBy, "approval.reviewed-by", "Reviewer should be declared.");
        AddMissing(issues, manifest.Approval.ApprovedBy, "approval.approved-by", "Approver should be declared before promotion.");
        AddMissing(issues, manifest.Approval.Reference, "approval.reference", "Approval should cite a source policy, meeting note, or request.");
        AddMissing(issues, manifest.Approval.Rationale, "approval.rationale", "Approval should include rationale.");

        if (manifest.Approval.EffectiveDate is null)
        {
            issues.Add(new RulePackDoctorIssue(
                "approval.effective-date-missing",
                RulePackDoctorIssueSeverity.Warning,
                "Approval should include an effective date."));
        }

        if (manifest.Approval.ReviewDueDate is null)
        {
            issues.Add(new RulePackDoctorIssue(
                "approval.review-due-date-missing",
                RulePackDoctorIssueSeverity.Warning,
                "Approval should include a review due date."));
        }
        else if (manifest.Approval.ReviewDueDate < DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
        {
            issues.Add(new RulePackDoctorIssue(
                "approval.review-overdue",
                RulePackDoctorIssueSeverity.Warning,
                "Approval review due date has passed.",
                manifest.Approval.ReviewDueDate.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)));
        }
    }

    private static void AddMissing(List<RulePackDoctorIssue> issues, string? value, string code, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(new RulePackDoctorIssue(code, RulePackDoctorIssueSeverity.Warning, message));
        }
    }
}
