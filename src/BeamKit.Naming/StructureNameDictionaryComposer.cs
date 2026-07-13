namespace BeamKit.Naming;

/// <summary>
/// Applies local overlays to base structure-name dictionaries.
/// </summary>
public static class StructureNameDictionaryComposer
{
    /// <summary>
    /// Applies an overlay to a base dictionary.
    /// </summary>
    public static StructureNameDictionary Apply(StructureNameDictionary baseDictionary, StructureNameDictionaryOverlay overlay)
    {
        ArgumentNullException.ThrowIfNull(baseDictionary);
        ArgumentNullException.ThrowIfNull(overlay);

        if (!string.IsNullOrWhiteSpace(overlay.BaseDictionaryId)
            && !string.Equals(baseDictionary.Id, overlay.BaseDictionaryId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Overlay '{overlay.Id}' expects base dictionary '{overlay.BaseDictionaryId}', but received '{baseDictionary.Id ?? "(none)"}'.");
        }

        var canonicalNames = baseDictionary.CanonicalNames
            .Concat(overlay.CanonicalNamesToAdd)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var aliases = ReplaceByKey(
            baseDictionary.Aliases,
            overlay.AliasesToAdd,
            alias => alias.Alias);
        var regexMappings = ReplaceByKey(
            baseDictionary.RegexMappings,
            overlay.RegexMappingsToAdd,
            mapping => mapping.Pattern);
        var requiredRemove = overlay.RequiredStructureNamesToRemove.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var required = baseDictionary.RequiredStructureNames
            .Concat(overlay.RequiredStructureNamesToAdd)
            .Where(name => !requiredRemove.Contains(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var deprecated = ReplaceByKey(
            baseDictionary.DeprecatedNames,
            overlay.DeprecatedNamesToAdd,
            deprecatedName => deprecatedName.Name);
        var tags = baseDictionary.Tags
            .Concat(overlay.TagsToAdd)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new StructureNameDictionary(
            overlay.Name ?? $"{baseDictionary.Name} + {overlay.Id}",
            canonicalNames,
            aliases,
            regexMappings,
            required,
            id: baseDictionary.Id,
            version: overlay.Version ?? baseDictionary.Version,
            description: overlay.Description ?? baseDictionary.Description,
            source: overlay.Source ?? baseDictionary.Source,
            tags: tags,
            deprecatedNames: deprecated);
    }

    private static IReadOnlyList<T> ReplaceByKey<T>(
        IEnumerable<T> baseItems,
        IEnumerable<T> overlayItems,
        Func<T, string> keySelector)
    {
        var byKey = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in baseItems)
        {
            byKey[keySelector(item)] = item;
        }

        foreach (var item in overlayItems)
        {
            byKey[keySelector(item)] = item;
        }

        return byKey
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Value)
            .ToArray();
    }
}
