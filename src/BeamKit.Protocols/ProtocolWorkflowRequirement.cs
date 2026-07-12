namespace BeamKit.Protocols;

/// <summary>
/// Non-dosimetric workflow expectation from an RT-PX package.
/// </summary>
public sealed record ProtocolWorkflowRequirement
{
    /// <summary>
    /// Creates an empty workflow requirement for JSON deserialization.
    /// </summary>
    public ProtocolWorkflowRequirement()
    {
        Id = string.Empty;
        Title = string.Empty;
        Type = string.Empty;
        IsActive = true;
    }

    /// <summary>
    /// Creates a workflow requirement.
    /// </summary>
    public ProtocolWorkflowRequirement(
        string id,
        string title,
        string type,
        ProtocolRequirementLevel level = ProtocolRequirementLevel.Required,
        string? description = null,
        ProtocolSourceReference? source = null,
        bool isActive = true)
    {
        Id = ProtocolText.Required(id, nameof(id));
        Title = ProtocolText.Required(title, nameof(title));
        Type = ProtocolText.Required(type, nameof(type));
        Level = level;
        Description = ProtocolText.Optional(description);
        Source = source;
        IsActive = isActive;
    }

    /// <summary>
    /// Stable workflow requirement id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Human-readable title.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Workflow category or future evaluator type.
    /// </summary>
    public string Type { get; init; }

    /// <summary>
    /// Requirement level.
    /// </summary>
    public ProtocolRequirementLevel Level { get; init; }

    /// <summary>
    /// Human-readable workflow note.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Source-document reference.
    /// </summary>
    public ProtocolSourceReference? Source { get; init; }

    /// <summary>
    /// Indicates whether this requirement is active.
    /// </summary>
    public bool IsActive { get; init; }
}
