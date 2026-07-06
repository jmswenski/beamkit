namespace BeamKit.Core.Domain;

/// <summary>
/// Numeric comparison used by a clinical goal or dose-metric rule.
/// </summary>
public enum GoalComparison
{
    /// <summary>
    /// Observed value must be less than the threshold.
    /// </summary>
    LessThan,

    /// <summary>
    /// Observed value must be less than or equal to the threshold.
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Observed value must be greater than the threshold.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Observed value must be greater than or equal to the threshold.
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Observed value must equal the threshold exactly.
    /// </summary>
    Equal
}

/// <summary>
/// Severity applied when a clinical goal is not satisfied.
/// </summary>
public enum GoalSeverity
{
    /// <summary>
    /// Goal failure should be reported as a non-blocking advisory.
    /// </summary>
    Advisory,

    /// <summary>
    /// Goal failure should be reported as a warning.
    /// </summary>
    Warning,

    /// <summary>
    /// Goal failure should be reported as a blocking failure.
    /// </summary>
    Required
}

/// <summary>
/// Defines a clinical dose or DVH goal for a structure.
/// </summary>
public sealed record ClinicalGoal
{
    /// <summary>
    /// Creates a clinical goal.
    /// </summary>
    public ClinicalGoal(
        string id,
        string structureName,
        string metricKey,
        GoalComparison comparison,
        decimal threshold,
        string unit,
        GoalSeverity severity = GoalSeverity.Required)
    {
        Id = Guard.Required(id, nameof(id));
        StructureName = Guard.Required(structureName, nameof(structureName));
        MetricKey = Guard.Required(metricKey, nameof(metricKey));
        Comparison = comparison;
        Threshold = threshold;
        Unit = Guard.Required(unit, nameof(unit));
        Severity = severity;
    }

    /// <summary>
    /// Stable goal identifier.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Structure name or identifier that the goal evaluates.
    /// </summary>
    public string StructureName { get; init; }

    /// <summary>
    /// Dose-statistics metric key, typically produced by <see cref="DoseMetricKeys"/>.
    /// </summary>
    public string MetricKey { get; init; }

    /// <summary>
    /// Comparison applied to the observed metric value.
    /// </summary>
    public GoalComparison Comparison { get; init; }

    /// <summary>
    /// Goal threshold in the stated <see cref="Unit"/>.
    /// </summary>
    public decimal Threshold { get; init; }

    /// <summary>
    /// Unit for the threshold and observed metric.
    /// </summary>
    public string Unit { get; init; }

    /// <summary>
    /// Severity used when the goal is not satisfied.
    /// </summary>
    public GoalSeverity Severity { get; init; }
}
