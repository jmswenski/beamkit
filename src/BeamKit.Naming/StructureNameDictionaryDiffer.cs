namespace BeamKit.Naming;

/// <summary>
/// Compares two structure-name dictionaries for reviewable changes.
/// </summary>
public sealed class StructureNameDictionaryDiffer
{
    /// <summary>
    /// Compares dictionaries.
    /// </summary>
    public StructureNameDictionaryDiffReport Compare(StructureNameDictionary oldDictionary, StructureNameDictionary newDictionary)
    {
        ArgumentNullException.ThrowIfNull(oldDictionary);
        ArgumentNullException.ThrowIfNull(newDictionary);

        var changes = new List<StructureNameDictionaryDiffChange>();
        AddMetadataChange(changes, "id", oldDictionary.Id, newDictionary.Id);
        AddMetadataChange(changes, "version", oldDictionary.Version, newDictionary.Version, isPolicyRelevant: false);
        AddSetChanges(changes, "Canonical", oldDictionary.CanonicalNames, newDictionary.CanonicalNames, value => value);
        AddAliasChanges(changes, oldDictionary.Aliases, newDictionary.Aliases);
        AddRegexChanges(changes, oldDictionary.RegexMappings, newDictionary.RegexMappings);
        AddSetChanges(changes, "Required", oldDictionary.RequiredStructureNames, newDictionary.RequiredStructureNames, value => value);
        AddDeprecatedChanges(changes, oldDictionary.DeprecatedNames, newDictionary.DeprecatedNames);
        return new StructureNameDictionaryDiffReport(oldDictionary.Name, newDictionary.Name, changes);
    }

    private static void AddMetadataChange(
        ICollection<StructureNameDictionaryDiffChange> changes,
        string key,
        string? oldValue,
        string? newValue,
        bool isPolicyRelevant = true)
    {
        if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            changes.Add(new StructureNameDictionaryDiffChange(
                "Metadata",
                key,
                StructureNameDictionaryChangeKind.Changed,
                $"Dictionary {key} changed.",
                oldValue,
                newValue,
                isPolicyRelevant));
        }
    }

    private static void AddSetChanges<T>(
        ICollection<StructureNameDictionaryDiffChange> changes,
        string category,
        IEnumerable<T> oldValues,
        IEnumerable<T> newValues,
        Func<T, string> keySelector)
    {
        var oldByKey = oldValues.ToDictionary(keySelector, StringComparer.OrdinalIgnoreCase);
        var newByKey = newValues.ToDictionary(keySelector, StringComparer.OrdinalIgnoreCase);
        foreach (var removed in oldByKey.Keys.Except(newByKey.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            changes.Add(new StructureNameDictionaryDiffChange(category, removed, StructureNameDictionaryChangeKind.Removed, $"{category} '{removed}' was removed.", removed, null));
        }

        foreach (var added in newByKey.Keys.Except(oldByKey.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            changes.Add(new StructureNameDictionaryDiffChange(category, added, StructureNameDictionaryChangeKind.Added, $"{category} '{added}' was added.", null, added));
        }
    }

    private static void AddAliasChanges(
        ICollection<StructureNameDictionaryDiffChange> changes,
        IEnumerable<StructureNameAlias> oldAliases,
        IEnumerable<StructureNameAlias> newAliases)
    {
        AddMappedChanges(
            changes,
            "Alias",
            oldAliases,
            newAliases,
            alias => alias.Alias,
            alias => $"{alias.CanonicalName}|{alias.Source}");
    }

    private static void AddRegexChanges(
        ICollection<StructureNameDictionaryDiffChange> changes,
        IEnumerable<StructureNameRegexMapping> oldMappings,
        IEnumerable<StructureNameRegexMapping> newMappings)
    {
        AddMappedChanges(
            changes,
            "Regex",
            oldMappings,
            newMappings,
            mapping => mapping.Pattern,
            mapping => $"{mapping.CanonicalName}|{mapping.Source}");
    }

    private static void AddDeprecatedChanges(
        ICollection<StructureNameDictionaryDiffChange> changes,
        IEnumerable<DeprecatedStructureName> oldDeprecatedNames,
        IEnumerable<DeprecatedStructureName> newDeprecatedNames)
    {
        AddMappedChanges(
            changes,
            "Deprecated",
            oldDeprecatedNames,
            newDeprecatedNames,
            deprecated => deprecated.Name,
            deprecated => $"{deprecated.CanonicalName}|{deprecated.Reason}|{deprecated.Source}");
    }

    private static void AddMappedChanges<T>(
        ICollection<StructureNameDictionaryDiffChange> changes,
        string category,
        IEnumerable<T> oldValues,
        IEnumerable<T> newValues,
        Func<T, string> keySelector,
        Func<T, string> valueSelector)
    {
        var oldByKey = oldValues.ToDictionary(keySelector, StringComparer.OrdinalIgnoreCase);
        var newByKey = newValues.ToDictionary(keySelector, StringComparer.OrdinalIgnoreCase);
        foreach (var removed in oldByKey.Keys.Except(newByKey.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            changes.Add(new StructureNameDictionaryDiffChange(category, removed, StructureNameDictionaryChangeKind.Removed, $"{category} '{removed}' was removed.", valueSelector(oldByKey[removed]), null));
        }

        foreach (var added in newByKey.Keys.Except(oldByKey.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            changes.Add(new StructureNameDictionaryDiffChange(category, added, StructureNameDictionaryChangeKind.Added, $"{category} '{added}' was added.", null, valueSelector(newByKey[added])));
        }

        foreach (var key in oldByKey.Keys.Intersect(newByKey.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            var oldValue = valueSelector(oldByKey[key]);
            var newValue = valueSelector(newByKey[key]);
            if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
            {
                changes.Add(new StructureNameDictionaryDiffChange(category, key, StructureNameDictionaryChangeKind.Changed, $"{category} '{key}' changed.", oldValue, newValue));
            }
        }
    }
}
