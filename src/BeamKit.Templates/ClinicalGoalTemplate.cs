using BeamKit.Core.Domain;

namespace BeamKit.Templates;

/// <summary>
/// Serializable template definition for one clinical goal.
/// </summary>
public sealed record ClinicalGoalTemplate
{
    /// <summary>
    /// Creates a clinical goal template.
    /// </summary>
    public ClinicalGoalTemplate(
        string id,
        string structureName,
        string metricKey,
        GoalComparison comparison,
        decimal threshold,
        string unit,
        GoalSeverity severity = GoalSeverity.Required)
    {
        Id = TemplateText.Required(id, nameof(id));
        StructureName = TemplateText.Required(structureName, nameof(structureName));
        MetricKey = TemplateText.Required(metricKey, nameof(metricKey));
        Comparison = comparison;
        Threshold = threshold;
        Unit = TemplateText.Required(unit, nameof(unit));
        Severity = severity;
    }

    /// <summary>
    /// Stable goal identifier.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Target structure name or canonical identifier.
    /// </summary>
    public string StructureName { get; init; }

    /// <summary>
    /// Dose metric key, typically produced by <see cref="DoseMetricKeys"/>.
    /// </summary>
    public string MetricKey { get; init; }

    /// <summary>
    /// Comparison used for evaluation.
    /// </summary>
    public GoalComparison Comparison { get; init; }

    /// <summary>
    /// Threshold value.
    /// </summary>
    public decimal Threshold { get; init; }

    /// <summary>
    /// Unit for the threshold.
    /// </summary>
    public string Unit { get; init; }

    /// <summary>
    /// Severity used when the goal is not satisfied.
    /// </summary>
    public GoalSeverity Severity { get; init; }

    /// <summary>
    /// Converts the template to a core clinical goal.
    /// </summary>
    public ClinicalGoal ToClinicalGoal()
    {
        return new ClinicalGoal(Id, StructureName, MetricKey, Comparison, Threshold, Unit, Severity);
    }
}
