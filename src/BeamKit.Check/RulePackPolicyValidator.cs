using BeamKit.PlanCheck;
using BeamKit.Naming;
using BeamKit.Rules;

namespace BeamKit.Check;

/// <summary>
/// Validates rule packs as clinical policy-as-code before they are promoted.
/// </summary>
public sealed class RulePackPolicyValidator
{
    private readonly RulePackPolicyValidationOptions options;

    /// <summary>
    /// Creates a rule-pack policy validator.
    /// </summary>
    public RulePackPolicyValidator(RulePackPolicyValidationOptions? options = null)
    {
        this.options = options ?? RulePackPolicyValidationOptions.Default;
    }

    /// <summary>
    /// Validates a loaded rule pack.
    /// </summary>
    public RulePackValidationReport Validate(BeamKitRulePack rulePack)
    {
        ArgumentNullException.ThrowIfNull(rulePack);

        var issues = new List<RulePackPolicyIssue>();
        CheckMetadata(rulePack, issues);
        CheckClinicalRules(rulePack, issues);
        CheckPlanChecks(rulePack, issues);
        CheckSupportCatalogs(rulePack, issues);
        return new RulePackValidationReport(rulePack.Name, rulePack.Version, RulePackFingerprint.Compute(rulePack), issues);
    }

    private void CheckMetadata(BeamKitRulePack rulePack, List<RulePackPolicyIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(rulePack.Owner))
        {
            Add(issues, "rule-pack.owner-missing", RequiredOrWarning(options.RequireRulePackOwner), "Rule pack should declare an owner.", rulePack.Name);
        }

        if (string.IsNullOrWhiteSpace(rulePack.Description))
        {
            Add(issues, "rule-pack.description-missing", RequiredOrInfo(options.RequireRulePackDescription), "Rule pack should include a human-readable description.", rulePack.Name);
        }

