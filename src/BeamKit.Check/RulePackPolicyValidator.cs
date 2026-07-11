namespace BeamKit.Check;

/// <summary>
/// Validates rule packs as clinical policy-as-code before they are promoted.
/// </summary>
public sealed class RulePackPolicyValidator
{
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

    private static void CheckMetadata(BeamKitRulePack rulePack, List<RulePackPolicyIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(rulePack.Owner))
        {
            Add(issues, "rule-pack.owner-missing", PolicyIssueSeverity.Warning, "Rule pack should declare an owner.", rulePack.Name);
        }

        if (string.IsNullOrWhiteSpace(rulePack.Description))
        {
            Add(issues, "rule-pack.description-missing", PolicyIssueSeverity.Info, "Rule pack should include a human-readable description.", rulePack.Name);
        }

        if (rulePack.Tags.Count == 0)
        {
            Add(issues, "rule-pack.tags-missing", PolicyIssueSeverity.Info, "Rule pack should include searchable tags.", rulePack.Name);
        }
    }

    private static void CheckClinicalRules(BeamKitRulePack rulePack, List<RulePackPolicyIssue> issues)
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
            Add(issues, "clinical-rules.description-missing", PolicyIssueSeverity.Warning, "Clinical rule should include a description.", rule.Id);
        }
    }

    private static void CheckPlanChecks(BeamKitRulePack rulePack, List<RulePackPolicyIssue> issues)
    {
        AddDuplicateIssues(
            issues,
            rulePack.PlanCheckCatalog.Checks.Select(check => check.Id),
            "plan-checks.duplicate-id",
            "Duplicate plan-check id");

        if (string.IsNullOrWhiteSpace(rulePack.PlanCheckCatalog.Owner))
        {
            Add(issues, "plan-checks.owner-missing", PolicyIssueSeverity.Warning, "Plan-check catalog should declare an owner.", rulePack.PlanCheckCatalog.Name);
        }

        foreach (var check in rulePack.PlanCheckCatalog.Checks.Where(check => string.IsNullOrWhiteSpace(check.Reference)))
        {
            Add(issues, "plan-checks.reference-missing", PolicyIssueSeverity.Info, "Plan check should include a policy, protocol, or owner reference.", check.Id);
        }
    }

    private static void CheckSupportCatalogs(BeamKitRulePack rulePack, List<RulePackPolicyIssue> issues)
    {
        if (rulePack.NamingDictionary is null)
        {
            Add(issues, "naming.dictionary-missing", PolicyIssueSeverity.Warning, "Rule pack does not include a naming dictionary.", rulePack.Name);
        }

        if (rulePack.MachineProfile is null)
        {
            Add(issues, "machine-profile.missing", PolicyIssueSeverity.Warning, "Rule pack does not include a machine profile.", rulePack.Name);
        }

        if (rulePack.NamingDictionary is not null && rulePack.NamingDictionary.RequiredStructureNames.Count == 0)
        {
            Add(issues, "naming.required-structures-missing", PolicyIssueSeverity.Warning, "Naming dictionary should define required structures for this rule pack.", rulePack.NamingDictionary.Name);
        }
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
}
