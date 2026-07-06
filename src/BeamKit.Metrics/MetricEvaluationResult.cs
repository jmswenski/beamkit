namespace BeamKit.Metrics;

/// <summary>
/// Result of evaluating a metric expression against a plan.
/// </summary>
public sealed record MetricEvaluationResult
{
    /// <summary>
    /// Creates a metric evaluation result.
    /// </summary>
    public MetricEvaluationResult(
        DvhMetricExpression expression,
        string? structureName,
        decimal? value,
        string? unit,
        bool isEvaluable,
        string message)
    {
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        StructureName = string.IsNullOrWhiteSpace(structureName) ? null : structureName.Trim();
        Value = value;
        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        IsEvaluable = isEvaluable;
        Message = MetricText.Required(message, nameof(message));
    }

    /// <summary>
    /// Evaluated expression.
    /// </summary>
    public DvhMetricExpression Expression { get; init; }

    /// <summary>
    /// Structure used for evaluation, when applicable.
    /// </summary>
    public string? StructureName { get; init; }

    /// <summary>
    /// Observed metric value.
    /// </summary>
    public decimal? Value { get; init; }

    /// <summary>
    /// Metric unit.
    /// </summary>
    public string? Unit { get; init; }

    /// <summary>
    /// Indicates whether the metric could be evaluated.
    /// </summary>
    public bool IsEvaluable { get; init; }

    /// <summary>
    /// Human-readable evaluation message.
    /// </summary>
    public string Message { get; init; }
}
