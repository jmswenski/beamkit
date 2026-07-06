namespace BeamKit.PlanCheck;

/// <summary>
/// One configurable plan-check definition.
/// </summary>
public sealed record PlanCheckDefinition
{
    /// <summary>
    /// Creates a plan-check definition.
    /// </summary>
    public PlanCheckDefinition(
        string id,
        string title,
        string type,
        PlanCheckSeverity severity = PlanCheckSeverity.Failure,
        string? description = null,
        string? reference = null,
        IReadOnlyDictionary<string, string>? parameters = null,
        bool isActive = true)
    {
        Id = PlanCheckText.Required(id, nameof(id));
        Title = PlanCheckText.Required(title, nameof(title));
        Type = PlanCheckText.Required(type, nameof(type));
        Severity = severity;
        Description = PlanCheckText.Optional(description);
        Reference = PlanCheckText.Optional(reference);
        Parameters = parameters ?? new Dictionary<string, string>();
        IsActive = isActive;
    }

    /// <summary>
    /// Stable check identifier.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Human-readable check title.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Built-in check type.
    /// </summary>
    public string Type { get; init; }

    /// <summary>
    /// Severity applied when the check does not pass.
    /// </summary>
    public PlanCheckSeverity Severity { get; init; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional protocol, guideline, owner, or reminder reference.
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>
    /// Check parameters.
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; init; }

    /// <summary>
    /// Indicates whether the check should be evaluated.
    /// </summary>
    public bool IsActive { get; init; }
}
