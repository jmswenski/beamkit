using BeamKit.Metrics;
using BeamKit.Naming;
using BeamKit.PlanCheck;
using BeamKit.Release;
using BeamKit.Reporting;
using BeamKit.Rules;
using BeamKit.Workflow;

namespace BeamKit.Check;

/// <summary>
/// Complete report for the flagship BeamKit Check workflow.
/// </summary>
public sealed record BeamKitCheckReport
{
    /// <summary>
    /// Creates a check report.
    /// </summary>
    public BeamKitCheckReport(
        string planId,
        string patientId,
        string courseId,
        DateTimeOffset generatedAtUtc,
        string rulePackName,
        string rulePackVersion,
        PlanCheckReport planCheckReport,
        PlanEvaluationReport clinicalGoalReport,
        StructureNameNormalizationReport? namingReport,
        PlanReadinessState readinessState,
        PlanQualityMetrics? targetMetrics,
        WriteUpManifest? writeUpManifest = null,
        string? diseaseSite = null,
        string? inputSource = null,
        string? metricSummaryMessage = null)
    {
        PlanId = CheckText.Required(planId, nameof(planId));
        PatientId = CheckText.Required(patientId, nameof(patientId));
        CourseId = CheckText.Required(courseId, nameof(courseId));
        GeneratedAtUtc = generatedAtUtc;
        RulePackName = CheckText.Required(rulePackName, nameof(rulePackName));
        RulePackVersion = CheckText.Required(rulePackVersion, nameof(rulePackVersion));
        PlanCheckReport = planCheckReport ?? throw new ArgumentNullException(nameof(planCheckReport));
        ClinicalGoalReport = clinicalGoalReport ?? throw new ArgumentNullException(nameof(clinicalGoalReport));
        NamingReport = namingReport;
        ReadinessState = readinessState ?? throw new ArgumentNullException(nameof(readinessState));
        TargetMetrics = targetMetrics;
        WriteUpManifest = writeUpManifest;
        DiseaseSite = CheckText.Optional(diseaseSite);
        InputSource = CheckText.Optional(inputSource);
        MetricSummaryMessage = CheckText.Optional(metricSummaryMessage);
    }

    /// <summary>
    /// Evaluated plan id.
    /// </summary>
    public string PlanId { get; init; }

    /// <summary>
    /// Evaluated patient id.
    /// </summary>
    public string PatientId { get; init; }

    /// <summary>
    /// Course id containing the plan.
    /// </summary>
    public string CourseId { get; init; }

    /// <summary>
    /// Optional disease-site label.
    /// </summary>
    public string? DiseaseSite { get; init; }

    /// <summary>
    /// Optional input source label.
    /// </summary>
    public string? InputSource { get; init; }

    /// <summary>
    /// UTC timestamp when the report was generated.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary>
    /// Rule-pack name.
    /// </summary>
    public string RulePackName { get; init; }

    /// <summary>
    /// Rule-pack version.
    /// </summary>
    public string RulePackVersion { get; init; }

    /// <summary>
    /// Configurable plan-check results.
    /// </summary>
    public PlanCheckReport PlanCheckReport { get; init; }

    /// <summary>
    /// Clinical goal evaluation results.
    /// </summary>
    public PlanEvaluationReport ClinicalGoalReport { get; init; }

    /// <summary>
    /// Optional structure-name normalization report.
    /// </summary>
    public StructureNameNormalizationReport? NamingReport { get; init; }

    /// <summary>
    /// Plan-readiness state.
    /// </summary>
    public PlanReadinessState ReadinessState { get; init; }

    /// <summary>
    /// Optional target plan-quality metric summary.
    /// </summary>
    public PlanQualityMetrics? TargetMetrics { get; init; }

    /// <summary>
    /// Optional message explaining why target metrics were not available.
    /// </summary>
    public string? MetricSummaryMessage { get; init; }

    /// <summary>
    /// Optional write-up manifest captured during the check run.
    /// </summary>
    public WriteUpManifest? WriteUpManifest { get; init; }

    /// <summary>
    /// Top-level status suitable for CI/CD gates.
    /// </summary>
    public BeamKitCheckStatus Status =>
        HasBlockingIssues ? BeamKitCheckStatus.Fail :
        HasWarnings ? BeamKitCheckStatus.Warning :
        BeamKitCheckStatus.Pass;

    /// <summary>
    /// Indicates whether the check produced any blocking issue.
    /// </summary>
    public bool HasBlockingIssues =>
        PlanCheckReport.HasBlockingIssues
        || ClinicalGoalReport.Results.Any(result => result.Status is EvaluationStatus.Fail or EvaluationStatus.NotEvaluable or EvaluationStatus.Error)
        || NamingReport?.AmbiguousCount > 0
        || NamingReport?.UnmappedCount > 0
        || NamingReport?.MissingStructures.Count > 0
        || ReadinessState.IsReady == false
        || WriteUpManifest?.HasOutstandingChecklistItems == true;

    /// <summary>
    /// Indicates whether the check has non-blocking warnings.
    /// </summary>
    public bool HasWarnings =>
        PlanCheckReport.WarningCount > 0
        || ClinicalGoalReport.Summary.WarningCount > 0;

    /// <summary>
    /// Total count of blocking items across all check sections.
    /// </summary>
    public int BlockingIssueCount =>
        PlanCheckReport.FailCount
        + PlanCheckReport.NotEvaluableCount
        + ClinicalGoalReport.Summary.FailCount
        + ClinicalGoalReport.Summary.NotEvaluableCount
        + ClinicalGoalReport.Summary.ErrorCount
        + (NamingReport?.AmbiguousCount ?? 0)
        + (NamingReport?.UnmappedCount ?? 0)
        + (NamingReport?.MissingStructures.Count ?? 0)
        + ReadinessState.OutstandingItems.Count
        + (WriteUpManifest?.OutstandingChecklistItems.Count ?? 0);
}
