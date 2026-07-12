namespace BeamKit.Protocols;

/// <summary>
/// Structure expected by a radiotherapy protocol.
/// </summary>
public sealed record ProtocolStructureRequirement
{
    /// <summary>
    /// Creates an empty structure requirement for JSON deserialization.
    /// </summary>
    public ProtocolStructureRequirement()
    {
        Id = string.Empty;
        Name = string.Empty;
        Aliases = Array.Empty<string>();
    }

    /// <summary>
    /// Creates a structure requirement.
    /// </summary>
    public ProtocolStructureRequirement(
        string id,
        string name,
        ProtocolStructureRole role,
        ProtocolRequirementLevel level = ProtocolRequirementLevel.Required,
        IEnumerable<string>? aliases = null,
        bool mustHaveContours = true,
        string? description = null,
        ProtocolSourceReference? source = null)
    {
        Id = ProtocolText.Required(id, nameof(id));
        Name = ProtocolText.Required(name, nameof(name));
        Role = role;
        Level = level;
        Aliases = ProtocolText.CleanList(aliases);
        MustHaveContours = mustHaveContours;
        Description = ProtocolText.Optional(description);
        Source = source;
    }

    /// <summary>
    /// Stable structure requirement id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Canonical structure name expected in the plan.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Structure role.
    /// </summary>
    public ProtocolStructureRole Role { get; init; }

    /// <summary>
    /// Requirement level.
    /// </summary>
    public ProtocolRequirementLevel Level { get; init; }

    /// <summary>
    /// Non-canonical names commonly seen in source protocols or TPS exports.
    /// </summary>
    public IReadOnlyList<string> Aliases { get; init; }

    /// <summary>
    /// Indicates whether empty contours should be flagged.
    /// </summary>
    public bool MustHaveContours { get; init; }

    /// <summary>
    /// Human-readable structure note.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Source-document reference.
    /// </summary>
    public ProtocolSourceReference? Source { get; init; }
}
