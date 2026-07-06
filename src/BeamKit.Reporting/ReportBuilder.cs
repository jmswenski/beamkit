using BeamKit.Core.Domain;
using BeamKit.Rules;

namespace BeamKit.Reporting;

/// <summary>
/// Creates report models from plans, rule sets, and evaluation results.
/// </summary>
public sealed class ReportBuilder
{
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Creates a report builder.
    /// </summary>
    public ReportBuilder(TimeProvider? timeProvider = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Builds a plan evaluation report.
    /// </summary>
    public PlanEvaluationReport Build(Plan plan, PlanRuleSet ruleSet, IEnumerable<EvaluationResult> results)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(ruleSet);
        ArgumentNullException.ThrowIfNull(results);

        return new PlanEvaluationReport(
            plan.Id,
            plan.Patient.Id,
            ruleSet.Name,
            timeProvider.GetUtcNow(),
            results);
    }
}
