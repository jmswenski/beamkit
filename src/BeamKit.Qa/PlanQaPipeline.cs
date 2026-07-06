using BeamKit.Naming;
using BeamKit.Reporting;
using BeamKit.Rules;
using BeamKit.Workflow;

namespace BeamKit.Qa;

/// <summary>
/// Runs BeamKit's combined plan QA workflow.
/// </summary>
public sealed class PlanQaPipeline
{
    private readonly RuleEngine ruleEngine;
    private readonly ReportBuilder reportBuilder;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Creates a QA pipeline.
    /// </summary>
    public PlanQaPipeline(RuleEngine? ruleEngine = null, ReportBuilder? reportBuilder = null, TimeProvider? timeProvider = null)
    {
        this.ruleEngine = ruleEngine ?? new RuleEngine();
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.reportBuilder = reportBuilder ?? new ReportBuilder(this.timeProvider);
    }

    /// <summary>
    /// Evaluates naming, rules, and readiness based on the supplied request.
    /// </summary>
    public PlanQaReport Evaluate(PlanQaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ruleResults = ruleEngine.Evaluate(request.Plan, request.RuleSet);
        var ruleReport = reportBuilder.Build(request.Plan, request.RuleSet, ruleResults);
        var namingReport = request.NamingDictionary is null
            ? null
            : new StructureNameNormalizer(request.NamingDictionary).NormalizePlan(request.Plan);
        var readinessState = request.ReadinessInput is null
            ? null
            : new PlanReadinessEvaluator().Evaluate(request.ReadinessInput);

        return new PlanQaReport(
            request.Plan.Id,
            request.Plan.Patient.Id,
            timeProvider.GetUtcNow(),
            ruleReport,
            namingReport,
            readinessState);
    }
}
