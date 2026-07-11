namespace BeamKit.RulePacks;

/// <summary>
/// One field-level rule-pack change.
/// </summary>
public sealed record RulePackDiffItem
{
    /// <summary>
    /// Creates a rule-pack diff item.
    /// </summary>
    public RulePackDiffItem(
        RulePackChangeKind kind,
        string area,
        string id,
        string property,
        string? oldValue,
        string? newValue,
        bool isPolicyRelevant,
        string? message = null)
    {
        Kind = kind;
        Area = RulePackText.Required(area, nameof(area));
        Id = RulePackText.Required(id, nameof(id));
        Property = RulePackText.Required(property, nameof(property));
        OldValue = RulePackText.Optional(oldValue);
        NewValue = RulePackText.Optional(newValue);
        IsPolicyRelevant = isPolicyRelevant;
        Message = RulePackText.Optional(message);
    }

    /// <summary>
    /// Change kind.
    /// </summary>
    public RulePackChangeKind Kind { get; init; }

    /// <summary>
    /// Rule-pack area, such as Manifest, ClinicalRule, PlanCheck, Naming, or MachineProfile.
    /// </summary>
    public string Area { get; init; }

    /// <summary>
    /// Stable changed item identifier.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Changed property name.
    /// </summary>
    public string Property { get; init; }

    /// <summary>
    /// Value in the baseline rule pack.
    /// </summary>
    public string? OldValue { get; init; }

    /// <summary>
    /// Value in the comparison rule pack.
    /// </summary>
    public string? NewValue { get; init; }

    /// <summary>
    /// Indicates whether the change can affect plan-check behavior or governance decisions.
    /// </summary>
    public bool IsPolicyRelevant { get; init; }

    /// <summary>
    /// Human-readable change summary.
    /// </summary>
    public string? Message { get; init; }
}
