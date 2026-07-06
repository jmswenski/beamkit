using BeamKit.Rules;

namespace BeamKit.Reporting;

/// <summary>
/// Portable report model for plan evaluation results.
/// </summary>
public sealed record PlanEvaluationReport
{
    /// <summary>
    /// Creates a plan evaluation report.
    /// </summary>
    public PlanEvaluationReport(
        string planId,
        string patientId,
        string ruleSetName,
        DateTimeOffset generatedAtUtc,
        IEnumerable<EvaluationResult> results)
    {
        PlanId = ReportText.Required(planId, nameof(planId));
        PatientId = ReportText.Required(patientId, nameof(patientId));
        RuleSetName = ReportText.Required(ruleSetName, nameof(ruleSetName));
        GeneratedAtUtc = generatedAtUtc;
        Results = results?.ToArray() ?? throw new ArgumentNullException(nameof(results));
        Summary = ReportSummary.FromResults(Results);
    }

    /// <summary>
    /// Identifier of the evaluated plan.
    /// </summary>
    public string PlanId { get; init; }

    /// <summary>
    /// Identifier of the patient associated with the plan.
    /// </summary>
    public string PatientId { get; init; }

    /// <summary>
    /// Name of the rule set used to evaluate the plan.
    /// </summary>
    public string RuleSetName { get; init; }

    /// <summary>
    /// UTC timestamp when the report was generated.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary>
    /// Ordered rule evaluation results.
    /// </summary>
    public IReadOnlyList<EvaluationResult> Results { get; init; }

    /// <summary>
    /// Aggregate counts by result status.
    /// </summary>
    public ReportSummary Summary { get; init; }
}
