using BeamKit.Naming;
using BeamKit.Reporting;
using BeamKit.Rules;
using BeamKit.Workflow;

namespace BeamKit.Qa;

/// <summary>
/// Combined QA report containing naming, rule, and readiness results.
/// </summary>
public sealed record PlanQaReport
{
    /// <summary>
    /// Creates a combined QA report.
    /// </summary>
    public PlanQaReport(
        string planId,
        string patientId,
        DateTimeOffset generatedAtUtc,
        PlanEvaluationReport ruleReport,
        StructureNameNormalizationReport? namingReport = null,
        PlanReadinessState? readinessState = null)
    {
        PlanId = QaText.Required(planId, nameof(planId));
        PatientId = QaText.Required(patientId, nameof(patientId));
        GeneratedAtUtc = generatedAtUtc;
        RuleReport = ruleReport ?? throw new ArgumentNullException(nameof(ruleReport));
        NamingReport = namingReport;
        ReadinessState = readinessState;
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
    /// UTC timestamp when the report was generated.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary>
    /// Rule evaluation report.
    /// </summary>
    public PlanEvaluationReport RuleReport { get; init; }

    /// <summary>
    /// Optional structure-name normalization report.
    /// </summary>
    public StructureNameNormalizationReport? NamingReport { get; init; }

    /// <summary>
    /// Optional plan-readiness state.
    /// </summary>
    public PlanReadinessState? ReadinessState { get; init; }

    /// <summary>
    /// Indicates whether any QA gate did not pass.
    /// </summary>
    public bool HasBlockingIssues =>
        RuleReport.Results.Any(result => result.Status is EvaluationStatus.Fail or EvaluationStatus.NotEvaluable or EvaluationStatus.Error)
        || NamingReport?.AmbiguousCount > 0
        || NamingReport?.UnmappedCount > 0
        || NamingReport?.MissingStructures.Count > 0
        || ReadinessState?.IsReady == false;
}
