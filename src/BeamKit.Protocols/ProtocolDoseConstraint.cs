using BeamKit.Core.Domain;

namespace BeamKit.Protocols;

/// <summary>
/// Computable dose or DVH constraint from an RT-PX package.
/// </summary>
public sealed record ProtocolDoseConstraint
{
    /// <summary>
    /// Creates an empty dose constraint for JSON deserialization.
    /// </summary>
    public ProtocolDoseConstraint()
    {
        Id = string.Empty;
        Structure = string.Empty;
        Metric = string.Empty;
        Unit = string.Empty;
        IsActive = true;
    }

    /// <summary>
    /// Creates a dose constraint.
    /// </summary>
    public ProtocolDoseConstraint(
        string id,
        string structure,
        string metric,
        GoalComparison comparison,
        decimal value,
        string unit,
        ProtocolRequirementLevel level = ProtocolRequirementLevel.Required,
        string? description = null,
        ProtocolSourceReference? source = null,
        bool isActive = true)
    {
        Id = ProtocolText.Required(id, nameof(id));
        Structure = ProtocolText.Required(structure, nameof(structure));
        Metric = ProtocolText.Required(metric, nameof(metric));
        Comparison = comparison;
        Value = value;
        Unit = ProtocolText.Required(unit, nameof(unit));
        Level = level;
        Description = ProtocolText.Optional(description);
        Source = source;
        IsActive = isActive;
    }

    /// <summary>
    /// Stable constraint id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Structure name or `$target`.
    /// </summary>
    public string Structure { get; init; }

    /// <summary>
    /// DVH metric expression such as Max, Mean, D95%, V20Gy, CI, HI, GI, or R50.
    /// </summary>
    public string Metric { get; init; }

    /// <summary>
    /// Comparison applied to the observed metric.
    /// </summary>
    public GoalComparison Comparison { get; init; }

    /// <summary>
    /// Threshold value.
    /// </summary>
    public decimal Value { get; init; }

    /// <summary>
    /// Threshold unit.
    /// </summary>
    public string Unit { get; init; }

    /// <summary>
    /// Requirement level.
    /// </summary>
    public ProtocolRequirementLevel Level { get; init; }

    /// <summary>
    /// Human-readable constraint note.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Source-document reference.
    /// </summary>
    public ProtocolSourceReference? Source { get; init; }

    /// <summary>
    /// Indicates whether the constraint should be compiled into executable checks.
    /// </summary>
    public bool IsActive { get; init; }
}
