namespace BeamKit.Protocols;

/// <summary>
/// Explicit plan-check requirement that should be carried into a generated rule pack.
/// </summary>
public sealed record ProtocolPlanCheckRequirement
{
    /// <summary>
    /// Creates an empty plan-check requirement for JSON deserialization.
    /// </summary>
    public ProtocolPlanCheckRequirement()
    {
        Id = string.Empty;
        Title = string.Empty;
        Type = string.Empty;
        Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        IsActive = true;
    }

    /// <summary>
    /// Creates an explicit plan-check requirement.
    /// </summary>
    public ProtocolPlanCheckRequirement(
        string id,
        string title,
        string type,
        ProtocolRequirementLevel level = ProtocolRequirementLevel.Required,
        IReadOnlyDictionary<string, string>? parameters = null,
        string? description = null,
        ProtocolSourceReference? source = null,
        bool isActive = true)
    {
        Id = ProtocolText.Required(id, nameof(id));
        Title = ProtocolText.Required(title, nameof(title));
        Type = ProtocolText.Required(type, nameof(type));
        Level = level;
        Parameters = parameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Description = ProtocolText.Optional(description);
        Source = source;
        IsActive = isActive;
    }

    /// <summary>
    /// Stable check id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Human-readable title.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// BeamKit plan-check type.
    /// </summary>
    public string Type { get; init; }

    /// <summary>
    /// Requirement level.
    /// </summary>
    public ProtocolRequirementLevel Level { get; init; }

    /// <summary>
    /// Check parameters.
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; init; }

    /// <summary>
    /// Human-readable check note.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Source-document reference.
    /// </summary>
    public ProtocolSourceReference? Source { get; init; }

    /// <summary>
    /// Indicates whether the check is active.
    /// </summary>
    public bool IsActive { get; init; }
}
