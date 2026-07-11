using BeamKit.Check;
using BeamKit.RulePacks;

namespace BeamKit.CiServer;

/// <summary>
/// Server-side review result for a draft rule pack before import or promotion.
/// </summary>
public sealed record CiServerRulePackDraftReviewResult
{
    /// <summary>
    /// Creates a draft review result.
    /// </summary>
    public CiServerRulePackDraftReviewResult(
        string rulePackId,
        string comparedToVersionId,
        RulePackValidationReport validation,
        RulePackTestReport? testReport,
        RulePackDiffReport diff)
    {
        RulePackId = CiServerText.Required(rulePackId, nameof(rulePackId));
        ComparedToVersionId = CiServerText.Required(comparedToVersionId, nameof(comparedToVersionId));
        Validation = validation ?? throw new ArgumentNullException(nameof(validation));
        TestReport = testReport;
        Diff = diff ?? throw new ArgumentNullException(nameof(diff));
    }

    /// <summary>
    /// Rule-pack id being reviewed.
    /// </summary>
    public string RulePackId { get; init; }

    /// <summary>
    /// Version id or source label used as the baseline comparison.
    /// </summary>
    public string ComparedToVersionId { get; init; }

    /// <summary>
    /// Draft validation report.
    /// </summary>
    public RulePackValidationReport Validation { get; init; }

    /// <summary>
    /// Optional draft regression-test report.
    /// </summary>
    public RulePackTestReport? TestReport { get; init; }

    /// <summary>
    /// Diff between the active/baseline rule pack and the draft.
    /// </summary>
    public RulePackDiffReport Diff { get; init; }

    /// <summary>
    /// Indicates whether the draft passed validation and any requested regression tests.
    /// </summary>
    public bool IsPromotable => Validation.IsValid && (TestReport is null || TestReport.Passed);
}
