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
        IEnumerable<string>? requiredStructureNames = null)
    {
        Name = NamingText.Required(name, nameof(name));
        CanonicalNames = canonicalNames?.Select(value => NamingText.Required(value, nameof(canonicalNames))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            ?? throw new ArgumentNullException(nameof(canonicalNames));
        Aliases = aliases?.ToArray() ?? Array.Empty<StructureNameAlias>();
        RegexMappings = regexMappings?.ToArray() ?? Array.Empty<StructureNameRegexMapping>();
        RequiredStructureNames = requiredStructureNames?.Select(value => NamingText.Required(value, nameof(requiredStructureNames))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            ?? Array.Empty<string>();
    }

    /// <summary>
    /// Dictionary name.
    /// </summary>
    public string Name { get; init; }

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
}
