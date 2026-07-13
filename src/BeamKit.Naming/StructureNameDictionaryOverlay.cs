namespace BeamKit.Naming;

/// <summary>
/// Institution, physician, disease-site, or protocol overlay applied to a base structure-name dictionary.
/// </summary>
public sealed record StructureNameDictionaryOverlay
{
    /// <summary>
    /// Creates a dictionary overlay.
    /// </summary>
    public StructureNameDictionaryOverlay(
        string id,
        string? baseDictionaryId = null,
        string? name = null,
        string? version = null,
        string? description = null,
        string? source = null,
        IEnumerable<string>? tagsToAdd = null,
        IEnumerable<string>? canonicalNamesToAdd = null,
        IEnumerable<StructureNameAlias>? aliasesToAdd = null,
        IEnumerable<StructureNameRegexMapping>? regexMappingsToAdd = null,
        IEnumerable<string>? requiredStructureNamesToAdd = null,
        IEnumerable<string>? requiredStructureNamesToRemove = null,
        IEnumerable<DeprecatedStructureName>? deprecatedNamesToAdd = null)
    {
        Id = NamingText.Required(id, nameof(id));
        BaseDictionaryId = NamingText.Optional(baseDictionaryId);
        Name = NamingText.Optional(name);
        Version = NamingText.Optional(version);
        Description = NamingText.Optional(description);
        Source = NamingText.Optional(source);
        TagsToAdd = NormalizeList(tagsToAdd);
        CanonicalNamesToAdd = NormalizeList(canonicalNamesToAdd);
        AliasesToAdd = aliasesToAdd?.ToArray() ?? Array.Empty<StructureNameAlias>();
        RegexMappingsToAdd = regexMappingsToAdd?.ToArray() ?? Array.Empty<StructureNameRegexMapping>();
        RequiredStructureNamesToAdd = NormalizeList(requiredStructureNamesToAdd);
        RequiredStructureNamesToRemove = NormalizeList(requiredStructureNamesToRemove);
        DeprecatedNamesToAdd = deprecatedNamesToAdd?.ToArray() ?? Array.Empty<DeprecatedStructureName>();
    }

    /// <summary>
    /// Stable overlay identifier.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Expected base dictionary id.
    /// </summary>
    public string? BaseDictionaryId { get; init; }

    /// <summary>
    /// Optional replacement dictionary name after composition.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Optional replacement dictionary version after composition.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Optional overlay description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional overlay source label.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Tags appended to the composed dictionary.
    /// </summary>
    public IReadOnlyList<string> TagsToAdd { get; init; }

    /// <summary>
    /// Canonical names appended to the base dictionary.
    /// </summary>
    public IReadOnlyList<string> CanonicalNamesToAdd { get; init; }

    /// <summary>
    /// Alias mappings appended to or replacing base aliases with the same alias text.
    /// </summary>
    public IReadOnlyList<StructureNameAlias> AliasesToAdd { get; init; }

    /// <summary>
    /// Regex mappings appended to or replacing base regex mappings with the same pattern.
    /// </summary>
    public IReadOnlyList<StructureNameRegexMapping> RegexMappingsToAdd { get; init; }

    /// <summary>
    /// Required canonical names appended to the base dictionary.
    /// </summary>
    public IReadOnlyList<string> RequiredStructureNamesToAdd { get; init; }

    /// <summary>
    /// Required canonical names removed from the base dictionary.
    /// </summary>
    public IReadOnlyList<string> RequiredStructureNamesToRemove { get; init; }

    /// <summary>
    /// Deprecated-name mappings appended to or replacing base deprecations with the same name.
    /// </summary>
    public IReadOnlyList<DeprecatedStructureName> DeprecatedNamesToAdd { get; init; }

    private static IReadOnlyList<string> NormalizeList(IEnumerable<string>? values)
    {
        return values?.Select(value => NamingText.Required(value, nameof(values))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            ?? Array.Empty<string>();
    }
}