        if (rulePack.Tags.Count == 0)
        {
            Add(issues, "rule-pack.tags-missing", RequiredOrInfo(options.RequireRulePackTags), "Rule pack should include searchable tags.", rulePack.Name);
        }
    }

    private void CheckClinicalRules(BeamKitRulePack rulePack, List<RulePackPolicyIssue> issues)
    {
        if (rulePack.ClinicalRuleSet.Rules.Count == 0)
        {
            Add(issues, "clinical-rules.empty", PolicyIssueSeverity.Error, "Rule pack has no executable clinical rules.", rulePack.ClinicalRuleSet.Name);
            return;
        }

        AddDuplicateIssues(
            issues,
            rulePack.ClinicalRuleSet.Rules.Select(rule => rule.Id),
            "clinical-rules.duplicate-id",
            "Duplicate clinical rule id");

        foreach (var rule in rulePack.ClinicalRuleSet.Rules.Where(rule => string.IsNullOrWhiteSpace(rule.Description)))
        {
            Add(issues, "clinical-rules.description-missing", RequiredOrWarning(options.RequireClinicalRuleDescription), "Clinical rule should include a description.", rule.Id);
        }

        foreach (var rule in rulePack.ClinicalRuleSet.Rules)
        {
            CheckClinicalRuleTraceability(rule, issues);
        }
    }

    private void CheckPlanChecks(BeamKitRulePack rulePack, List<RulePackPolicyIssue> issues)
    {
        AddDuplicateIssues(
            issues,
            rulePack.PlanCheckCatalog.Checks.Select(check => check.Id),
            "plan-checks.duplicate-id",
            "Duplicate plan-check id");

        if (string.IsNullOrWhiteSpace(rulePack.PlanCheckCatalog.Owner))
        {
            Add(issues, "plan-checks.owner-missing", RequiredOrWarning(options.RequirePlanCheckOwner), "Plan-check catalog should declare an owner.", rulePack.PlanCheckCatalog.Name);
        }

        foreach (var check in rulePack.PlanCheckCatalog.Checks.Where(check => check.IsActive && string.IsNullOrWhiteSpace(check.Reference)))
        {
            Add(issues, "plan-checks.reference-missing", RequiredOrInfo(options.RequirePlanCheckReference), "Plan check should include a policy, protocol, or owner reference.", check.Id);
        }

        foreach (var check in rulePack.PlanCheckCatalog.Checks.Where(check => check.IsActive))
        {
            CheckPlanCheckTraceability(check, issues);
        }
    }

    private void CheckSupportCatalogs(BeamKitRulePack rulePack, List<RulePackPolicyIssue> issues)
    {
        if (rulePack.NamingDictionary is null)
        {
            Add(issues, "naming.dictionary-missing", RequiredOrWarning(options.RequireNamingDictionary), "Rule pack does not include a naming dictionary.", rulePack.Name);
        }

        if (rulePack.MachineProfile is null)
        {
            Add(issues, "machine-profile.missing", RequiredOrWarning(options.RequireMachineProfile), "Rule pack does not include a machine profile.", rulePack.Name);
        }

        if (rulePack.NamingDictionary is not null && rulePack.NamingDictionary.RequiredStructureNames.Count == 0)
        {
            Add(issues, "naming.required-structures-missing", RequiredOrWarning(options.RequireRequiredStructures), "Naming dictionary should define required structures for this rule pack.", rulePack.NamingDictionary.Name);
        }

        if (rulePack.NamingDictionary is not null)
        {
            var review = new StructureNameDictionaryReviewer().Review(rulePack.NamingDictionary);
            foreach (var finding in review.Findings.Where(finding => finding.Severity == StructureNameDictionaryReviewSeverity.Error))
            {
                Add(
                    issues,
                    $"naming.{finding.Code}",
                    PolicyIssueSeverity.Error,
                    finding.Message,
                    finding.Subject ?? rulePack.NamingDictionary.Name);
            }
        }
    }

    private void CheckClinicalRuleTraceability(IPlanRule rule, List<RulePackPolicyIssue> issues)
    {
        if (rule is not ITraceablePlanRule traceable)
        {
            AddIfRequired(
                issues,
                options.RequireClinicalRuleReference
                    || options.RequireClinicalRuleRationale
                    || options.RequireClinicalRuleRequirementId
                    || options.RequireClinicalRuleHazardLinks
                    || options.RequireClinicalRuleControlLinks,
                "clinical-rules.traceability-unavailable",
                "Clinical rule does not expose traceability metadata.",
                rule.Id);
            return;
        }

        AddMissingIfRequired(issues, options.RequireClinicalRuleReference, traceable.Reference, "clinical-rules.reference-missing", "Clinical rule should cite a policy, protocol, or source document.", rule.Id);
        AddMissingIfRequired(issues, options.RequireClinicalRuleRationale, traceable.Rationale, "clinical-rules.rationale-missing", "Clinical rule should include a rationale.", rule.Id);
        AddMissingIfRequired(issues, options.RequireClinicalRuleRequirementId, traceable.RequirementId, "clinical-rules.requirement-id-missing", "Clinical rule should link to a stable requirement id.", rule.Id);
        AddEmptyIfRequired(issues, options.RequireClinicalRuleHazardLinks, traceable.HazardIds, "clinical-rules.hazard-links-missing", "Clinical rule should link to at least one hazard id.", rule.Id);
        AddEmptyIfRequired(issues, options.RequireClinicalRuleControlLinks, traceable.ControlIds, "clinical-rules.control-links-missing", "Clinical rule should link to at least one safety-control id.", rule.Id);
    }

    private void CheckPlanCheckTraceability(PlanCheckDefinition check, List<RulePackPolicyIssue> issues)
    {
        AddMissingIfRequired(issues, options.RequirePlanCheckRequirementId, check.RequirementId, "plan-checks.requirement-id-missing", "Plan check should link to a stable requirement id.", check.Id);
        AddEmptyIfRequired(issues, options.RequirePlanCheckHazardLinks, check.HazardIds, "plan-checks.hazard-links-missing", "Plan check should link to at least one hazard id.", check.Id);
        AddEmptyIfRequired(issues, options.RequirePlanCheckControlLinks, check.ControlIds, "plan-checks.control-links-missing", "Plan check should link to at least one safety-control id.", check.Id);
    }

    private static void AddDuplicateIssues(List<RulePackPolicyIssue> issues, IEnumerable<string> values, string code, string message)
    {
        foreach (var duplicate in values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key))
        {
            Add(issues, code, PolicyIssueSeverity.Error, $"{message}: '{duplicate}'.", duplicate);
        }
    }

    private static void Add(List<RulePackPolicyIssue> issues, string code, PolicyIssueSeverity severity, string message, string? subject)
    {
        issues.Add(new RulePackPolicyIssue(code, severity, message, subject));
    }

    private static void AddIfRequired(List<RulePackPolicyIssue> issues, bool required, string code, string message, string? subject)
    {
        if (required)
        {
            Add(issues, code, PolicyIssueSeverity.Error, message, subject);
        }
    }

    private static void AddMissingIfRequired(List<RulePackPolicyIssue> issues, bool required, string? value, string code, string message, string? subject)
    {
        if (required && string.IsNullOrWhiteSpace(value))
        {
            Add(issues, code, PolicyIssueSeverity.Error, message, subject);
        }
    }

    private static void AddEmptyIfRequired(List<RulePackPolicyIssue> issues, bool required, IReadOnlyList<string> values, string code, string message, string? subject)
    {
        if (required && values.Count == 0)
        {
            Add(issues, code, PolicyIssueSeverity.Error, message, subject);
        }
    }

    private static PolicyIssueSeverity RequiredOrWarning(bool required)
    {
        return required ? PolicyIssueSeverity.Error : PolicyIssueSeverity.Warning;
    }

    private static PolicyIssueSeverity RequiredOrInfo(bool required)
    {
        return required ? PolicyIssueSeverity.Error : PolicyIssueSeverity.Info;
    }
}
