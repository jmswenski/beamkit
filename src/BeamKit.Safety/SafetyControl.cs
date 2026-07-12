namespace BeamKit.Safety;

/// <summary>
/// Safety control used to reduce or detect clinical workflow risk.
/// </summary>
public sealed record SafetyControl
{
    /// <summary>
    /// Creates an empty safety control for JSON deserialization.
    /// </summary>
    public SafetyControl()
    {
        Id = string.Empty;
        Title = string.Empty;
        Description = string.Empty;
        EvidenceIds = Array.Empty<string>();
    }

    /// <summary>
    /// Creates a safety control.
    /// </summary>
    public SafetyControl(
        string id,
        string title,
        string description,
        SafetyControlType type,
        string? owner = null,
        bool isRequired = true,
        bool isSatisfied = false,
        IEnumerable<string>? evidenceIds = null)
    {
        Id = SafetyText.Required(id, nameof(id));
        Title = SafetyText.Required(title, nameof(title));
        Description = SafetyText.Required(description, nameof(description));
        Type = type;
        Owner = SafetyText.Optional(owner);
        IsRequired = isRequired;
        IsSatisfied = isSatisfied;
        EvidenceIds = SafetyText.CleanList(evidenceIds);
    }

    /// <summary>
    /// Stable control id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Human-readable control title.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Control description.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Control type.
    /// </summary>
    public SafetyControlType Type { get; init; }

    /// <summary>
    /// Person, group, or system responsible for the control.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Indicates whether this control is required for acceptance.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Indicates whether the control has been satisfied.
    /// </summary>
    public bool IsSatisfied { get; init; }

    /// <summary>
    /// Evidence item ids supporting the control.
    /// </summary>
    public IReadOnlyList<string> EvidenceIds { get; init; }
}
