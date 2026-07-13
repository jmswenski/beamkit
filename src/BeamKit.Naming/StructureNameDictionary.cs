namespace BeamKit.Naming;

/// <summary>
/// Dictionary of canonical structure names, aliases, regex mappings, and required structures.
/// </summary>
public sealed record StructureNameDictionary
{
    /// <summary>
    /// Creates a structure name dictionary.
    /// </summary>
    public StructureNameDictionary(
        string name,
        IEnumerable<string> canonicalNames,
        IEnumerable<StructureNameAlias>? aliases = null,
        IEnumerable<StructureNameRegexMapping>? regexMappings = null,
        IEnumerable<string>? requiredStructureNames = null,
        string? id = null,
        string? version = null,
        string? description = null,
        string? source = null,
        IEnumerable<string>? tags = null,
        IEnumerable<DeprecatedStructureName>? deprecatedNames = null)
    {
        Name = NamingText.Required(name, nameof(name));
        CanonicalNames = canonicalNames?.Select(value => NamingText.Required(value, nameof(canonicalNames))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            ?? throw new ArgumentNullException(nameof(canonicalNames));
        Aliases = aliases?.ToArray() ?? Array.Empty<StructureNameAlias>();
        RegexMappings = regexMappings?.ToArray() ?? Array.Empty<StructureNameRegexMapping>();
        RequiredStructureNames = requiredStructureNames?.Select(value => NamingText.Required(value, nameof(requiredStructureNames))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            ?? Array.Empty<string>();
        Id = NamingText.Optional(id);
        Version = NamingText.Optional(version);
        Description = NamingText.Optional(description);
        Source = NamingText.Optional(source);
        Tags = tags?.Select(value => NamingText.Required(value, nameof(tags))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            ?? Array.Empty<string>();
        DeprecatedNames = deprecatedNames?.ToArray() ?? Array.Empty<DeprecatedStructureName>();
    }

    /// <summary>
    /// Stable dictionary identifier for versioned policy management.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Dictionary name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Dictionary version.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Human-readable dictionary description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Source label, such as TG-263 starter, institution, physician, or protocol overlay.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Optional tags used for review, filtering, and governance.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; }

    /// <summary>
    /// Canonical structure names recognized by the dictionary.
    /// </summary>
    public IReadOnlyList<string> CanonicalNames { get; init; }

    /// <summary>
    /// Explicit aliases.
    /// </summary>
    public IReadOnlyList<StructureNameAlias> Aliases { get; init; }

    /// <summary>
    /// Regex mappings evaluated after exact and normalized alias matching.
    /// </summary>
    public IReadOnlyList<StructureNameRegexMapping> RegexMappings { get; init; }

    /// <summary>
    /// Canonical structures expected to exist for a validation template.
    /// </summary>
    public IReadOnlyList<string> RequiredStructureNames { get; init; }

    /// <summary>
    /// Names that should be migrated to a canonical replacement.
    /// </summary>
    public IReadOnlyList<DeprecatedStructureName> DeprecatedNames { get; init; }
}
