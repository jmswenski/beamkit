using BeamKit.Core.Domain;

namespace BeamKit.Rules;

/// <summary>
/// Evaluates a numeric dose metric for one structure against a threshold.
/// </summary>
public class DoseMetricThresholdRule : IPlanRule
{
    /// <summary>
    /// Creates a dose-metric threshold rule.
    /// </summary>
    public DoseMetricThresholdRule(
        string id,
        string description,
        string structureName,
        string metricKey,
        GoalComparison comparison,
        decimal threshold,
        string unit,
        EvaluationStatus failureStatus = EvaluationStatus.Fail)
    {
        Id = RuleText.Required(id, nameof(id));
        Description = RuleText.Required(description, nameof(description));
        StructureName = RuleText.Required(structureName, nameof(structureName));
        MetricKey = RuleText.Required(metricKey, nameof(metricKey));
        Comparison = comparison;
        Threshold = threshold;
        Unit = RuleText.Required(unit, nameof(unit));
        FailureStatus = failureStatus;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Description { get; }

    /// <summary>
    /// Name or identifier of the structure whose metric is evaluated.
    /// </summary>
    public string StructureName { get; }

    /// <summary>
    /// Dose-statistics metric key.
    /// </summary>
    public string MetricKey { get; }

    /// <summary>
    /// Comparison applied to the observed value.
    /// </summary>
    public GoalComparison Comparison { get; }

    /// <summary>
    /// Expected threshold value.
    /// </summary>
    public decimal Threshold { get; }

    /// <summary>
    /// Unit for observed and expected values.
    /// </summary>
    public string Unit { get; }

    /// <summary>
    /// Status returned when the observed value does not satisfy the threshold.
    /// </summary>
    public EvaluationStatus FailureStatus { get; }

    /// <inheritdoc />
    public EvaluationResult Evaluate(PlanEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var structure = context.Plan.FindStructure(StructureName);
        if (structure is null)
        {
            return EvaluationResult.NotEvaluable(
                Id,
                Description,
                $"Structure '{StructureName}' was not found.",
                structureName: StructureName);
        }

        var statistics = context.Plan.FindDoseStatistics(structure.Id);
        if (statistics is null)
        {
            return EvaluationResult.NotEvaluable(
                Id,
                Description,
                $"Dose statistics were not found for '{structure.Name}'.",
                structureName: structure.Name);
        }

        var observedValue = statistics.GetMetric(MetricKey);
        if (observedValue is null)
        {
            return EvaluationResult.NotEvaluable(
                Id,
                Description,
                $"Metric '{MetricKey}' was not found for '{structure.Name}'.",
                structureName: structure.Name);
        }

        var passed = GoalComparisonEvaluator.IsSatisfied(observedValue.Value, Comparison, Threshold);
        var status = passed ? EvaluationStatus.Pass : FailureStatus;
        var message = passed
            ? $"{structure.Name} {MetricKey} met the threshold."
            : $"{structure.Name} {MetricKey} did not meet the threshold.";

        return new EvaluationResult(
            Id,
            Description,
            status,
            message,
            observedValue.Value,
            Threshold,
            Unit,
            structure.Name);
    }
}
